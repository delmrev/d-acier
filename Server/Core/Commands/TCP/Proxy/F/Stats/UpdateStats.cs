using System.Reflection;
using System.Text;
using Database.Tables;
using NLog;

public class UpdateStats
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public static async Task Process(FPacket fPacket, Session session)
    {
        using MemoryStream stream = new(fPacket.payload);
        using BinaryReader reader = new(stream);
        var buffer = await Reader.ReadBytes(reader, "BII");
        var stats = await DatabaseManager.GetData(session.EugenID, session.game_id);
        Type type = typeof(Stat);
        for (int i = 0; i < (int)buffer[1]; i++)
        {
            var tmp = await Reader.ReadBytes(reader, "SS");
            if (tmp[0] != null && tmp[0] is string str)
            {
                str = str.Replace("@", "");
                var value = type.GetProperty(str);
                if (value is null)
                {
                    if (str == "_rev" || str == "name" || str == "avatar")
                    {
                        await UpdateU0(reader,session,fPacket);
                        return;
                    }
                    else
                    {
                        Log.Error($"Dont exist value: {str}");
                        continue;
                    }
                }
                value.SetValue(stats, int.Parse((string)tmp[1]));
            }
            else
            {
                Log.Error("Empty string!");
            }
        }
        DatabaseManager.UpdateData(stats);
        byte[] bytes = new byte[32]; // Well, well, Eugens thanks for #v
        var fields = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(f => f.PropertyType == typeof(int) && f.Name != "GameID");
        var buf = await Writer.WriteBytes("QQsQQLL", session.EugenID, -1, "", -1, -1, session.game_id, fields.Count());
        foreach (var field in fields)
        {
            var strbytes = Encoding.UTF8.GetBytes($"@{field.Name}");
            for (int j = 0; j < strbytes.Length; j++)
            {
                bytes[j] = strbytes[j];
            }
            buf.AddRange(bytes);
            Array.Clear(bytes, 0, bytes.Length);
            buf.AddRange(await Writer.WriteBytes("S", $"{field.GetValue(stats)}"));
        }
        FResponse responce = new(fPacket.channel, FClientOpcode.StatsResult, await Writer.WriteBytes("aa", true, false));
        await ProxyReader.FinalizePacket(await responce.ToSend(), session);
        responce = new(fPacket.channel, FClientOpcode.Stats, buf);
        await ProxyReader.FinalizePacket(await responce.ToSend(), session);
    }
    private static async Task UpdateU0(BinaryReader reader, Session session, FPacket fPacket)
    {
        reader.BaseStream.Position = 0;
        var buffer = await Reader.ReadBytes(reader, "BII");
        var stats = await DatabaseManager.GetU0(session.EugenID);
        Type type = typeof(u0);
        for (int i = 0; i < (int)buffer[1]; i++)
        {
            var tmp = await Reader.ReadBytes(reader,"SS");
            if(tmp[0] != null && tmp[0] is string str)
            {
                str = str.Replace("@", "");
                var value = type.GetProperty(str);
                if(value is null)
                {
                    Log.Error($"Dont exist value: {str}");
                    continue;
                }
                value.SetValue(stats,int.Parse((string)tmp[1]));
            } else
            {
                Log.Error("Empty string!");
            }
        }
        DatabaseManager.UpdateData(stats);
        byte[] bytes = new byte[32]; // Well, well, Eugens thanks for #v
        var fields = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(f => f.PropertyType == typeof(int) && f.Name != "SteamID" && 
        f.Name != "Login" && f.Name != "Password" && f.Name != "EugenID");
        var buf = await Writer.WriteBytes("QQsQQLL",session.EugenID,-1,"",-1,-1,session.game_id, fields.Count());
        foreach(var field in fields)
        {
            var strbytes = Encoding.UTF8.GetBytes($"@{field.Name}");
            for (int i = 0; i < strbytes.Length; i++)
            {
                bytes[i] = strbytes[i];
            }
            buf.AddRange(bytes);
            Array.Clear(bytes, 0, bytes.Length);
            buf.AddRange(await Writer.WriteBytes("S",$"{field.GetValue(stats)}"));
        } 
        FResponse responce = new(fPacket.channel,FClientOpcode.StatsResult, await Writer.WriteBytes("aa", true, false));
        await ProxyReader.FinalizePacket(await responce.ToSend(),session);
        responce = new(fPacket.channel,FClientOpcode.Stats, buf);
        await ProxyReader.FinalizePacket(await responce.ToSend(),session);
    }
}