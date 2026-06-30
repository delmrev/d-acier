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
            var usersList = await DatabaseManager.GetELOList(gameId,int.Parse(options[6]),int.Parse(options[8]));
            JObject output = new(
                new JProperty("total_rows", await DatabaseManager.GetEloCount(gameId)),
                new JProperty("offset", 0),
                new JProperty("rows", JArray.FromObject(usersList))
            );
            return output.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}