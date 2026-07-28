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
    private const int MaxBodySize = 10*1024*1024;
    private bool IsStarted;
    private CancellationTokenSource cts = new();
    private ConfigData _config;
    private HTTPManager _manager = new();
    public HttpServer(ConfigData config)
    {
        Address = config.Server.Address;
        Port = config.Server.HTTP;
        _config = config;
        EndPoint = new IPEndPoint(IPAddress.Parse(Address), Port);
    }

    public HttpServer(ConfigData config, EndPoint endPoint)
    {
        Address = config.Server.Address;
        Port = config.Server.HTTP;
        EndPoint = endPoint;
        _config = config;
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
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, _config.Server.HTTPKeepAliveTime);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, _config.Server.HTTPKeepAliveInterval);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, _config.Server.HTTPKeepAliveRetryCount);
        using (client)
        using (var network = new NetworkStream(client, ownsSocket: false))
        {
            try
            {
                byte[]? leftover = [];

                while (!token.IsCancellationRequested)
                {
                    leftover = await HandlePacket(network, leftover, token);
                    if (leftover == null) 
                        break;
                }
                Log.Info($"{client.RemoteEndPoint} disconnected, reason: close connection");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error handling client {client.RemoteEndPoint}");
            }
        }
    }
    private async Task<byte[]?> HandlePacket(NetworkStream network, byte[]? leftoverBytes, CancellationToken cts)
    {
        int bufferSize = leftoverBytes != null && leftoverBytes.Length > MaxHeaderSize ? leftoverBytes.Length + 1024 : MaxHeaderSize;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int totalBytesRead = 0;
            int headerEndIndex = -1;

            if (leftoverBytes != null && leftoverBytes.Length > 0)
            {
                leftoverBytes.CopyTo(buffer, 0);
                totalBytesRead = leftoverBytes.Length;
            }

            while (!cts.IsCancellationRequested)
            {
                var currentSpan = buffer.AsSpan(0, totalBytesRead);
                headerEndIndex = currentSpan.IndexOf("\r\n\r\n"u8);
                
                if (headerEndIndex != -1) break;

                if (totalBytesRead >= MaxHeaderSize)
                {
                    Log.Warn("Request headers exceeded max size limit.");
                    await SendError(network, 413, "Payload Too Large");
                    return null;
                }
                using var rcts = CancellationTokenSource.CreateLinkedTokenSource(cts);
                rcts.CancelAfter(TimeSpan.FromSeconds(30));

                try
                {
                    int read = await network.ReadAsync(buffer.AsMemory(totalBytesRead, buffer.Length - totalBytesRead), rcts.Token);
                    if (read == 0) return null;
                    
                    totalBytesRead += read;
                } 
                catch 
                { 
                    return null; 
                }
            }

            if (headerEndIndex == -1) return null;

            HTTPRequestOptions requestOptions = new();
            HTTPResponseOptions responseOptions = new();

            ParseHeaders(buffer.AsSpan(0, headerEndIndex), requestOptions, responseOptions);

            int headerBytesCount = headerEndIndex + 4; 
            int firstReadBytes = totalBytesRead - headerBytesCount;
            int contentLength = 0;

            if (requestOptions.Method == "POST")
            {
                if (requestOptions.Headers.TryGetValue("content-length", out var lengthStr) && int.TryParse(lengthStr, out contentLength) && contentLength > 0)
                {
                    if (contentLength > MaxBodySize)
                    {
                        await SendError(network, 413, "Payload Too Large");
                        return null;
                    }
                    byte[] bodyBuffer = new byte[contentLength];
                    
                    int bytesToCopy = Math.Min(firstReadBytes, contentLength);
                    if (bytesToCopy > 0)
                    {
                        buffer.AsSpan(headerBytesCount, bytesToCopy).CopyTo(bodyBuffer);
                    }

                    int bodyRead = bytesToCopy;

                    while (bodyRead < contentLength && !cts.IsCancellationRequested)
                    {
                        int read = await network.ReadAsync(bodyBuffer.AsMemory(bodyRead, contentLength - bodyRead), cts);
                        if (read == 0) return null;
                        bodyRead += read;
                    }
                    requestOptions.BodyBytes = bodyBuffer;
                }
                else
                {
                    await SendError(network, 400, "Bad Request");
                    return null;
                }
            }
            await _manager.Handle(requestOptions, responseOptions);
            await SendPacket(network, responseOptions);

            int excessBytes = firstReadBytes - contentLength;
            
            if (excessBytes > 0)
            {
                return buffer.AsSpan(headerBytesCount + contentLength, excessBytes).ToArray();
            }

            return []; 
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing incoming packet");
            return null;
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

            if (bodyBytes != null)
            {
                byte[] fullResponse = new byte[headerBytes.Length + bodyBytes.Length];
                Buffer.BlockCopy(headerBytes, 0, fullResponse, 0, headerBytes.Length);
                Buffer.BlockCopy(bodyBytes, 0, fullResponse, headerBytes.Length, bodyBytes.Length);
            
                await stream.WriteAsync(fullResponse,cts.Token);
            }
            else
            {
                await stream.WriteAsync(headerBytes,cts.Token);
            }

            await stream.FlushAsync(cts.Token);

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