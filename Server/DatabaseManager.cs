using Database.Tables;
using NLog;
using SQLite;

public static class DatabaseManager
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static SQLiteAsyncConnection _db;
    public static async Task Init()
    {
        if (!Directory.Exists("./Data"))
        {
            Directory.CreateDirectory("./Data");
            Log.Info("Dont exist data directory. Created");
        }
        _db = new("Data/database.sqlite3", false);
        await _db.CreateTableAsync<TotalRegisetered>();
        await _db.CreateTableAsync<u0>();
        await _db.CreateTableAsync<Stat>();
    }
    public static async Task<long> CreateAccount(long steamID, int gameID)
    {
        var total = await _db.Table<TotalRegisetered>().FirstOrDefaultAsync(t => t.ID == 1);
        if(total is null)
        {
            var TotalAccounts = new TotalRegisetered{ID = 1, TotalAccounts = 1};
            await _db.InsertAsync(TotalAccounts);
            if(gameID == 0){
                var user = new u0 {EugenID = 1, SteamID = steamID, Avatar = $"VirtualData/SteamGamerPicture/{steamID}", Rev = "4-def22e51543f0d06ed42d91c7488d310"};
                await _db.InsertAsync(user);
                return user.EugenID;
            } else
            {
                var user = new Stat {GameID = gameID, EugenID = 1};
                await _db.InsertAsync(user);
                return user.EugenID;
            }
        } else
        { 
            if(gameID == 0){
                total.TotalAccounts++;
                var user = new u0 {EugenID = total.TotalAccounts, SteamID = steamID, Avatar = $"VirtualData/SteamGamerPicture/{steamID}", Rev = "4-def22e51543f0d06ed42d91c7488d310"};
                var userdata = await GetU0BySteamID(steamID);
                if(userdata == null){
                    await _db.InsertAsync(user);
                }
                await _db.UpdateAsync(total);
                return user.EugenID;
            } else
            {
                var data = await _db.Table<u0>().FirstAsync(t => t.SteamID == steamID);
                var user = new Stat {GameID = gameID, EugenID = data.EugenID};
                await _db.InsertAsync(user);
                return user.EugenID;
            }
        }
    }
    public static async Task<u0?> GetU0(long EugenID)
    {
        var result = await _db.Table<u0>().FirstOrDefaultAsync(t => t.EugenID == EugenID);
        if(result is null)
        {
            Log.Error("Try to get U0 but dont have account.");
            return null;
        }
        return result;
    }
    public static async Task<u0?> GetU0BySteamID(long SteamID)
    {
        var result = await _db.Table<u0>().FirstOrDefaultAsync(t => t.SteamID == SteamID);
        if(result is null)
        {
            Log.Debug("Try to get U0 but dont have account.");
            return null;
        }
        result = await _db.Table<u0>().FirstOrDefaultAsync(t => t.SteamID == SteamID);
        return result;
    }
    public static void UpdateData<T>(T data)
    {
        _db.UpdateAsync(data);
    }
    public static async Task<Stat> GetData(long EugenID, int GameID)
    {
        var result = await _db.Table<Stat>().FirstOrDefaultAsync(t => t.EugenID == EugenID && t.GameID == GameID);
        if(result is null)
        {
            var user = await _db.Table<u0>().FirstAsync(t => t.EugenID == EugenID);
            await CreateAccount(user.SteamID, GameID);
            result = await _db.Table<Stat>().FirstAsync(t => t.EugenID == EugenID && t.GameID == GameID);
        }
        return result;
    }
    public static async Task Stop()
    {
        Log.Info("Stopping database...");
        await _db.CloseAsync();
    }
}