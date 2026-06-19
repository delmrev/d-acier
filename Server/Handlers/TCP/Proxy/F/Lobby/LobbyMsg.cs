public static class LobbyMsg
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        if(session.currentRoom is null)
        {
            return;
        }
        foreach(var user in session.currentRoom.Users)
        {
            await ProxyReader.FinalizePacket(await fPacket.ToSend(),user.Value);
        }
    }
}