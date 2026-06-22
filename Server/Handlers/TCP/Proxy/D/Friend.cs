using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.D
{
    public class Friend : IDPacketHandler
    {
        public async Task Process(DPacket dPacket, Session session)
        {
            var buffer = await Writer.WriteBytes("BII", (byte)'d', dPacket.channel, 2);
            buffer.InsertRange(0,await Writer.WriteBytes("H",buffer.Count));
            await session.Send(buffer);
            var fpacket = new FPacket(dPacket.channel, (byte)FClientOpcode.CONTINUE, await Writer.WriteBytes("B", StatusCode.Success));
            await session.Send(await fpacket.ToSend());
            if (!session.channels.Contains(2))
            {
                session.channels.Add(2);
            }
        }
    }
}