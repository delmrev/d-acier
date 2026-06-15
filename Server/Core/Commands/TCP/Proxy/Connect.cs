using NLog;
public static class ConnectMessage
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static async Task Process(byte[] IncomeData, Session session)
    {
        using MemoryStream m = new(IncomeData);
        using BinaryReader reader = new(m);
        var data = await Reader.ReadBytes(reader,"BBI");
        session.game_id = (int)data[2];
        long steamID = 0;
        await Reader.ReadBytes(reader,"IIII");
        steamID = Reader.Readint64Le(reader);
        var u0 = await DatabaseManager.GetU0BySteamID(steamID);
        var config = Global.GetConfigData();
        byte statusCode = (byte)StatusCode.SUCCESS;
        long EugenID = -1;
        if(u0 == null)
        {
            statusCode = (byte)StatusCode.UNKNOWNEXTERNALAPIACCOUNT;
        } else
        {
            EugenID = u0.EugenID;
            session.EugenID = u0.EugenID;
            session.Name = u0.Name;
        }
        if(EugenID != -1 && await Global.GetSession(EugenID,session.game_id) != null)
        {
            statusCode = (byte)StatusCode.ALREADYINSESSION;
        }
        Log.Debug($"Packet: c ->");
        var buffer = await Writer.WriteBytes("BQQIBQ", PacketType.CONNECT_SERVER, EugenID, (long)0, 60, statusCode, (long)0);
        Log.Debug($"Packet: c <-");
        if(statusCode == 0x0){
            await Global.RegSession(session, session.game_id);
        }
        await ProxyReader.FinalizePacket(buffer, session);
    }
}