using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using NLog;

public class HttpsServer : IDisposable
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
    private readonly X509Certificate2 cert;
    private ConfigData _config;
    private HTTPManager _manager = new();

    public HttpsServer(ConfigData config,X509Certificate2 certificate)
    {
        Address = config.Server.Address;
        Port = config.Server.HTTPS;
        EndPoint = new IPEndPoint(IPAddress.Parse(Address), Port);
        cert = certificate;
        _config = config;
    }

    public HttpsServer(ConfigData config,EndPoint endPoint, X509Certificate2 certificate)
    {
        Address = config.Server.Address;
        Port = config.Server.HTTPS;
        EndPoint = endPoint;
        cert = certificate;
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
        Log.Info($"HTTPS Server Started on {Address}:{Port}");

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
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, _config.Server.HTTPKeepAliveTime);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, _config.Server.HTTPKeepAliveRetryCount);
        
        using (client)
        using (var network = new NetworkStream(client, ownsSocket: false))
        using (var ssl = new SslStream(network, leaveInnerStreamOpen: false))
        {
            try
            {
                var options = new SslServerAuthenticationOptions
                {
                    EnabledSslProtocols = SslProtocols.None,
                    ServerCertificate = cert,
                    ClientCertificateRequired = false,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                };
                await ssl.AuthenticateAsServerAsync(options, token);

                byte[]? left = [];

                while (!token.IsCancellationRequested)
                {
                    left = await HandlePacket(ssl, left, token);
                    if (left == null) 
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

    private async Task<byte[]?> HandlePacket(SslStream ssl, byte[]? leftoverBytes, CancellationToken cts)
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
                    await SendError(ssl, 413, "Payload Too Large");
                    return null;
                }
                using var rcts = CancellationTokenSource.CreateLinkedTokenSource(cts);
                rcts.CancelAfter(TimeSpan.FromSeconds(30));

                try
                {
                    int read = await ssl.ReadAsync(buffer.AsMemory(totalBytesRead, buffer.Length - totalBytesRead), rcts.Token);
                    if (read == 0) 
                    {
                        return null;
                    }
                    
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
            int bodyBytesInFirstRead = totalBytesRead - headerBytesCount;
            int contentLength = 0;

            if (requestOptions.Method == "POST")
            {
                if (requestOptions.Headers.TryGetValue("content-length", out var lengthStr) && int.TryParse(lengthStr, out contentLength) && contentLength > 0)
                {
                    if (contentLength > MaxBodySize)
                    {
                        await SendError(ssl, 413, "Payload Too Large");
                        return null;
                    }
                    byte[] bodyBuffer = new byte[contentLength];
                    
                    int bytesToCopy = Math.Min(bodyBytesInFirstRead, contentLength);
                    if (bytesToCopy > 0)
                    {
                        buffer.AsSpan(headerBytesCount, bytesToCopy).CopyTo(bodyBuffer);
                    }

                    int currentBodyRead = bytesToCopy;

                    while (currentBodyRead < contentLength && !cts.IsCancellationRequested)
                    {
                        int read = await ssl.ReadAsync(bodyBuffer.AsMemory(currentBodyRead, contentLength - currentBodyRead), cts);
                        if (read == 0) return null;
                        currentBodyRead += read;
                    }
                    requestOptions.BodyBytes = bodyBuffer;
                }
                else
                {
                    await SendError(ssl, 400, "Bad Request");
                    return null;
                }
            }
            await _manager.Handle(requestOptions, responseOptions);
            await SendPacket(ssl, responseOptions);

            int excessBytes = bodyBytesInFirstRead - contentLength;
            
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

    private async Task SendError(SslStream stream, int code, string message)
    {
        var response = new HTTPResponseOptions { StatusCode = code, StatusString = message, RequestVersion = "HTTP/1.1" };
        await SendPacket(stream, response);
    }

    public async Task SendPacket(SslStream stream, HTTPResponseOptions options)
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
        catch (OperationCanceledException)
        {
            
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending packet");
        }
    }
    public static Dictionary<string, string> ParseMultipartFormData(byte[]? bodyBytes, string boundary)
    {
        var values = new Dictionary<string, string>();
        if (bodyBytes == null || bodyBytes.Length == 0) return values;

        byte[] boundaryBytes = Encoding.UTF8.GetBytes("--" + boundary);
        ReadOnlySpan<byte> span = bodyBytes.AsSpan();

        try
        {
            while (true)
            {
                int boundaryIndex = span.IndexOf(boundaryBytes);
                if (boundaryIndex == -1) break;

                span = span[(boundaryIndex + boundaryBytes.Length)..];

                if (span.Length >= 2 && span[0] == '-' && span[1] == '-')
                    break;

                if (span.Length >= 2 && span[0] == '\r' && span[1] == '\n')
                    span = span[2..];

                int headersEnd = span.IndexOf("\r\n\r\n"u8);
                if (headersEnd == -1) break;

                var headersSpan = span.Slice(0, headersEnd);
                string headersString = Encoding.UTF8.GetString(headersSpan);

                var dataSpan = span[(headersEnd + 4)..];
                
                int nextBoundary = dataSpan.IndexOf(boundaryBytes);
                if (nextBoundary == -1) break;

                var valueSpan = dataSpan[..nextBoundary];
                if (valueSpan.Length >= 2 && valueSpan[^2] == '\r' && valueSpan[^1] == '\n')
                {
                    valueSpan = valueSpan[..^2];
                }

                string nameKey = ExtractNameFromHeaders(headersString);
                
                if (!string.IsNullOrEmpty(nameKey))
                {
                    string valueStr = Encoding.UTF8.GetString(valueSpan);
                    values[nameKey] = valueStr;
                    Log.Debug($"Parsed Form Field: {nameKey} = {valueStr}");
                }

                span = dataSpan; 
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error parsing multipart/form-data");
        }

        return values;
    }
    private static string ExtractNameFromHeaders(string headers)
    {
        int nameIdx = headers.IndexOf("name=\"", StringComparison.OrdinalIgnoreCase);
        if (nameIdx == -1) return string.Empty;
        
        nameIdx += 6;
        int endIdx = headers.IndexOf('"', nameIdx);
        if (endIdx == -1) return string.Empty;
        
        return headers[nameIdx..endIdx];
    }

    public void Stop()
    {
        if (!IsStarted) return;

        cts?.Cancel();
        Socket_TCP?.Close();
        Socket_TCP?.Dispose();
        IsStarted = false;
        Log.Info("HTTPS Server stopped");
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}