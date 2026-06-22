using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class LambdaHandler(Func<FPacket, Session, Task> action) : IFPacketHandler
    {
        private readonly Func<FPacket,  Session, Task> _action = action;

        public Task Process(FPacket packet, Session session) => _action(packet, session);
    } 
}
