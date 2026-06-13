using System.Diagnostics;
using Database.Tables;
using NLog;

public static class OldConnect
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static async Task Process(byte[] IncomeData, Session session)
    {
        using MemoryStream m = new(IncomeData);
        using BinaryReader reader = new(m);
        var data = await Reader.ReadBytes(reader,"BIISSBS");
        long steamID = long.Parse((string)data[6]);
        var user = await DatabaseManager.GetU0BySteamID(steamID);
        byte statusCode = (byte)StatusCode.SUCCESS;
        int gameid = (int)data[2];
        if (user is not null)
        {
            Log.Debug($"1Packet: c ->");
            if(user.Password == null)
            {
                user.Password = (string)data[4];
                DatabaseManager.UpdateData(user);
            } else if(user.Password != (string)data[4])
            {
                statusCode = (byte)StatusCode.INCORRECTIDENTIFICATION;
                var buf = await Writer.WriteBytes("BQQIBQ", PacketType.CONNECT_SERVER, -1, (long)0, 60, statusCode, (long)0);
                await ProxyReader.FinalizePacket(buf,session);
                return;
            }
            if(await DatabaseManager.GetData(user.EugenID,gameid) == null)
            {
                await DatabaseManager.CreateAccount(steamID,gameid);
            }
            session.game_id = gameid;
            session.EugenID = user.EugenID;
            session.Name = user.Name;
            var buffer = await Writer.WriteBytes("BQQIBQ", PacketType.CONNECT_SERVER, user.EugenID, (long)0, 60, statusCode, (long)0);
            await ProxyReader.FinalizePacket(buffer, session);
            Log.Debug($"Packet: c <-");
        } else
        {
            Log.Debug($"Packet: c ->");
            var newEugid = await DatabaseManager.CreateAccount(steamID,0);
            if(newEugid == -1){
                Log.Error("OldLogin: EugenID is -1");
                return;
            }
            user = await DatabaseManager.GetU0(newEugid);
            if(user is null)
            {
                Log.Error("OldConnect: user is null");
                return;
            }
            user.Name = (string)data[3];
            user.Password = (string)data[4];
            DatabaseManager.UpdateData(user);
            await DatabaseManager.CreateAccount(steamID,gameid);
            session.game_id = gameid;
            session.EugenID = newEugid;
            session.Name = (string)data[3];
            var buffer = await Writer.WriteBytes("BQQIBQ", PacketType.CONNECT_SERVER, newEugid, (long)0, 60, statusCode, (long)0);
            await ProxyReader.FinalizePacket(buffer, session);
            Log.Debug($"Packet: c <-");
        }
    }
}