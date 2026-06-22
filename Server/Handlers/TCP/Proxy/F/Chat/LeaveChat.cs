using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class LeaveChat : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            var data = await Reader.ReadBytes(fPacket.payload, "IS"); // gameid, chatKey
            var buffer = await Writer.WriteBytes("Qs", session.EugenID, (string)data[1]);
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.BM_CHAT_LEAVE, buffer);
            await session.Send(await response.ToSend());
            session.currentChat = null;
            await GlobalManager.LeftChat((string)data[1],session);
        }
    }
}