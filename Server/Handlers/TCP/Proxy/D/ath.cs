using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.D
{
    public class Ath : IDPacketHandler
    {
        public async Task Process(DPacket dPacket, Session session)
        {
            var buffer = await Writer.WriteBytes("BII", (byte)'d', dPacket.channel, 4);
            buffer.InsertRange(0,await Writer.WriteBytes("H",buffer.Count));
            await session.Send(buffer);
            if (!session.channels.Contains(4))
            {
                session.channels.Add(4);
            }
        }
    }
}
