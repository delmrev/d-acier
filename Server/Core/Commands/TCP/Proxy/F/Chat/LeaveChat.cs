public class LeaveChat
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        var data = await Reader.ReadBytes(fPacket.payload, "IS"); // gameid, chatKey
        var buffer = await Writer.WriteBytes("Qs", session.EugenID, (string)data[1]);
        FResponse response = new(fPacket.channel, FClientOpcode.BM_CHAT_LEAVE, buffer);
        await ProxyReader.FinalizePacket(await response.ToSend(), session);
        session.currentChat = null;
        await Global.LeftChat((string)data[1],session);
    }
}