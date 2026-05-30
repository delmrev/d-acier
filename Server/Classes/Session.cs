using System.Net.Security;
using System.Net.Sockets;

public class Session(Socket socket, SslStream ssl, TCPServer server) : IDisposable
{
    public Socket Socket { get; } = socket;
    public SslStream Ssl { get; } = ssl;
    public TCPServer Server {get; } = server;
    public string? Name;
    public long EugenID;
    public string? SpecialRoomID;
    public Room? currentRoom;
    public bool has_EF = false;
    public List<int> channels = new();
    public Chat? currentChat;
    public int unk_1;
    public int unk_2;
    public int game_id;
    public void Dispose()
    {
        Global.LogOutSession(this);
        Socket.Close();
        Socket.Dispose();
    }
}