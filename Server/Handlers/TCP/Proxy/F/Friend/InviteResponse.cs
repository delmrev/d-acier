using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class InviteResponce : IFPacketHandler
    {
        public async Task Process(FPacket fpacket, Session session)
        {
            var read = Reader.ReadBytes(fpacket.payload,"QQa");
            var user = await GlobalManager.Instance.GetSession((long)read[1],session.game_id);
            if(user == null)
            {
                return;
            }
            var buffer = Writer.WriteBytes("QQa", (long)read[0], session.EugenID, read[2]);
            FPacket responce = new(fpacket.channel,(byte)FClientOpcode.InviteResponse,buffer);
            await user.Send(responce.ToBytes());
        }
    }
}