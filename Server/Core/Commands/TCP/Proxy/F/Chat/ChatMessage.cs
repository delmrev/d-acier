public class ChatMessage
{
    public static async Task Process(FPacket fPacket, Session session)
    {
        var values = Reader.ReadBytes(fPacket.payload, "QsIs");
        FResponse response = new(fPacket.channel, FClientOpcode.BM_CHAT_MESSAGE, Writer.WriteBytes("Qsss", session.EugenID, $"{values[1]}", $"{session.Name}", $"{values[3]}"));
        await ProxyReader.FinalizePacket(response.ToSend(), session);
        Global.SendMessage(response,(string)values[1],session.game_id);
    }
}