using Database;
using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.F
{
    public class GetFriendEugenID : IFPacketHandler
    {
        public async Task Process(FPacket fPacket, Session session)
        {
            ReadOnlySpan<byte> span = fPacket.payload.AsSpan();
            var read = Reader.ReadBytes(ref span, "H");
            ushort length = (ushort)read[0];
            var requests = new (byte unknownByte, string steamIdStr)[length];
            for (int i = 0; i < length; i++)
            {
                read = Reader.ReadBytes(ref span, "BS");
                requests[i] = ((byte)read[0], (string)read[1]);
            }
            List<byte> buffer = [];
            ushort responce_length = 0;
            foreach (var (_, steamIdStr) in requests)
            {
                var user = await DatabaseManager.GetU0BySteamID(long.Parse(steamIdStr));
                if (user == null)
                {
                    continue;
                }

                buffer.AddRange(Writer.WriteBytes("aQBS", true, user.EugenID, 0x0, steamIdStr));
                responce_length++;
            }
            buffer.InsertRange(0, Writer.WriteBytes("H", responce_length));
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.BM_FRIEND_GET_EUGNET_ID, [.. buffer]);
            await session.Send(response.ToBytes());
        }
    }
}