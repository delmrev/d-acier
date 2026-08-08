using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class Continue : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            if(session.currentRoom == null || session.isConnectedToRelay)
            {
                return;
            }
            var buffer = Writer.WriteBytes("B", StatusCode.Success);
            FPacket fresponse = new(fPacket.channel,(byte)FClientOpcode.CONTINUE,buffer);
            await session.Send(fresponse.ToBytes());
            session.isConnectedToRelay = true;
            var channel = session.channels["Relay.1"];
            while (session.QueuedPackets.TryDequeue(out var packet))
            {
                packet.channel = channel;
                await session.Send(packet.ToBytes());
            }
        }
    }
}