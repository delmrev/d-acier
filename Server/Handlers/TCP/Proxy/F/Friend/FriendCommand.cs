using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class FriendCommand : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            List<byte> buffer = new();
            byte[] packet = [ //empty?
                0x09,0x00, // unknown
                        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
            ];
            buffer.AddRange(packet);
            buffer.AddRange(await Writer.WriteBytes("Q", session.EugenID));
            packet = [0xFF, 0xFF, 0xFF, 0xFF];
            buffer.AddRange(packet);
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.BM_TEAM_COMMAND, buffer);
            await session.Send(await response.ToSend());
        }
    }
}