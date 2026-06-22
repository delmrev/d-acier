using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class Continue : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            var buffer = await Writer.WriteBytes("B", StatusCode.Success);
            FPacket fresponse = new(fPacket.channel,(byte)FClientOpcode.CONTINUE,buffer);
            await session.Send(await fresponse.ToSend());
        }
    }
}