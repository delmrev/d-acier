using Database;
using EugnetProtocol.Common.Interfaces;
using NLog;

public class NormandyConnect : IProxyHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public async Task Process(byte[] body, Session session)
    {
        using MemoryStream m = new(body);
        using BinaryReader reader = new(m);
        var read = await Reader.ReadBytes(reader,"BIIIIII");
        long steamID = Reader.Readint64Le(reader);
        Log.Debug(steamID);
        session.game_id = (int)read[1];
        var u0 = await DatabaseManager.GetU0BySteamID(steamID);
        byte statusCode = (byte)StatusCode.Success;
        long EugenID = -1;
        if(u0 == null)
        {
            statusCode = (byte)StatusCode.UnknownExternalApiAccount;
        } else
        {
            EugenID = u0.EugenID;
            session.EugenID = u0.EugenID;
            session.Name = u0.Name;
        }
        if(EugenID != -1 && await GlobalManager.Instance.GetSession(EugenID,session.game_id) != null)
        {
            statusCode = (byte)StatusCode.AlreadyInSession;
        }
        Log.Debug($"Packet: c ->");
        var buffer = await Writer.WriteBytes("BQQIBQ", PacketType.CONNECT_SERVER, EugenID, (long)0, 60, statusCode, (long)0);
        buffer.InsertRange(0,await Writer.WriteBytes("H",buffer.Count));
        Log.Debug($"Packet: c <-");
        if(statusCode == 0x0){
            await GlobalManager.Instance.RegSession(session, session.game_id);
        }
        await session.Send(buffer);
    }
}