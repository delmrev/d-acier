using NLog;

public class PrivateMessage
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static async Task Process(FPacket fPacket)
    {
        var values = Reader.ReadBytes(fPacket.payload,"I");
        var friend = Global.GetSession((long)values[0]);
        if(friend == null)
        {
            Log.Warn("Try to send message to user who offline");
            return;
        }
        await ProxyReader.FinalizePacket(fPacket.ToSend(),friend);
    }
}