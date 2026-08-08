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
                await session.Send(gPacket.ToBytes());
            }
            await session.Send(Writer.WriteBytes("HBII",9,(byte)'d', dPacket.channel, dPacket.channel));
            var fpacket = new FPacket(dPacket.channel, (byte)FClientOpcode.CONTINUE, [(byte)StatusCode.Success]);
            await session.Send(fpacket.ToBytes());
            if (!session.channels.TryAdd(dPacket.command, dPacket.channel))
            {
                session.channels[dPacket.command] = dPacket.channel;
            }
        }
    }
}