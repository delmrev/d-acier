using System.Diagnostics;
using System.Reflection;
using System.Text;
using Database;
using Database.Tables;
using EugnetProtocol.Common.Interfaces;
using NLog;
namespace EugnetProtocol.TCP.Proxy.F
{
    public class UpdateStats : IFPacketHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(FPacket fPacket, Session session)
        {
            using MemoryStream stream = new(fPacket.payload);
            using BinaryReader reader = new(stream);
            var buffer = await Reader.ReadBytes(reader, "BII");
            var stats = await DatabaseManager.GetData(session.EugenID, session.game_id);
            for (int i = 0; i < (int)buffer[1]; i++)
            {
                var tmp = await Reader.ReadBytes(reader, "SS");
                if (tmp[0] != null && tmp[0] is string str)
                {
                    if (str == "_rev" || str == "@name" || str == "@avatar")
                    {
                        await UpdateU0(reader,session,fPacket);
                        return;
                    }
                    var value = int.Parse((string)tmp[1]);
                    if(stats.Count == 0)
                    {
                        stats.Add(str, value);
                        await DatabaseManager.ChangeOrAddStat(session.EugenID,session.game_id,str,value);
                        continue;
                    }
                    if (!stats.TryAdd(str, value))
                    {
                        stats[str] = value;
                    }
                    await DatabaseManager.ChangeOrAddStat(session.EugenID,session.game_id,str,value);
                }
                else
                {
                    Log.Error("Empty string!");
                }
            }
            byte[] bytes = new byte[32]; // Well, well, Eugens thanks for #v
            var buf = await Writer.WriteBytes("QQsQQLL", session.EugenID, -1, "", -1, -1, session.game_id, stats.Count);
            foreach (var stat in stats)
            {
                var strbytes = Encoding.UTF8.GetBytes(stat.Key);
                for (int j = 0; j < strbytes.Length; j++)
                {
                    bytes[j] = strbytes[j];
                }
                buf.AddRange(bytes);
                Array.Clear(bytes, 0, bytes.Length);
                buf.AddRange(await Writer.WriteBytes("S", $"{stat.Value}"));
            }
            FPacket responce = new(fPacket.channel, (byte)FClientOpcode.StatsResult, await Writer.WriteBytes("aa", true, false));
            await session.Send(await responce.ToSend());
            responce = new(fPacket.channel, (byte)FClientOpcode.Stats, buf);
            await session.Send(await responce.ToSend());
        }
        private static async Task UpdateU0(BinaryReader reader, Session session, FPacket fPacket)
        {
            reader.BaseStream.Position = 0;
            var buffer = await Reader.ReadBytes(reader, "BII");
            var stats = await DatabaseManager.GetU0(session.EugenID);
            Type type = typeof(u0);
            var propertiesCache = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name.ToLower(), p => p);

            for (int i = 0; i < (int)buffer[1]; i++)
            {
                var tmp = await Reader.ReadBytes(reader,"SS");
                if(tmp[0] != null && tmp[0] is string str)
                {
                    str = str.Replace("@", "").ToLower();

                    if(!propertiesCache.TryGetValue(str, out var value))
                    {
                        Log.Error($"Dont exist value: {str}");
                        continue;
                    }
                    value.SetValue(stats, (string)tmp[1]);
                } else
                {
                    Log.Error("Empty string!");
                }
            }
            await DatabaseManager.UpdateData(stats);
            byte[] bytes = new byte[32]; // Well, well, Eugens thanks for #v
            var fields = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(f => f.PropertyType == typeof(int) && f.Name != "SteamID" && f.Name != "EugenID");
            var buf = await Writer.WriteBytes("QQsQQLL",session.EugenID,-1,"",-1,-1,session.game_id, fields.Count());
            foreach(var field in fields)
            {
                Log.Debug("S");
                var strbytes = Encoding.UTF8.GetBytes($"@{field.Name}");
                for (int i = 0; i < strbytes.Length; i++)
                {
                    bytes[i] = strbytes[i];
                }
                buf.AddRange(bytes);
                Array.Clear(bytes, 0, bytes.Length);
                buf.AddRange(await Writer.WriteBytes("S",$"{field.GetValue(stats)}"));
            } 
            FPacket responce = new(fPacket.channel,(byte)FClientOpcode.StatsResult, await Writer.WriteBytes("aa", true, false));
            await session.Send(await responce.ToSend());
            responce = new(fPacket.channel,(byte)FClientOpcode.Stats, buf);
            await session.Send(await responce.ToSend());
        }
    }
}