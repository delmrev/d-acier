using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.D
{
    public class Relay : IDPacketHandler
    {
        public async Task Process(DPacket dPacket, Session session)
        {
            await session.Send(Writer.WriteBytes("HBII",9,(byte)'d', dPacket.channel, dPacket.channel));
            if (!session.channels.TryAdd(dPacket.command, dPacket.channel))
            {
                session.channels[dPacket.command] = dPacket.channel;
            }
        }
    }
}