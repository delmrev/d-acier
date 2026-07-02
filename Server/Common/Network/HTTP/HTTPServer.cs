using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NLog;

public class HttpServer : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public Socket? Socket_TCP { get; private set; }
    public EndPoint EndPoint { get; private set; }
    public string Address { get; private set; }
    public int Port { get; private set; }

    private const int MaxHeaderSize = 8192;
    private bool IsStarted;
    private CancellationTokenSource? cts;

    public HttpServer(string addres, int port)
    {
        Address = addres;
        Port = port;
        EndPoint = new IPEndPoint(IPAddress.Parse(addres), port);
    }

    public HttpServer(string address, int port, EndPoint endPoint)
    {
        Address = address;
        Port = port;
        EndPoint = endPoint;
    }

    public async Task Start()
    {
        if (IsStarted)
        {
            Log.Warn("Server is already started");
            return;
        }

        Socket_TCP = new(SocketType.Stream, ProtocolType.Tcp);
        Socket_TCP.Bind(EndPoint);
        Socket_TCP.Listen();

        cts = new CancellationTokenSource();
        IsStarted = true;
        Log.Info($"HTTP Server Started on {Address}:{Port}");

        while (!cts.Token.IsCancellationRequested)
        {
            Socket clientSocket;
            try
            {
                clientSocket = await Socket_TCP.AcceptAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error accepting client");
                continue;
            }

            _ = Task.Run(() => HandleClient(clientSocket, cts.Token), cts.Token);
        }
    }

    private async Task HandleClient(Socket client, CancellationToken token)
    {
        client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 20);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
        using (client)
        using (var network = new NetworkStream(client, ownsSocket: false))
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!await HandlePacket(network, token)) 
                        break;
                }
                Log.Info($"{client.RemoteEndPoint} disconnected, reason: close connection");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error handling client {client.RemoteEndPoint}");
            }
            finally
            {
                client.Close();
                network.Dispose();
                client.Dispose();
            }
        }
    }
    private async Task<bool> HandlePacket(NetworkStream network, CancellationToken cts)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(MaxHeaderSize);
        try
        {
            int totalBytesRead = 0;
            int headerEndIndex = -1;

            while (!cts.IsCancellationRequested)
            {
                try
                {
                    int read = await network.ReadAsync(buffer.AsMemory(totalBytesRead, buffer.Length - totalBytesRead), cts);
                    if (read == 0) return false;
                    
                    totalBytesRead += read;
                    
                    var currentSpan = buffer.AsSpan(0, totalBytesRead);
                    headerEndIndex = currentSpan.IndexOf("\r\n\r\n"u8);
                    
                    if (headerEndIndex != -1) break;
                    
                    if (totalBytesRead >= MaxHeaderSize)
                    {
                        Log.Warn("Request headers exceeded max size limit.");
                        await SendError(network, 413, "Payload Too Large");
                        return false;
                    }
                } catch {}
            }

            if (headerEndIndex == -1) return false;

            HTTPRequestOptions requestOptions = new();
            HTTPResponseOptions responceOptions = new();

            ParseHeaders(buffer.AsSpan(0, headerEndIndex), requestOptions, responceOptions);

            if (requestOptions.Method == "POST")
            {
                if (requestOptions.Headers.TryGetValue("content-length", out var lengthStr) && int.TryParse(lengthStr, out int length) && length > 0)
                {
                    byte[] bodyBuffer = new byte[length];
                    
                    int headerBytesCount = headerEndIndex + 4; 
                    int bodyBytesInFirstRead = totalBytesRead - headerBytesCount;
                    
                    if (bodyBytesInFirstRead > 0)
                    {
                        int bytesToCopy = Math.Min(bodyBytesInFirstRead, length);
                        buffer.AsSpan(headerBytesCount, bytesToCopy).CopyTo(bodyBuffer);
                    }

                    int currentBodyRead = Math.Min(bodyBytesInFirstRead, length);

                    while (currentBodyRead < length && !cts.IsCancellationRequested)
                    {
                        int read = await network.ReadAsync(bodyBuffer.AsMemory(currentBodyRead, length - currentBodyRead), cts);
                        if (read == 0) break;
                        currentBodyRead += read;
                    }
                    requestOptions.BodyBytes = bodyBuffer;
                }
                else
                {
                    await SendError(network, 400, "Bad Request");
                    return true;
                }
            }

            await HTTPInputReader.ReadTheInputHTTP(requestOptions, responceOptions);
            await SendPacket(network, responceOptions);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing incoming packet");
            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private void ParseHeaders(ReadOnlySpan<byte> headersSpan, HTTPRequestOptions req, HTTPResponseOptions res)
    {
        int firstLineEnd = headersSpan.IndexOf("\r\n"u8);
        if (firstLineEnd == -1) firstLineEnd = headersSpan.Length;

        var firstLine = Encoding.UTF8.GetString(headersSpan[..firstLineEnd]);
        var requestParts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestParts.Length >= 3)
        {
            req.Method = requestParts[0];
            req.RequestURL = requestParts[1];
            req.RequestVersion = requestParts[2];
            res.RequestVersion = requestParts[2];
        }

        headersSpan = headersSpan[Math.Min(headersSpan.Length, firstLineEnd + 2)..];
        
        while (headersSpan.Length > 0)
        {
            int lineEnd = headersSpan.IndexOf("\r\n"u8);
            ReadOnlySpan<byte> lineSpan = lineEnd == -1 ? headersSpan : headersSpan[..lineEnd];

            int colonIndex = lineSpan.IndexOf((byte)':');
            if (colonIndex > 0)
            {
                var keySpan = lineSpan[..colonIndex];
                var valueSpan = lineSpan[(colonIndex + 1)..];

                string key = Encoding.UTF8.GetString(keySpan).Trim().ToLowerInvariant();
                string value = Encoding.UTF8.GetString(valueSpan).Trim();
                req.Headers[key] = value;
            }

            if (lineEnd == -1) break;
            headersSpan = headersSpan[(lineEnd + 2)..];
        }
    }
    private async Task SendError(NetworkStream stream, int code, string message)
    {
        var response = new HTTPResponseOptions { StatusCode = code, StatusString = message, RequestVersion = "HTTP/1.1" };
        await SendPacket(stream, response);
    }
    public async Task SendPacket(NetworkStream stream, HTTPResponseOptions options)
    {
        try
        {
            DateTime now = DateTime.UtcNow;
            
            var headerBuilder = new StringBuilder();
            headerBuilder.Append($"{options.RequestVersion} {options.StatusCode} {options.StatusString}\r\n");
            headerBuilder.Append($"Date: {now:R}\r\n");

            byte[]? bodyBytes = null;
            if (!string.IsNullOrEmpty(options.Body))
            {
                bodyBytes = Encoding.UTF8.GetBytes(options.Body);
                headerBuilder.Append($"Content-Type: {options.ContentType}\r\n");
                headerBuilder.Append($"Content-Length: {bodyBytes.Length}\r\n");
            }

            headerBuilder.Append("Connection: keep-alive\r\n\r\n");

            byte[] headerBytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());
            await stream.WriteAsync(headerBytes);

            if (bodyBytes != null)
            {
                await stream.WriteAsync(bodyBytes);
            }

            Log.Debug($"Response sent: {options.StatusCode} {options.StatusString}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending packet");
        }
    }
    public void Stop()
    {
        if (!IsStarted) return;

        cts?.Cancel();
        Socket_TCP?.Close();
        Socket_TCP?.Dispose();
        IsStarted = false;
        Log.Info("HTTP Server stopped");
        cts?.Dispose();
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    public async Task Restart()
    {
        Stop();
        await Start();
    }
}