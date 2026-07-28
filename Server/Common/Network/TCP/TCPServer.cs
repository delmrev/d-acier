using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using NLog;
using System.Buffers.Binary;
using System.Buffers;
using System.Collections.Concurrent;

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
    private readonly CancellationTokenSource _cts = new();
    private ConfigData _config;
    private readonly ConcurrentDictionary<Guid, Task> _activeClients = new();
    private const int MaxMessageSize = 10*1024*1024;

    public TCPServer(ConfigData config, X509Certificate2 certificate)
    {
        _cert = certificate;
        _config = config;
        Address = config.Server.Address;
        Port = config.Server.TCP;
        EndPoint = new IPEndPoint(IPAddress.Parse(Address), Port);
    }

    public TCPServer(ConfigData config, EndPoint endPoint, X509Certificate2 certificate)
    {
        _cert = certificate;
        _config = config;
        Address = config.Server.Address;
        Port = config.Server.TCP;
        EndPoint = endPoint;
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
            Guid clientId = Guid.NewGuid();
            Task clientTask = HandleClient(clientSocket, _cts.Token);
            _activeClients.TryAdd(clientId, clientTask);

            _ = clientTask.ContinueWith(_ => _activeClients.TryRemove(clientId, out _), TaskContinuationOptions.ExecuteSynchronously);
        }
    }

    private async Task HandleClient(Socket client, CancellationToken token)
    {
        client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, _config.Server.TCPKeepAliveTime);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, _config.Server.TCPKeepAliveInterval);
        client.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, _config.Server.TCPKeepAliveRetryCount);
        Session? session = null;
        Stream? stream = null;
        byte[] readBuffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            byte[] peekBuffer = new byte[1];
            int read = await client.ReceiveAsync(peekBuffer.AsMemory(0, 1), SocketFlags.Peek, _cts.Token);
            
            if (read > 0 && peekBuffer[0] == 0x16)
            {
                var network = new NetworkStream(client, ownsSocket: false);
                var ssl =  new SslStream(network, leaveInnerStreamOpen: false);
                var options = new SslServerAuthenticationOptions
                {
                    EnabledSslProtocols = SslProtocols.None,
                    ServerCertificate = _cert,
                    ClientCertificateRequired = false,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                };
                await ssl.AuthenticateAsServerAsync(options, token).WaitAsync(TimeSpan.FromSeconds(10), token);
                stream = ssl;
            }
            else
            {
                stream = new NetworkStream(client, ownsSocket: false);
            } 
            if(stream == null)
            {
                throw new ArgumentNullException();
            }
            session = new Session(client, stream);

            session.cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, session.cts.Token);

            
            using var messageBuffer = new MemoryStream();
            while (stream.CanRead && !session.cts.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(readBuffer, session.cts.Token);
                if (bytesRead <= 0) break;
                messageBuffer.Write(readBuffer, 0, bytesRead);
                if (messageBuffer.TryGetBuffer(out ArraySegment<byte> currentData))
                {
                    if (messageBuffer.Length + bytesRead > MaxMessageSize)
                    {
                        Log.Warn($"Client {client.RemoteEndPoint} exceeded maximum payload size.");
                        break;
                    }
                    int consumedBytes = await ProcessIncoming(currentData, session, session.cts.Token);
                    if (consumedBytes > 0)
                    {
                        int leftover = (int)messageBuffer.Length - consumedBytes;
                        if (leftover > 0)
                        {
                            Buffer.BlockCopy(currentData.Array!, currentData.Offset + consumedBytes, currentData.Array!, currentData.Offset, leftover);
                            messageBuffer.SetLength(leftover);
                        }
                        else
                        {
                            messageBuffer.SetLength(0);
                        }
                    }
                }
            }
            Log.Info($"{client.RemoteEndPoint} disconnected, reason: close connection");
        }
        catch (OperationCanceledException)
        {
            Log.Info($"{client.RemoteEndPoint} disconnected, reason: close connection"); 
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling client {0}", client.RemoteEndPoint);
            Log.Info($"{client.RemoteEndPoint} disconnected, reason: fail to handling");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
            if (session != null)
            {
                await session.DisposeAsync();
            }
            try
            {
                if (client.Connected)
                {
                    client.Shutdown(SocketShutdown.Both);
                }
            }
            catch (SocketException) {}
            stream?.Dispose();
            client?.Close();
            client?.Dispose();
        }
    }
    private async Task<int> ProcessIncoming(ReadOnlyMemory<byte> data, Session session, CancellationToken token)
    {
        int position = 0;
        try
        {
            while (position < data.Length && !token.IsCancellationRequested)
            {
                if (position + 2 > data.Length)
                {
                    break;
                }
                ushort length = BinaryPrimitives.ReadUInt16BigEndian(data.Span.Slice(position, 2));
                if (position + 2 + length > data.Length)
                {
                    break;
                }
                position += 2;
                ReadOnlyMemory<byte> bodyMemory = data.Slice(position, length);
                position += length; 
                
                byte[] payload = bodyMemory.ToArray();
                
                if (_config.Logging.EnableDebug)
                {
                    Log.Debug("Incoming packet ({0} bytes):\n{1}", length, HexDump.Dump(payload));
                }

                await _proxyManager.Handle(payload, session);
            }

            return position;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing incoming packet");
            if (session != null && (session.Socket == null || session.Stream == null || !session.Stream.CanRead || !session.Socket.Connected || !token.IsCancellationRequested))
            {
                await session.DisposeAsync();
            }
            return position;
        }
    }
    public void Stop()
    {
        if (!_isStarted) return;

        _cts?.Cancel();
        Socket_TCP?.Close();
        Socket_TCP?.Dispose();
        _isStarted = false;
        if (!_activeClients.IsEmpty)
        {
            Log.Info($"Waiting for {_activeClients.Count} active clients");
            try
            {
                Task.WaitAll([.. _activeClients.Values], TimeSpan.FromSeconds(3));
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Some client tasks were forced to stop.");
            }
        }
        Log.Info("TCP Server stopped");
    }

    public void Dispose(){
        Stop();
        GC.SuppressFinalize(this);
    }
}
