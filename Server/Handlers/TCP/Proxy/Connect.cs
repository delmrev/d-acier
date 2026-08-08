using System.Buffers.Binary;
using Database;
using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy
{
    public class ConnectMessage : IProxyHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(byte[] data, Session session)
        {
            ReadOnlySpan<byte> span = data.AsSpan();
            var read = Reader.ReadBytes(ref span,"BBI");
            session.game_id = (int)read[2];
            long steamID = 0;
            Reader.ReadBytes(ref span,"IIII");
            steamID = (long)Reader.ReadBytes(ref span,"l")[0];
            var u0 = await DatabaseManager.GetU0BySteamID(steamID);
            byte statusCode = (byte)StatusCode.Success;
            long EugenID = -1;
            if(u0 == null)
            {
                statusCode = (byte)StatusCode.UnknownExternalApiAccount;
            } else
            {
                EugenID = u0.EugenID;
                session.EugenID = u0.EugenID;
                session.Name = u0.Name;
            }
            if(EugenID != -1 && await GlobalManager.Instance.GetSession(EugenID,session.game_id) != null)
            {
                statusCode = (byte)StatusCode.AlreadyInSession;
            }
            var stat = await DatabaseManager.GetData(session.EugenID,session.game_id);
            if(stat.Count == 0)
            {
                await DatabaseManager.CreateAccount(steamID,session.game_id);
            }
            Log.Debug($"Packet: c ->");
            var buffer = Writer.WriteBytes("BQQIBQ", PacketType.CONNECT_SERVER, EugenID, (long)0, 60, statusCode, (long)0);
            byte[] packet = new byte[2+buffer.Length];
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(),(ushort)buffer.Length);
            buffer.CopyTo(packet.AsSpan(2));
            Log.Debug($"Packet: c <-");
            if(statusCode == 0x0){
                await GlobalManager.Instance.RegSession(session, session.game_id);
            }
            await session.Send(packet);
        }
    }
}