using NLog;

public class PrivateMessage
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static async Task Process(FPacket fPacket, Session session)
    {
        var values = await Reader.ReadBytes(fPacket.payload,"I");
        var friend = await Global.GetSession((long)values[0],session.game_id);
        if(friend == null)
        {
            Log.Warn("Try to send message to user who offline");
            return;
        }
        await ProxyReader.FinalizePacket(await fPacket.ToSend(),friend);
    }
}