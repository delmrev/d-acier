using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.D
{
    public class Ath : IDPacketHandler
    {
        public async Task Process(DPacket dPacket, Session session)
        {
            if (session.channels.TryGetValue(dPacket.command, out int value))
            {
                GPacket gPacket = new(value);
                await session.Send(await gPacket.ToSend());
            }
            var buffer = await Writer.WriteBytes("BII", (byte)'d', dPacket.channel, 4);
            buffer.InsertRange(0,await Writer.WriteBytes("H",buffer.Count));
            await session.Send(buffer);
            if (!session.channels.TryAdd(dPacket.command, dPacket.channel))
            {
                session.channels[dPacket.command] = dPacket.channel;
            }
        }
    }
}
