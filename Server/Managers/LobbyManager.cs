using System.Collections.Concurrent;
using System.Collections.ObjectModel;

public class LobbyManager
{
    private static readonly LobbyManager _instance = new();
    public static LobbyManager Instance => _instance;
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<long,Lobby>> Rooms = new();
    private readonly object _locker = new();
    private long _totalRooms = 0;
    public async Task AddRoom(Lobby room, int gameid)
    {
        Rooms.TryAdd(gameid, new());
        Rooms[gameid].TryAdd(room.ID,room);
    }
    public async Task<int> GetRoomsCount(int gameid)
    {
        Rooms.TryAdd(gameid, new());
        return Rooms[gameid].Count;
    }
    public async Task<ReadOnlyDictionary<long,Lobby>> GetRoomList(int gameid)
    {
        Rooms.TryAdd(gameid, new());
        return Rooms[gameid].AsReadOnly();
    }
    public async Task<Lobby?> GetRoom(long roomID, int gameid)
    {
        Rooms[gameid].TryGetValue(roomID, out var lobby);
        return lobby;
    }
    public async Task RemoveRoom(long roomID, int gameid)
    {
        Rooms[gameid].TryRemove(roomID, out _);
    }
    public async Task<long> GetRoomID()
    {
        lock (_locker)
        {
            byte header = Convert.ToByte(2); 
            long newid = ((long)header << 56) | (++_totalRooms & 0x00FFFFFFFFFFFFFFL);
            return newid;
        }
    }
}