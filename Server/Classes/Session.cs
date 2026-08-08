using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Channels;
using EugnetProtocol.TCP.Proxy.F;
using NLog;

public class Session : IAsyncDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private bool _disposed = false;
    private readonly object _disposalLock = new();
    private readonly Channel<byte[]> _channel;
    private readonly Task _sendTask;
    public Socket Socket { get; }
    public Stream Stream { get; }
    public string? Name;
    public long EugenID;
    public Lobby? currentRoom;
    public int roomKeyID = -1;
    public bool has_EF = false;
    public Dictionary<string,int> channels = new();
    public Chat? currentChat;
    public int unk_1;
    public int unk_2;
    public int game_id;
    public bool isConnectedToRelay = false;
    public bool isAntiHackChecked = false;
    public CancellationTokenSource cts = new();
    public ConcurrentQueue<FPacket> QueuedPackets = new();
    public Session(Socket socket, Stream stream)
    {
        Socket = socket;
        Stream = stream;

        _channel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

        _sendTask = Task.Run(SendLoopAsync);
    }
    private async Task SendLoopAsync()
    {
        try
        {
            await foreach (var packet in _channel.Reader.ReadAllAsync(cts.Token))
            {
                if (Stream == null || !Stream.CanWrite) break;

                if (GlobalManager.Instance.Config != null && GlobalManager.Instance.Config.Logging.EnableDebug)
                {
                    Log.Debug("Outgoing packet ({0} bytes):\n{1}", packet.Length, HexDump.Dump(packet));
                }
                await Stream.WriteAsync(packet, cts.Token);
            }
        }
        catch (OperationCanceledException) {}
        catch (Exception ex)
        {
            Log.Error(ex,"Error sending packet");
        }
    }
    public async ValueTask DisposeAsync()
    {
        lock (_disposalLock)
        {
            if (_disposed) return;
            _disposed = true;
        }
        _channel.Writer.Complete();
        cts.Cancel();
        try
        {
            await _sendTask;
        }
        catch { }
        currentChat?.users.Remove(this);
        currentChat = null;
        currentRoom?.Users.Remove(roomKeyID);
        if(currentRoom != null){
            LeaveLobby lobby = new();
            FPacket fPacket = new(1,0x00,Writer.WriteBytes("BBHLLQ",0x0,0x0,0,0,0,currentRoom.ID));
            await lobby.Process(fPacket, this);
            currentRoom = null;
        }
        channels.Clear();
        QueuedPackets.Clear();
        await GlobalManager.Instance.LogOutSession(this);
        try
        {
            Stream?.Dispose();
        } catch {}
        try
        {
            if (Socket != null)
            {
                if (Socket.Connected)
                {
                    Socket.Shutdown(SocketShutdown.Both);
                }
                Socket.Close();
                Socket.Dispose();
            }
        } catch {}

        GC.SuppressFinalize(this);
    }
    public async Task Send(byte[] data)
    {
        _channel.Writer.TryWrite(data);
    }
    public async Task Send(List<byte> data)
    {
        _channel.Writer.TryWrite([..data]);
    }
}