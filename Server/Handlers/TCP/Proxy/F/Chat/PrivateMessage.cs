using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class PrivateMessage : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket fPacket, Session session)
        {
            var values = await Reader.ReadBytes(fPacket.payload,"I");
            var friend = await GlobalManager.GetSession((long)values[0],session.game_id);
            if(friend == null)
            {
                Log.Warn("Try to send message to user who offline");
                return;
            }
            await friend.Send(await fPacket.ToSend());
        }
    }
}