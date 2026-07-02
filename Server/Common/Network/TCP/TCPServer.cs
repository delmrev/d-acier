using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using NLog;

public class TCPServer : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public Socket? Socket_TCP { get; private set; }
    public EndPoint EndPoint { get; private set; }
    public string Address { get; private set; }
    public int Port { get; private set; }

    private readonly X509Certificate2 _cert;
    private ProxyManager _proxyManager = new();
    private bool _isStarted;
    private readonly CancellationTokenSource _cts;
    private ConfigData _config;

    public TCPServer(ConfigData config, X509Certificate2 certificate)
    {
        _cert = certificate;
        _config = config;
        Address = config.Server.Address;
        Port = config.Server.TCP;
        EndPoint = new IPEndPoint(IPAddress.Parse(config.Server.Address), config.Server.TCP);
        _cts = new CancellationTokenSource();
    }

    public TCPServer(ConfigData config, EndPoint endPoint, X509Certificate2 certificate)
    {
        _cert = certificate;
        _config = config;
        Address = config.Server.Address;
        Port = config.Server.TCP;
        EndPoint = endPoint;
        _cts = new CancellationTokenSource();
    }

    public async Task Start()
    {
        if (_isStarted)
        {
            Log.Warn("Server is already started");
            return;
        }

        Socket_TCP = new(SocketType.Stream, ProtocolType.Tcp);
        Socket_TCP.Bind(EndPoint);
        Socket_TCP.Listen();

        _isStarted = true;
        Log.Info("TLS 1.3 (TCP) Server Started on {0}:{1}", Address, Port);

        while (!_cts.Token.IsCancellationRequested)
        {
            Socket clientSocket;
            try
            {
                clientSocket = await Socket_TCP.AcceptAsync(_cts.Token);
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
            _ = Task.Run(() => HandleClient(clientSocket, _cts.Token), _cts.Token);
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
            Session? session = null;
            try
            {
                var options = new SslServerAuthenticationOptions
                {
                    EnabledSslProtocols = SslProtocols.None,
                    ServerCertificate = _cert,
                    ClientCertificateRequired = false,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                };
                await ssl.AuthenticateAsServerAsync(options, token);

                session = new Session(client, ssl, this);

                byte[] buffer = new byte[4096];
                int read;

                while (ssl.CanRead && (read = await ssl.ReadAsync(buffer, token)) > 0 && !_cts.IsCancellationRequested)
                {
                    await ProcessIncoming(buffer.AsSpan(0, read).ToArray(), session);
                }
                Log.Info($"{client.RemoteEndPoint} disconnected, reason: close connection");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling client {0}", client.RemoteEndPoint);
                Log.Info($"{client.RemoteEndPoint} disconnected, reason: fail to handling");
            }
            finally
            {
                if (session != null)
                {
                    await session.DisposeAsync();
                }
                ssl?.Dispose();
                client?.Close();
                client?.Dispose();
            }
        }
    }

    private async Task ProcessIncoming(byte[] data, Session session)
    {
        try
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                ushort length = Reader.ReadInt16Be(reader);
                var body = reader.ReadBytes(length);
                if (_config.Logging.EnableDebug)
                {
                    Log.Debug("Incoming packet ({0} bytes):\n{1}", length, HexDump.Dump(body));
                }
                await _proxyManager.Handle(body, session);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing incoming packet");
            if (session!=null && (session.Socket == null || session.Ssl == null || !session.Ssl.CanRead || !session.Socket.Connected))
            {
                await session.DisposeAsync();
            }
        }
    }

    public async Task SendPacket(SslStream stream, byte[] packet)
    {
        try
        {
            if (stream == null || !stream.CanWrite)
            {
                Log.Warn("Cannot write to a null socket");
                return;
            }
            if (_config.Logging.EnableDebug)
            {
                Log.Debug("Outgoing packet ({0} bytes):\n{1}", packet.Length, HexDump.Dump(packet));
            }
            await stream.WriteAsync(packet);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending packet");
        }
    }

    public void Stop()
    {
        if (!_isStarted) return;

        _cts?.Cancel();
        Socket_TCP?.Close();
        Socket_TCP?.Dispose();
        _isStarted = false;
        Log.Info("TCP Server stopped");
        _cts?.Dispose();
    }

    public void Dispose(){
        Stop();
        GC.SuppressFinalize(this);
    }

    public async Task Restart()
    {
        Stop();
        await Start();
    }
}
