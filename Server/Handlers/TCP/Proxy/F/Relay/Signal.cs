public static class Signal
{
    public async static Task Process(FPacket fPacket, Session session)
    {
        if(session.currentRoom is null)
        {
            return;
        }
        var values = await Reader.ReadBytes(fPacket.payload,"B");
        if((byte)values[0] == 0x00)
        {
            foreach(var user in session.currentRoom.Users)
            {
                await ProxyReader.FinalizePacket(await fPacket.ToSend(),user.Value);
            }
        }
    }
}