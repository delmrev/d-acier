using EugnetProtocol.Common.Interfaces;
using Newtonsoft.Json.Linq;

namespace EugnetProtocol.TCP.Proxy.D
{
    public class MMS : IDPacketHandler
    {
        public async Task Process(DPacket dPacket, Session session)
        {
            var buffer = await Writer.WriteBytes("BII", (byte)'d', dPacket.channel, 1);
            buffer.InsertRange(0,await Writer.WriteBytes("H",buffer.Count));
            await session.Send(buffer);
            buffer = await Writer.WriteBytes("IBQS", 71, 0x00, session.EugenID, GetMMSJson(session));
            buffer.AddRange(new byte[4]);
            byte[] key = new byte[128];
            Random random = new();
            random.NextBytes(key);
            buffer.AddRange(key);
            FPacket fResponce = new(1, (byte)FClientOpcode.MMS_MSG_INIT, buffer);
            await session.Send(await fResponce.ToSend());
            session.channels.TryAdd("mms", 1);
        }
        private string GetMMSJson(Session session)
        {
            JObject jsonData = new(
                new JProperty("type", "game"),
                new JProperty("name", "steel division 2"),
                new JProperty("free", "0"),
                new JProperty("AllowEugNetLogin", "1"),
                new JProperty("AllowSteamLogin", "1"),
                new JProperty("RequireSteamVAC", "0"),
                new JProperty("SteamAppId", $"{Get_Game_ID(session.game_id)}"),
                new JProperty("StatsURL", $"http://178.32.126.73:80/"),
                new JProperty("paradoxAccount", "0"),
                new JProperty("MinPlayerToUseSlDedi", "20")
            );
            return jsonData.ToString(Newtonsoft.Json.Formatting.None);
        }
        private int Get_Game_ID(int EugenAppID) => EugenAppID switch
        {
            24 => 251060, // Wargame
            27 => 919640, // SD2
            29 => 1611600, // WARNO
            _ => -1
        };
    }
}