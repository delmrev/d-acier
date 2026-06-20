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
    private X509Certificate2 cert;

    private bool IsStarted;
    private CancellationTokenSource cts;

    public TCPServer(string addres, int port, X509Certificate2 certificate)
    {
        cert = certificate;
        Address = addres;
        Port = port;
        EndPoint = new IPEndPoint(IPAddress.Parse(addres), port);
        cts = new CancellationTokenSource();
    }

    public TCPServer(string address, int port, EndPoint endPoint, X509Certificate2 certificate)
    {
        cert = certificate;
        Address = address;
        Port = port;
        EndPoint = endPoint;
        cts = new CancellationTokenSource();
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
        Log.Info("TLS 1.3 (TCP) Server Started on {0}:{1}", Address, Port);

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
            Session? session = null;
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

                session = new Session(client, ssl, this);

                byte[] buffer = new byte[4096];
                int read;

                while (ssl.CanRead && (read = await ssl.ReadAsync(buffer, 0, buffer.Length, token)) > 0 && !cts.IsCancellationRequested)
                {
                    await ProcessIncoming(buffer.AsSpan(0, read).ToArray(), session);
                }
                Log.Info($"{client.RemoteEndPoint} diconected, reason: close connection");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error handling client {0}", client.RemoteEndPoint);
                Log.Info($"{client.RemoteEndPoint} diconected, reason: fail to handling");
            }
            finally
            {
                session?.Dispose();
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
                Log.Debug("Incoming packet ({0} bytes):\n{1}", length, HexDump.Dump(body));
                await ProxyReader.ProcessPacket(body, session);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing incoming packet");
            if (session!=null && (session.Socket == null || session.Ssl == null || !session.Ssl.CanRead || !session.Socket.Connected))
            {
                session?.Dispose();
            }
        }
    }

    public async Task SendPacket(SslStream stream, byte[] packet)
    {
        try
        {
            Log.Debug("Outgoing packet ({0} bytes):\n{1}", packet.Length, HexDump.Dump(packet));
            await stream.WriteAsync(packet);
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
        Log.Info("TCP Server stopped");
        cts?.Dispose();
    }

    public void Dispose() => Stop();

    public async Task Restart()
    {
        Stop();
        await Start();
    }
}
