using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.D
{
    public class Friend : IDPacketHandler
    {
        public async Task Process(DPacket dPacket, Session session)
        {
            if (session.channels.TryGetValue("friend", out int value))
            {
                GPacket gPacket = new(value);
                await session.Send(await gPacket.ToSend());
            }
            var buffer = await Writer.WriteBytes("BII", (byte)'d', dPacket.channel, dPacket.channel);
            buffer.InsertRange(0,await Writer.WriteBytes("H",buffer.Count));
            await session.Send(buffer);
            var fpacket = new FPacket(dPacket.channel, (byte)FClientOpcode.CONTINUE, await Writer.WriteBytes("B", StatusCode.Success));
            await session.Send(await fpacket.ToSend());
            if (!session.channels.TryAdd(dPacket.command, dPacket.channel))
            {
                session.channels[dPacket.command] = dPacket.channel;
            }
        }
    }
}