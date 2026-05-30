public static class LobbyMsg
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        if(session.currentRoom is null)
        {
            return;
        }
        for (int i = 0; i < session.currentRoom.Users.Count; i++)
        {
            if(session.currentRoom?.Users[i] is null)
            {
                return;
            }
            await ProxyReader.FinalizePacket(fPacket.ToSend(),session.currentRoom.Users[i]);
        }
    }
}