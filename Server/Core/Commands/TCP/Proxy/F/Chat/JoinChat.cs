public class JoinChat
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        var data = Reader.ReadBytes(fPacket.payload, "IS"); // gameid, chatKey
        var chat = Global.GetChat((string)data[1], (int)data[0]);
        var buffer = Writer.WriteBytes("Is", chat.users.Count, (string)data[1]);
        FResponse response = new(fPacket.channel, FClientOpcode.BM_CHAT_ROOM_INFO, buffer);
        await ProxyReader.FinalizePacket(response.ToSend(), session);
        buffer = Writer.WriteBytes("Qs", session.EugenID, (string)data[1]);
        response = new(fPacket.channel, FClientOpcode.BM_CHAT_JOIN, buffer);
        await ProxyReader.FinalizePacket(response.ToSend(), session);
        Global.JoinChat((string)data[1],(int)data[0],session);
        session.currentChat = chat;
    }
}