using System.Collections.Concurrent;
using NLog;

public class GlobalManager
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly GlobalManager _instance = new();
    public static GlobalManager Instance => _instance;
    public ConfigData? Data { get; private set; }
    private static readonly ConcurrentDictionary<int, ConcurrentDictionary<long,Session>> Players = new();
    private static object configDataLocker = new();
    private static ConfigData? Confdata;
    public async Task RegSession(Session session, int gameid)
    {
        Players.TryAdd(gameid, new());
        Players[gameid].TryAdd(session.EugenID,session);
    }
    public async Task LogOutSession(Session session)
    {
        Players[session.game_id].TryRemove(session.EugenID, out _);
    }
    public async Task<int> GetPlayersCount(int gameid)
    {
        Players.TryAdd(gameid, new());
        return Players[gameid].Count;
    }
    public async Task<Session?> GetSession(long EugenID, int gameid)
    {
        Players.TryAdd(gameid, new());
        return Players[gameid].GetValueOrDefault(EugenID);
    }
    
    public void SetConfigData(ConfigData data)
    {
        lock (configDataLocker)
        {
           Confdata = data; 
           Instance.Data = data;
        }
    }
    
    public ConfigData? GetConfigData()
    {
        lock (configDataLocker)
        {
           return Confdata;
        }
    }
    
    public async Task Stop()
    {
        foreach(var maps in Players){
            for(int i = 0; i < maps.Value.Count; i++)
            {
                foreach(var list in maps.Value){
                    await list.Value.DisposeAsync();
                }
            }
            Log.Info("Server stopped. All users disconnected");
        }
    }
}
