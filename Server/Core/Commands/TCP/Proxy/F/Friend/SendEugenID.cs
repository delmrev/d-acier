public class SendEugenID
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        var values = await Reader.ReadBytes(fPacket.payload, "Q");
        session.EugenID = (long)values[0];
        FResponse response = new(fPacket.channel, FClientOpcode.CONTINUE, [0x00, 0x00, 0x00, 0x00]);
        await ProxyReader.FinalizePacket(await response.ToSend(), session);
    }
    
}