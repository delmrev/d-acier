using System.Buffers.Binary;
using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class ConnectStartP2P : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket fPacket, Session session)
        {
            var read = Reader.ReadBytes(fPacket.payload, "Q");
            if(session.currentRoom != null)
            {
                Session? user = null;
                long eugid = (long)read[0];
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
                    if (GlobalManager.Instance.Config != null && GlobalManager.Instance.Config.Logging.EnableDebug)
                    {
                        Log.Info($"Connect message: {session.EugenID} -> {user.EugenID}");
                    }
                    BinaryPrimitives.WriteInt64BigEndian(fPacket.payload.AsSpan(0, 8), session.EugenID);
                    await user.Send(fPacket.ToBytes());
                } else
                {
                    Log.Error($"User dont found: {read[0]}");
                }
            }  else
            {
                Log.Error("Try to send message to user but lobby is null");
            }
        }
    } 
}