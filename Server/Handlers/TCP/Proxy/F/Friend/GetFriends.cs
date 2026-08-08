using Database;
using EugnetProtocol.Common.Interfaces;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetFriends : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            ReadOnlySpan<byte> span = fPacket.payload.AsSpan();
            var settings = Reader.ReadBytes(ref span, "H");
            ushort totalItems = (ushort)settings[0];
            List<string> steamIds = new(totalItems);
            for (int i = 0; i < totalItems; i++)
            {
                var buf = Reader.ReadBytes(ref span, "BS");
                steamIds.Add((string)buf[1]);
            }
            int count = 0;
            List<byte> buffer = new();
            foreach (var steamIdStr in steamIds)
            {
                var user = await DatabaseManager.GetU0BySteamID(long.Parse(steamIdStr));
                if (user == null)
                {
                    continue;
                }
                count++;
                buffer.AddRange(Writer.WriteBytes("BQBS", 0x1, user.EugenID, 0x0, steamIdStr));
            }
            buffer.InsertRange(0, Writer.WriteBytes("H", count));
            FPacket response = new(
                fPacket.channel, 
                (byte)FClientOpcode.NETWORK_CHANNEL_FRIEND_ADD_EXTERNAL_API_FRIEND, 
                [.. buffer]
            );
            await session.Send(response.ToBytes());
        }
    }
}