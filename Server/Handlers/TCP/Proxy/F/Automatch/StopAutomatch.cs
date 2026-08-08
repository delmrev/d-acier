using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class StopAutomatch : IFPacketHandler
    {
        public async Task Process(FPacket fPacket,Session session)
        {
            var buffer = Writer.WriteBytes("BBHLLQ",0,0x14,0,0,0,-1);
            FPacket response = new(fPacket.channel,(byte)FClientOpcode.AutoMatchCancel,buffer);
            await session.Send(response.ToBytes());
            await AutomatchManager.Instance.RemoveFromAutomatch(session);
        }
    }
}
