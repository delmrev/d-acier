using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class LobbyMsg : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            if(session.currentRoom is null)
            {
                return;
            }
            foreach(var user in session.currentRoom.Users)
            {
                await user.Value.Send(await fPacket.ToSend());
            }
        }
    }
}