public class Friend
{
    public static async Task Process(DPacket dPacket, Session session, TCPServer server)
    {
        var fpacket = new FResponse(dPacket.channel, FClientOpcode.CONTINUE, await Writer.WriteBytes("B", StatusCode.Success));
        await ProxyReader.FinalizePacket(await fpacket.ToSend(), session);
    }
}