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

    private X509Certificate2 cert;
    private bool IsStarted;
    private CancellationTokenSource? cts;

    public HttpsServer(string addres, int port, X509Certificate2 certificate)
    {
        Address = addres;
        Port = port;
        EndPoint = new IPEndPoint(IPAddress.Parse(addres), port);
        cert = certificate;
    }

    public HttpsServer(string address, int port, EndPoint endPoint, X509Certificate2 certificate)
    {
        Address = address;
        Port = port;
        EndPoint = endPoint;
        cert = certificate;
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
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 20);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, 3);
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
                while (!token.IsCancellationRequested)
                {
                    if (!await HandlePacket(ssl, token)) 
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error handling client {client.RemoteEndPoint}");
            } finally
            {
                client.Close();
                client.Dispose();
            }
        }
    }
    private async Task<bool> HandlePacket(SslStream ssl, CancellationToken cts)
    {
        try
        {
            using MemoryStream stream = new();
            while (!cts.IsCancellationRequested)
            {
                while (true)
                {
                    var readByte = ssl.ReadByte();
                    if(readByte == -1)
                    {
                        return false;
                    }
                    byte currentByte = (byte)readByte;
                    stream.WriteByte(currentByte);
                    string currentData = Encoding.UTF8.GetString(stream.ToArray());
                    if (currentData.EndsWith("\r\n\r\n")) break;
                }
                
                HTTPRequestOptions requestOptions = new();
                HTTPResponseOptions responceOptions = new();
                string stringData = Encoding.UTF8.GetString(stream.ToArray());
                string[] lines =stringData.Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries);
                var requestline = lines[0];
                var request = requestline.Split(' ');
                requestOptions.Method = request[0];
                requestOptions.RequestURL = request[1];
                requestOptions.RequestVersion = request[2];
                responceOptions.RequestVersion = request[2];
                for (int i = 1; i < lines.Length; i++)
                {
                    var index = lines[i].IndexOf(':');
                    string key = lines[i].Substring(0, index).Trim();
                    string value = lines[i].Substring(index + 1).Trim();
                    requestOptions.Headers.Add(key,value);
                }
                if(requestOptions.Method == "POST"){
                    int length = int.Parse(requestOptions.Headers["Content-Length"]);
                    if(length <= 0)
                    {
                        responceOptions.StatusString = "Bad request";
                        responceOptions.StatusCode = 400;
                        return true;
                    }
                    byte[] bodyBuffer = new byte[length];
                    int totalRead = 0;
                    int read = await ssl.ReadAsync(bodyBuffer, totalRead, length - totalRead, cts); 
                    requestOptions.Headers.Add("Body",Encoding.UTF8.GetString(bodyBuffer));
                }
                await HTTPSInputReader.ReadTheInputHTTP(requestOptions, responceOptions);
                await SendPacket(ssl,responceOptions);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing incoming packet");
            return false;
        }
    }
    public async Task SendPacket(SslStream stream, HTTPResponseOptions options)
    {
        try
        {
            List<byte> packet = new();
            var httpString = $"{options.RequestVersion} {options.StatusCode} {options.StatusString}\r\n";
            if(options.Body is not null && options.Body != "")
            {
                var byteBody = Encoding.UTF8.GetBytes(options.Body);
                httpString += $"Content-Type: {options.ContentType}\r\n";
                httpString += $"Content-Length: {byteBody.Length}\r\n";
                httpString += "Connection: keep-alive\r\n";
                httpString += "\r\n";
                packet.AddRange(Encoding.UTF8.GetBytes(httpString));
                packet.AddRange(byteBody);
            } else
            {
                httpString += "Connection: keep-alive\r\n";
                httpString += "\r\n";
                packet.AddRange(Encoding.UTF8.GetBytes(httpString));
            }
            Log.Debug("Outgoing packet ({0} bytes):\n{1}", packet.Count, HexDump.Dump(packet.ToArray()));
            await stream.WriteAsync(packet.ToArray());
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
        Log.Info("HTTPS Server stopped");
        cts?.Dispose();
    }

    public void Dispose() => Stop();

    public async Task Restart()
    {
        Stop();
        await Start();
    }
}