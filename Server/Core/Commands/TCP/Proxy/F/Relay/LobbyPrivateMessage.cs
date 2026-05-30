public static class LobbyPrivateMSG
{
    public async static Task Process(FPacket fPacket, Session session)
    {
        if(session.currentRoom is null)
        {
            return;
        }
        for (int i = 0; i < session.currentRoom.Users.Count; i++)
        {
            await ProxyReader.FinalizePacket(fPacket.ToSend(),session.currentRoom.Users[i]);
        }
    }
}