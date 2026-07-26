using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class CheckData : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            session.isAntiHackChecked = true;
        }
    }
}