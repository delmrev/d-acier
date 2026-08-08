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
            int count;
            (string? str, string? valStr)[] items;

            {
                ReadOnlySpan<byte> span = fPacket.payload.AsSpan();
                var buffer = Reader.ReadBytes(ref span, "BII");
                count = (int)buffer[1];
                items = new (string?, string?)[count];

                for (int i = 0; i < count; i++)
                {
                    var tmp = Reader.ReadBytes(ref span, "SS");
                    items[i] = (tmp[0] as string, tmp[1] as string);
                }
            }
            var stats = await DatabaseManager.GetData(session.EugenID, session.game_id);

            for (int i = 0; i < count; i++)
            {
                var (str, valStr) = items[i];

                if (str != null)
                {
                    if (str == "_rev" || str == "@name" || str == "@avatar")
                    {
                        await UpdateU0(session, fPacket);
                        return;
                    }

                    var value = int.Parse(valStr!);

                    if (stats.Count == 0)
                    {
                        stats.Add(str, value);
                        await DatabaseManager.ChangeOrAddStat(session.EugenID, session.game_id, str, value);
                        continue;
                    }

                    if (!stats.TryAdd(str, value))
                    {
                        stats[str] = value;
                    }

                    await DatabaseManager.ChangeOrAddStat(session.EugenID, session.game_id, str, value);
                }
                else
                {
                    Log.Error("Empty string!");
                }
            }
            byte[] bytes = new byte[32]; // Well, well, Eugens thanks for #v
            List<byte> buf = [.. Writer.WriteBytes("QQsQQLL", session.EugenID, -1, "", -1, -1, session.game_id, stats.Count)];
            foreach (var stat in stats)
            {
                var strbytes = Encoding.UTF8.GetBytes(stat.Key);
                for (int j = 0; j < strbytes.Length; j++)
                {
                    bytes[j] = strbytes[j];
                }
                buf.AddRange(bytes);
                Array.Clear(bytes, 0, bytes.Length);
                buf.AddRange(Writer.WriteBytes("S", $"{stat.Value}"));
            }
            FPacket responce = new(fPacket.channel, (byte)FClientOpcode.StatsResult, Writer.WriteBytes("aa", true, false));
            await session.Send(responce.ToBytes());
            responce = new(fPacket.channel, (byte)FClientOpcode.Stats, [.. buf]);
            await session.Send(responce.ToBytes());
        }

        private static async Task UpdateU0(Session session, FPacket fPacket)
        {
            int count;
            (string? str, string? valStr)[] items;
            {
                ReadOnlySpan<byte> span = fPacket.payload.AsSpan();
                var buffer = Reader.ReadBytes(ref span, "BII");
                count = (int)buffer[1];
                items = new (string?, string?)[count];

                for (int i = 0; i < count; i++)
                {
                    var tmp = Reader.ReadBytes(ref span, "SS");
                    items[i] = (tmp[0] as string, tmp[1] as string);
                }
            }
            var stats = await DatabaseManager.GetU0(session.EugenID);
            Type type = typeof(u0);
            var propertiesCache = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(p => p.Name.ToLower(), p => p);

            for (int i = 0; i < count; i++)
            {
                var (str, valStr) = items[i];
                if (str != null)
                {
                    str = str.Replace("@", "").ToLower();
                    if (!propertiesCache.TryGetValue(str, out var value))
                    {
                        Log.Error($"Dont exist value: {str}");
                        continue;
                    }
                    value.SetValue(stats, valStr);
                }
                else
                {
                    Log.Error("Empty string!");
                }
            }
            await DatabaseManager.UpdateData(stats);
            byte[] bytes = new byte[32]; // Well, well, Eugens thanks for #v
            var fields = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(f => f.Name != "SteamID" && f.Name != "EugenID" && f.Name != "Rev");

            List<byte> buf = [.. Writer.WriteBytes("QQsQQLL", session.EugenID, -1, "", -1, -1, 0, fields.Count())];
            foreach (var field in fields)
            {
                var strbytes = Encoding.UTF8.GetBytes($"@{field.Name.ToLower()}");
                for (int i = 0; i < strbytes.Length; i++)
                {
                    bytes[i] = strbytes[i];
                }
                buf.AddRange(bytes);
                Array.Clear(bytes, 0, bytes.Length);
                buf.AddRange(Writer.WriteBytes("S", $"{field.GetValue(stats)}"));
            }
            FPacket response = new(fPacket.channel, (byte)FClientOpcode.StatsResult, Writer.WriteBytes("aa", true, true));
            await session.Send(response.ToBytes());
            response = new(fPacket.channel, (byte)FClientOpcode.Stats, [.. buf]);
            await session.Send(response.ToBytes());
        }
    }
}