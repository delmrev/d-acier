using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class Invite : IFPacketHandler
    {
        public async Task Process(FPacket fpacket, Session session)
        {
            var read = await Reader.ReadBytes(fpacket.payload,"QQ");
            var user = await GlobalManager.Instance.GetSession((long)read[1],session.game_id);
            if(user == null)
            {
                return;
            }
            var buffer = await Writer.WriteBytes("QQ", (long)read[0], session.EugenID);
            FPacket responce = new(fpacket.channel,(byte)FClientOpcode.Invite,buffer);
            await user.Send(await responce.ToSend());
        }
    }
}