public static class Signal
{
    public async static Task Process(FPacket fPacket, Session session)
    {
        if(session.currentRoom is null)
        {
            return;
        }
        var values = Reader.ReadBytes(fPacket.payload,"B");
        if((byte)values[0] == 0x00)
        {
            for (int i = 0; i < session.currentRoom.Users.Count; i++)
            {
                await ProxyReader.FinalizePacket(fPacket.ToSend(),session.currentRoom.Users[i]);
            }
        }
    }
}