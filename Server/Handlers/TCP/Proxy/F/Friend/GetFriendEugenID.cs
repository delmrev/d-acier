using Database;
using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetFriendEugenID : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            using MemoryStream stream = new(fPacket.payload);
            using BinaryReader reader = new(stream);
            var read = await Reader.ReadBytes(reader,"H");
            ushort length = (ushort)read[0];
            List<byte> buffer = new();
            ushort responce_length = 0;
            for (int i = 0; i < length; i++)
            {
                read = await Reader.ReadBytes(reader,"BS");
                var user = await DatabaseManager.GetU0BySteamID(long.Parse((string)read[1]));
                if(user == null)
                {
                    continue;
                }
                buffer.AddRange(await Writer.WriteBytes("aQBS",true,user.EugenID,0x0,$"{read[1]}"));
                responce_length++;
            }
            buffer.InsertRange(0,await Writer.WriteBytes("H",responce_length));
            FPacket response = new(fPacket.channel,(byte)FClientOpcode.BM_FRIEND_GET_EUGNET_ID,buffer);
            await session.Send(await response.ToSend());
        }
    }
}