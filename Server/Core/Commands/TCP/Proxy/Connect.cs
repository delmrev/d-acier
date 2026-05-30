using NLog;
public static class ConnectMessage
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static async Task Process(byte[] IncomeData, Session session)
    {
        using MemoryStream m = new(IncomeData);
        using BinaryReader reader = new(m);
        var data = Reader.ReadBytes(reader,"BBI");
        session.game_id = (int)data[2];
        reader.BaseStream.Position = 74;
        var steamID = Reader.Readint64Le(reader);
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
        Log.Debug($"Packet: c ->");
        var buffer = Writer.WriteBytes("BQQIBQ", PacketType.CONNECT_SERVER, EugenID, (long)0, 60, statusCode, (long)0);
        Log.Debug($"Packet: c <-");
        if(statusCode == 0x0){
            Global.RegSession(session);
        }
        await ProxyReader.FinalizePacket(buffer, session);
    }
}