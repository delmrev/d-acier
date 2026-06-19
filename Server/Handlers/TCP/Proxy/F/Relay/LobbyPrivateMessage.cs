using NLog;

public static class LobbyPrivateMSG
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public async static Task Process(FPacket fPacket, Session session)
    {
        if(session.currentRoom is null)
        {
            return;
        }
        foreach(var user in session.currentRoom.Users)
        {
            Log.Debug("Change visibility");
            await ProxyReader.FinalizePacket(await fPacket.ToSend(),user.Value);
        }
    }
}