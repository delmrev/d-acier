using System.Buffers.Binary;
using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class UserData : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket fPacket, Session session)
        {
            var data = await Reader.ReadBytes(fPacket.payload,"Q");
            if(session.currentRoom != null)
            {
                var user = session.currentRoom.Users
                    .Where(r => (long)data[0] == r.Value.EugenID)
                    .Select(r => r.Value)
                    .FirstOrDefault();
                if(user != null)
                {
                    Log.Info($"Data user {session.EugenID} -> {user.EugenID}");
                    BinaryPrimitives.WriteInt64BigEndian(fPacket.payload.AsSpan(0, 8), session.EugenID);
                    await user.Send(await fPacket.ToSend());
                } else
                {
                    Log.Error($"User dont found: {data[0]}");
                }
            } else
            {
                Log.Error("Try to send message to user but lobby is null");
            }
        }
    }
}
