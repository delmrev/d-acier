public class Lobby(Session host, long id) : IDisposable
{
    private bool _disposed = false;
    private readonly object _disposalLock = new();
    public Dictionary<int,string> RoomSettings = new();
    public Dictionary<int,Session> Users = new();
    public Session Host = host;
    public long ID = id;
    public bool Is_public = false;
    public void Dispose()
    {
        lock (_disposalLock)
        {
            if (_disposed) return;
            _disposed = true;
        }
        RoomSettings.Clear();
        Users.Clear();
        Host = null;
        GC.SuppressFinalize(this);
    }
}