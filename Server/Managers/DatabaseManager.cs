using Database.Tables;
using EugnetProtocol.Common.Interfaces;
using NLog;
using SQLite;
namespace Database
{
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
            await _db.CreateTableAsync<u0>();
            await _db.CreateTableAsync<UserStat>();
            await _db.CreateTableAsync<ClientInfo>();
        }
        public static async Task<long> CreateAccount(long steamID, int gameID)
        {
            if (gameID == 0)
            {
                var userdata = await GetU0BySteamID(steamID);
                if (userdata != null)
                {
                    return userdata.EugenID;
                }
                var user = new u0 
                { 
                    SteamID = steamID, 
                    Avatar = $"VirtualData/SteamGamerPicture/{steamID}", 
                    Rev = "4-def22e51543f0d06ed42d91c7488d310"
                };
                await _db.InsertAsync(user); 
                return user.EugenID;
            }
            else
            {
                var data = await _db.Table<u0>().FirstOrDefaultAsync(t => t.SteamID == steamID);
                if (data == null)
                    {
                        return -1;
                    }

                UserStat stat = new()
                {
                    EugenID = data.EugenID,
                    GameID = gameID,
                    Key = "@level",
                    Value = 1
                };
                await _db.InsertAsync(stat);
                return stat.EugenID;
            }
        }
        public static async Task<ClientInfo?> GetClientInfoByEugenID(long EugenID)
        {
            var result = await _db.Table<ClientInfo>().FirstOrDefaultAsync(t => t.EugenID == EugenID);
            if(result is null)
            {
                Log.Error("Try to get U0 but dont have account. You registered account?");
                return null;
            }
            return result;
        }
        public static async Task<ClientInfo> CreateClientInfo(long EugenID)
        {
            ClientInfo info = new()
            {
                EugenID = EugenID
            };
            await _db.InsertAsync(info);
            return info;
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
                Log.Debug("Try to get U0 but dont have account. You registered account?");
                return null;
            }
            result = await _db.Table<u0>().FirstOrDefaultAsync(t => t.SteamID == SteamID);
            return result;
        }
        public static async Task UpdateData<T>(T data)
        {
            await _db.UpdateAsync(data);
        }
        public static async Task<Dictionary<string,int>> GetData(long EugenID, int GameID)
        {
            var result = await _db.Table<UserStat>()
            .Where(t => t.EugenID == EugenID && t.GameID == GameID)
            .ToListAsync();
            Dictionary<string,int> stats = new();
            for (int i = 0; i < result.Count; i++)
            {
                stats.Add(result[i].Key,result[i].Value);
            }
            return stats;
        }
        public static async Task ChangeOrAddStat(long EugenID, int gameID, string Key, int value)
        {
            var result = await _db.Table<UserStat>().FirstOrDefaultAsync(t => t.EugenID == EugenID && t.GameID == gameID && t.Key == Key);
            if(result is null)
            {
                UserStat stat = new()
                {
                    EugenID = EugenID,
                    GameID = gameID,
                    Key = Key,
                    Value = value
                };
                await _db.InsertAsync(stat);
            } else
            {
                result.Value = value;
                await _db.UpdateAsync(result);
            }
            
        }
        public static async Task<int> GetEloCount(int gameID)
        {
            var result = await _db.Table<UserStat>()
            .Where(t => t.GameID == gameID && t.Key == "ELO")
            .CountAsync();
            return result;
        }
        public static async Task<List<UserStat>> GetELOList(int gameID, int offset, int count)
        {
            var result = await _db.Table<UserStat>()
            .Where(t => t.GameID == gameID && t.Key == "ELO")
            .OrderByDescending(t => t.Value)
            .Skip(offset)
            .Take(count)
            .ToListAsync();
            return result;
        }
        public static async Task Stop()
        {
            Log.Info("Stopping database...");
            await _db.CloseAsync();
        }
    }
}