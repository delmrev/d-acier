using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class SendEugenID : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            var values = await Reader.ReadBytes(fPacket.payload, "Q");
            session.EugenID = (long)values[0];
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.CONTINUE, [0x00, 0x00, 0x00, 0x00]);
            await session.Send(await response.ToSend());
        }
        
    }
}