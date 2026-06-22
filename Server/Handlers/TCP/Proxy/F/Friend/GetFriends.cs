using Database;
using EugnetProtocol.Common.Interfaces;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetFriends : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            using MemoryStream stream = new(fPacket.payload);
            using BinaryReader reader = new(stream);
            var settings = await Reader.ReadBytes(reader,"H");
            int count = 0;
            List<byte> buffer = new();
            for (int i = 0; i < (ushort)settings[0]; i++)
            {
                var buf = await Reader.ReadBytes(reader, "BS");
                var user = await DatabaseManager.GetU0BySteamID(long.Parse((string)buf[1]));
                if(user == null)
                {
                    continue;
                } else
                {
                    count++;
                    buffer.AddRange(await Writer.WriteBytes("BQBS",0x1,user.EugenID,0x0,(string)buf[1]));
                }
            }
            buffer.InsertRange(0,await Writer.WriteBytes("H",count));
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.NETWORK_CHANNEL_FRIEND_ADD_EXTERNAL_API_FRIEND,buffer);
            await session.Send(await response.ToSend());
        }
    }
}