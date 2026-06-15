public class Lobby(Session host, long id)
{
    public Dictionary<int,string> RoomSettings = new();
    public List<Session> Users = new();
    public Session Host = host;
    public long ID = id;
    public bool is_visible = true;
}