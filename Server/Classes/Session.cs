using System.Net.Sockets;
using EugnetProtocol.TCP.Proxy.F;

public class Session(Socket socket, Stream stream, TCPServer server) : IAsyncDisposable
{
    private bool _disposed = false;
    private readonly object _disposalLock = new();
    public Socket Socket { get; } = socket;
    public Stream Stream { get; } = stream;
    public TCPServer Server {get; } = server;
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
    public async ValueTask DisposeAsync()
    {
        lock (_disposalLock)
        {
            if (_disposed) return;
            _disposed = true;
        }
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
        await Server.SendPacket(Stream,data);
    }
    public async Task Send(List<byte> data)
    {
        await Server.SendPacket(Stream,[..data]);
    }
}