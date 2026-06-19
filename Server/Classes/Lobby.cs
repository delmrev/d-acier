public class Lobby(Session host, long id)
{
    public Dictionary<int,string> RoomSettings = new();
    public Dictionary<int,Session> Users = new();
    public Session Host = host;
    public long ID = id;
}