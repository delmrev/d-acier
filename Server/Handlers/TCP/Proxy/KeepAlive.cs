using EugnetProtocol.Common.Interfaces;
using NLog;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class Keep_Alive : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket  packet, Session session)
        {
            Log.Info("Reseived keep-alive packet, ignoring");
        }
    }
}
