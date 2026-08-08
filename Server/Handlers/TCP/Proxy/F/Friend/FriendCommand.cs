using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class FriendCommand : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            byte[] buffer = Writer.WriteBytes("BBIQQI", 0x09, 0x00, -1, -1, session.EugenID, -1);
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.BM_TEAM_COMMAND, buffer);
            await session.Send(response.ToBytes());
        }
    }
}