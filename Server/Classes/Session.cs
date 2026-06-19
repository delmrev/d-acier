using System.Net.Security;
using System.Net.Sockets;

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
        Task.Run(async() => LeaveLobby.Process(this));
        currentRoom = null;
        channels.Clear();
        Task.Run(async() => Global.LogOutSession(this));
        Socket.Close();
        Socket.Dispose();
    }
}