public class LeaveChat
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        var data = Reader.ReadBytes(fPacket.payload, "IS"); // gameid, chatKey
        var buffer = Writer.WriteBytes("Qs", session.EugenID, (string)data[1]);
        FResponse response = new(fPacket.channel, FClientOpcode.BM_CHAT_LEAVE, buffer);
        await ProxyReader.FinalizePacket(response.ToSend(), session);
        session.currentChat = null;
    }
}