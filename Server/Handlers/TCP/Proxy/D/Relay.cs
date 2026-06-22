using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.D
{
    public class Relay : IDPacketHandler
    {
        public async Task Process(DPacket dPacket, Session session)
        {
            var buffer = await Writer.WriteBytes("BII", (byte)'d', dPacket.channel, 3);
            buffer.InsertRange(0,await Writer.WriteBytes("H",buffer.Count));
            await session.Send(buffer);
            if (!session.channels.Contains(3))
            {
                session.channels.Add(3);
            }
        }
    }
}