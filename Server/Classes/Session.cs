using System.Net.Sockets;
using System.Threading.Channels;
using EugnetProtocol.TCP.Proxy.F;
using NLog;

public class Session : IAsyncDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private bool _disposed = false;
    private readonly object _disposalLock = new();
    public Socket Socket { get; }
    public Stream Stream { get; }
    private readonly Channel<byte[]> _channel;
    private readonly Task _sendTask;
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
    private readonly CancellationTokenSource _cts = new();
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
            await foreach (var packet in _channel.Reader.ReadAllAsync(_cts.Token))
            {
                if (Stream == null || !Stream.CanWrite) break;
                
                await Stream.WriteAsync(packet, _cts.Token);
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
        _cts.Cancel();
        try
        {
            await _sendTask;
        }
        catch { }
        _cts.Dispose();
        currentChat?.users.Remove(this);
        currentChat = null;
        currentRoom?.Users.Remove(roomKeyID);
        if(currentRoom != null){
            LeaveLobby lobby = new();
            FPacket fPacket = new(1,0x00,Writer.WriteBytes("BBHLLQ",0x0,0x0,0,0,0,currentRoom.ID).Result);
            await lobby.Process(fPacket, this);
            currentRoom = null;
        }
        channels.Clear();
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