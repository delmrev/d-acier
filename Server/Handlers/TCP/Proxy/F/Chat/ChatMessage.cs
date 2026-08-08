using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class ChatMessage : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            var values = Reader.ReadBytes(fPacket.payload, "QsIs");
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.BM_CHAT_MESSAGE, Writer.WriteBytes("Qsss", session.EugenID, $"{values[1]}", $"{session.Name}", $"{values[3]}"));
            await ChatManager.Instance.SendMessage(response,(string)values[1],session.game_id);
        }
    }
}