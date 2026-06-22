using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class JoinChat : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            var data = await Reader.ReadBytes(fPacket.payload, "IS"); // gameid, chatKey
            var chat = await GlobalManager.GetChat((string)data[1], (int)data[0]);
            var buffer = await Writer.WriteBytes("Is", chat.users.Count, (string)data[1]);
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.BM_CHAT_ROOM_INFO, buffer);
            await session.Send(await response.ToSend());
            buffer = await Writer.WriteBytes("Qs", session.EugenID, (string)data[1]);
            response = new(fPacket.channel, (byte)FClientOpcode.BM_CHAT_JOIN, buffer);
            await session.Send(await response.ToSend());
            await GlobalManager.JoinChat((string)data[1],(int)data[0],session);
            session.currentChat = chat;
        }
    }
}