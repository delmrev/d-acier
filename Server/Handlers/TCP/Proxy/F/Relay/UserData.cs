using System.Buffers.Binary;
using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class UserData : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private ConfigData config = GlobalManager.Instance.Data;
        public async Task Process(FPacket fPacket, Session session)
        {
            var data = await Reader.ReadBytes(fPacket.payload,"Q");
            long eugid = (long)data[0];
            if(session.currentRoom != null)
            {
                Session? user = null;
                foreach (var users in session.currentRoom.Users)
                {
                    if (users.Value.EugenID == eugid)
                    {
                        user = users.Value;
                        break;
                    }
                }
                if(user != null)
                {
                    if (config != null && config.Logging.EnableDebug)
                    {
                        Log.Info($"Data user {session.EugenID} -> {user.EugenID}");
                    }
                    BinaryPrimitives.WriteInt64BigEndian(fPacket.payload.AsSpan(0, 8), session.EugenID);
                    fPacket.channel = user.channels["Relay.1"];
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
