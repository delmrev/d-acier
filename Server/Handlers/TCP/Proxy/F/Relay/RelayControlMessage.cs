using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class RelayControlMessage : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            if(session.currentRoom is null)
            {
                return;
            }
            var values = Reader.ReadBytes(fPacket.payload,"B");
            if(session.isAntiHackChecked && (byte)values[0] == 0x00)
            {
                await session.Send(fPacket.ToBytes());
                session.isAntiHackChecked = false;
            }
        }
    }
}