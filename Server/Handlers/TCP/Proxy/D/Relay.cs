using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.D
{
    public class Relay : IDPacketHandler
    {
        public async Task Process(DPacket dPacket, Session session)
        {
            var buffer = await Writer.WriteBytes("BII", (byte)'d', dPacket.channel, dPacket.channel);
            buffer.InsertRange(0,await Writer.WriteBytes("H",buffer.Count));
            await session.Send(buffer);
            if (!session.channels.TryAdd(dPacket.command, dPacket.channel))
            {
                session.channels[dPacket.command] = dPacket.channel;
            }
        }
    }
}