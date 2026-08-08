using System.Text.Json;
using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.TCP.Proxy.D
{
    public class MMS : IDPacketHandler
    {
        public async Task Process(DPacket dPacket, Session session)
        {
            await session.Send(Writer.WriteBytes("HBII",9,(byte)'d', dPacket.channel, 1));
            List<byte> buffer = [..Writer.WriteBytes("IBQS", 71, 0x00, session.EugenID, GetMMSJson(session))];
            buffer.AddRange(new byte[4]);
            byte[] key = new byte[128];
            Random random = new();
            random.NextBytes(key);
            buffer.AddRange(key);
            FPacket fResponce = new(1, (byte)FClientOpcode.MMS_MSG_INIT, [.. buffer]);
            await session.Send(fResponce.ToBytes());
            session.channels.TryAdd("mms", 1);
        }
        private string GetMMSJson(Session session)
        {
            var jsonData = new
            {
                type = "game",
                name = "steel division 2",
                free = "0",
                AllowEugNetLogin = "1",
                AllowSteamLogin = "1",
                RequireSteamVAC = "0",
                SteamAppId = Get_Game_ID(session.game_id).ToString(),
                StatsURL = "http://178.32.126.73:80/",
                paradoxAccount = "0",
                MinPlayerToUseSlDedi = "20"
            };

            return JsonSerializer.Serialize(jsonData);
        }
        private int Get_Game_ID(int EugenAppID) => EugenAppID switch
        {
            24 => 251060, // Wargame : Red Dragon
            25 => 572410, // Steel Division : Normandy 44
            27 => 919640, // SD2
            29 => 1611600, // WARNO
            _ => -1
        };
    }
}