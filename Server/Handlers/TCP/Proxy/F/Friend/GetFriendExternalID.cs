using Database;
using EugnetProtocol.Common.Interfaces;
using NLog;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetFriendExternalID : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket fPacket, Session session)
        {
            var read = await Reader.ReadBytes(fPacket.payload,"Q");
            var user = await DatabaseManager.GetU0((long)read[0]);
            if(user == null)
            {
                Log.Error("Try to add user who is null");
                return;
            }
            var buffer = await Writer.WriteBytes("aQBS",true,user.EugenID,0x00,user.SteamID);
            FPacket response = new(fPacket.channel,(byte)FClientOpcode.BM_FRIEND_GET_EXTERNAL_ID,buffer);
            await session.Send(await response.ToSend());
        }
    }
}