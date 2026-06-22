using System.Net.Security;
using System.Net.Sockets;
using EugnetProtocol.TCP.Proxy.F;

public class Session(Socket socket, SslStream ssl, TCPServer server) : IDisposable
{
    public Socket Socket { get; } = socket;
    public SslStream Ssl { get; } = ssl;
    public TCPServer Server {get; } = server;
    public string? Name;
    public long EugenID;
    public Lobby? currentRoom;
    public int roomKeyID = -1;
    public bool has_EF = false;
    public List<int> channels = new();
    public Chat? currentChat;
    public int unk_1;
    public int unk_2;
    public int game_id;
    public void Dispose()
    {
        currentChat?.users.Remove(this);
        currentChat = null;
        currentRoom?.Users.Remove(roomKeyID);
        if(currentRoom != null){
            LeaveLobby lobby = new();
            FPacket fPacket = new(1,0x00,[]);
            Task.Run(async() => lobby.Process(fPacket,this));
            currentRoom = null;
        }
        channels.Clear();
        Task.Run(async() => GlobalManager.LogOutSession(this));
        Socket.Close();
        Socket.Dispose();
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