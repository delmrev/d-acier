using Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace EugnetProtocol.HTTP.GET
{
    public class Ustat
    {
        public static async Task<string> ProcessGETUStat(long ID,int gameID)
        {
            var data = await DatabaseManager.GetData(ID,gameID);
            var user = await DatabaseManager.GetU0(ID);
            JObject jsonData = new(
                new JProperty("_id", $"u{gameID}_{ID}"),
                new JProperty("_rev", $"{user?.Rev}")
            );
            foreach(var value in data)
            {
                jsonData.Add(new JProperty($"{value.Key}", $"{value.Value}"));
            }
            return jsonData.ToString(Formatting.None);
        }
    }
}