using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class CurrentState : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {  
            var (success, output) = await Reader.TryReadBytes(fPacket.payload, "IIBBHIII");
            session.unk_1 = (int)output[0];
            session.unk_2 = (int)output[1];
            if(session.currentRoom is not null)
            {
                byte[] result = fPacket.payload
                            .Concat(BitConverter.GetBytes(session.EugenID))
                            .Concat(fPacket.payload.Skip(1))
                            .ToArray();
                FPacket response = new(fPacket.channel, (byte)FClientOpcode.BM_FRIEND_PRESENCE, [..result]);
                foreach(var user in session.currentRoom.Users)
                {
                    await user.Value.Send(await response.ToSend());
                }
            }
        }
    }
}