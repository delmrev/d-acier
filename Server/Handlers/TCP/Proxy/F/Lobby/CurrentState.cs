using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class CurrentState : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {  
            var (success, output) = await Reader.TryReadBytes(fPacket.payload, "IIBBHIII");
            session.unk_1 = (int)output[0];
            session.unk_2 = (int)output[1];
        }
    }
}