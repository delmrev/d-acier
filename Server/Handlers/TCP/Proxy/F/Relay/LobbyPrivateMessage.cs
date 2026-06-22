using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class LobbyPrivateMSG : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket fPacket, Session session)
        {
            if(session.currentRoom is null)
            {
                return;
            }
            foreach(var user in session.currentRoom.Users)
            {
                Log.Debug("Change visibility");
                await user.Value.Send(await fPacket.ToSend());
            }
        }
    }
}