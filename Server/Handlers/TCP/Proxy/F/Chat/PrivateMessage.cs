using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class PrivateMessage : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket fPacket, Session session)
        {
            var values = await Reader.ReadBytes(fPacket.payload,"QS");
            var friend = await GlobalManager.Instance.GetSession((long)values[0],session.game_id);
            if(friend == null)
            {
                Log.Warn("Try to send message to user who offline");
                return;
            }
            var buffer = await Writer.WriteBytes("QS",session.EugenID,(string)values[1]);
            FPacket response = new(fPacket.channel,(byte)FClientOpcode.BM_FRIEND_CHAT,buffer);
            await friend.Send(await response.ToSend());
        }
    }
}