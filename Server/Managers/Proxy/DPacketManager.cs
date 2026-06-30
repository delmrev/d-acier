using EugnetProtocol.Common.Interfaces;
using EugnetProtocol.TCP.Proxy.D;
using NLog;

namespace EugnetProtocol.TCP.Proxy
{
    public class DPacketManager : IProxyHandler
    {
        private readonly Dictionary<string, IDPacketHandler> _handlers = new();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public DPacketManager()
        {
            _handlers.Add("mms",new MMS());
            _handlers.Add("friend", new Friend());
            _handlers.Add("Relay.1",new Relay());
            _handlers.Add("Relay.2",new Relay());
            _handlers.Add("ath", new Ath());
        }
        public async Task Process(byte[] data, Session session)
        {
            DPacket packet = new(data);
            if(_handlers.TryGetValue(packet.command,out var handler))
            {
                await handler.Process(packet,session);
            } else
            {
                Log.Warn($"Unknown command: {packet.command}");
            }
        }
    }
}