using System.Buffers.Binary;
using Database;
using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy
{
    public class RedDragonLogin : IProxyHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(byte[] IncomeData, Session session) 
        {
            ReadOnlySpan<byte> span = IncomeData.AsSpan();
            var data = Reader.ReadBytes(ref span,"BIISSBS");
            long steamID = long.Parse((string)data[6]);
            var user = await DatabaseManager.GetU0BySteamID(steamID);
            byte statusCode = (byte)StatusCode.Success;
            int gameid = (int)data[2];
            if (user is not null)
            {
                var clientInfo = await DatabaseManager.GetClientInfoByEugenID(user.EugenID);
                clientInfo ??= await DatabaseManager.CreateClientInfo(user.EugenID);
                Log.Debug($"Packet: c ->");
                if(string.IsNullOrEmpty(clientInfo.Password))
                {
                    clientInfo.Password = (string)data[4];
                    await DatabaseManager.UpdateData(user);
                } else if(clientInfo.Password != (string)data[4])
                {
                    statusCode = (byte)StatusCode.IncorrectIdentification;

                    var buf = Writer.WriteBytes("BQQIBQ", PacketType.CONNECT_SERVER, -1, (long)0, 60, statusCode, (long)0);
                    byte[] packet = new byte[2+buf.Length];
                    BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(),(ushort)buf.Length);
                    buf.CopyTo(packet.AsSpan(2));
                    await session.Send(packet);
                    return;
                }
                var stat = await DatabaseManager.GetData(user.EugenID,gameid);
                if(stat.Count == 0)
                {
                    await DatabaseManager.CreateAccount(steamID,gameid);
                }
                session.game_id = gameid;
                session.EugenID = user.EugenID;
                session.Name = user.Name;
                var buffer = Writer.WriteBytes("BQQIBQ", PacketType.CONNECT_SERVER, user.EugenID, (long)0, 60, statusCode, (long)0);
                byte[] packet_2 = new byte[2+buffer.Length];
                BinaryPrimitives.WriteUInt16BigEndian(packet_2.AsSpan(),(ushort)buffer.Length);
                buffer.CopyTo(packet_2.AsSpan(2));
                await session.Send(packet_2);
                Log.Debug($"Packet: c <-");
            } else
            {
                Log.Debug($"Packet: c ->");
                var newEugid = await DatabaseManager.CreateAccount(steamID,0);
                if(newEugid == -1){
                    Log.Error("RedDragonLogin: EugenID is -1");
                    return;
                }
                user = await DatabaseManager.GetU0(newEugid);
                if(user is null)
                {
                    Log.Error("RedDragonLogin: user is null");
                    return;
                }
                var clientInfo = await DatabaseManager.CreateClientInfo(newEugid);
                user.Name = (string)data[3];
                clientInfo.Password = (string)data[4];
                await DatabaseManager.UpdateData(user);
                await DatabaseManager.CreateAccount(steamID,gameid);
                session.game_id = gameid;
                session.EugenID = newEugid;
                session.Name = (string)data[3];
                var buffer = Writer.WriteBytes("BQQIBQ", PacketType.CONNECT_SERVER, newEugid, (long)0, 60, statusCode, (long)0);
                byte[] packet = new byte[2+buffer.Length];
                BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(),(ushort)buffer.Length);
                buffer.CopyTo(packet.AsSpan(2));
                await session.Send(packet);
                Log.Debug($"Packet: c <-");
            }
        }
    }
}