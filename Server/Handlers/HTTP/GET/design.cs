using Database;
using Newtonsoft.Json.Linq;
using NLog;

namespace EugnetProtocol.HTTP.GET
{
    public class Design
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static async Task<string> Process(string[] options)
        {
            int gameId = int.Parse(options[1][2..]);
            int startKey = int.Parse(options[6]);
            int offset = startKey > 0 ? startKey-1 : 0;
            var usersList = await DatabaseManager.GetELOList(gameId,offset,int.Parse(options[8]));
            JArray array = [];
            for (int i = 0; i < usersList.Count; i++)
            {
                JObject obj = new(
                    new JProperty("id",$"u{gameId}_{usersList[i].EugenID}"),
                    new JProperty("key", startKey + i),
                    new JProperty("value",$"{usersList[i].Value}")
                );
                array.Add(obj);
            }
            JObject output = new(
                new JProperty("total_rows", await DatabaseManager.GetEloCount(gameId)),
                new JProperty("offset", offset),
                new JProperty("rows", array)
            );
            return output.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}