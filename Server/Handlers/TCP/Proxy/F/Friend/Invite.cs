using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class Invite : IFPacketHandler
    {
        public async Task Process(FPacket fpacket, Session session)
        {
            var read = Reader.ReadBytes(fpacket.payload,"QQ");
            var user = await GlobalManager.Instance.GetSession((long)read[1],session.game_id);
            if(user == null)
            {
                return;
            }
            var buffer = Writer.WriteBytes("QQ", (long)read[0], session.EugenID);
            FPacket responce = new(fpacket.channel,(byte)FClientOpcode.Invite,buffer);
            await user.Send(responce.ToBytes());
        }
    }
}