using System.Net.Security;
using System.Net.Sockets;
using EugnetProtocol.TCP.Proxy.F;

public class Session(Socket socket, SslStream ssl, TCPServer server) : IDisposable
{
    private bool _disposed = false;
    private readonly object _disposalLock = new();
    public Socket Socket { get; } = socket;
    public SslStream Ssl { get; } = ssl;
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
    public void Dispose()
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
            _ = Task.Run(() => lobby.Process(fPacket, this));
            currentRoom = null;
        }
        channels.Clear();
        _ = Task.Run(() => GlobalManager.Instance.LogOutSession(this));
        try
        {
            Ssl?.Dispose();
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
        await Server.SendPacket(Ssl,data);
    }
    public async Task Send(List<byte> data)
    {
        await Server.SendPacket(Ssl,[..data]);
    }
}