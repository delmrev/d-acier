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
        for (int i = 0; i < session.currentRoom.Users.Count; i++)
        {
            session.currentRoom.is_visible = false;
            Log.Debug("Change visibility");
            await ProxyReader.FinalizePacket(await fPacket.ToSend(),session.currentRoom.Users[i]);
        }
    }
}