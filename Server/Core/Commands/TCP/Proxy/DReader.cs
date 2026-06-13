using Newtonsoft.Json.Linq;
using NLog;

public class DReader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task ProcessPacket(byte[] packet, Session session)
    {
        DPacket dPacket = new(packet);

        Log.Info("Processing d packet! ");
        
        try
        {
            switch (dPacket.command)
            {
                case "mms":
                    Log.Info("Handling MMS command");
                    await BasicResponse(dPacket, session, 1);
                    await MMS.Process(session);
                    if (!session.channels.Contains(dPacket.channel))
                    {
                        session.channels.Add(dPacket.channel);
                    }
                    break;

                case "friend":
                    Log.Info("Handling Friend command");
                    await BasicResponse(dPacket, session, 2);
                    await Friend.Process(dPacket, session, session.Server);
                    break;

                case "Relay.1":
                    Log.Info("Handling Relay.1 command");
                    await BasicResponse(dPacket, session, 3);
                    if (!session.channels.Contains(dPacket.channel))
                    {
                        session.channels.Add(dPacket.channel);
                    }
                    break;
                case "ath":
                    await BasicResponse(dPacket,session,4);
                break;
                default:
                    Log.Warn("Unknown DPacket command {0}", dPacket.command);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing DPacket command {0}", dPacket.command);
        }
    }

    public static string GetMMSJson(Session session)
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
            new JProperty("MinPlayerToUseSlDedi", "8")
        );
        return jsonData.ToString(Newtonsoft.Json.Formatting.None);
    }
    private static int Get_Game_ID(int EugenAppID) => EugenAppID switch
    {
        24 => 251060,
        27 => 919640,
        _ => -1
    };

    private static async Task BasicResponse(DPacket dPacket, Session session, int index)
    {
        var buffer = await Writer.WriteBytes("BII", (byte)'d', dPacket.channel, index);
        Log.Debug("Sending basic response for command {0}, ack {1}, index {2}: {3}",
            dPacket.command,
            dPacket.channel,
            index,
            BitConverter.ToString(buffer.ToArray())
        );
        await ProxyReader.FinalizePacket(buffer, session);
    }
}
