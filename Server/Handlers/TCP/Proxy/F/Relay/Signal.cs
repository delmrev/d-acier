using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class Signal : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            if(session.currentRoom is null)
            {
                return;
            }
            var values = await Reader.ReadBytes(fPacket.payload,"B");
            if((byte)values[0] == 0x00)
            {
                foreach(var user in session.currentRoom.Users)
                {
                    await user.Value.Send(await fPacket.ToSend());
                }
            }
        }
    }
}