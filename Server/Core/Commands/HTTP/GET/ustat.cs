using System.Reflection;
using Database.Tables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
namespace HTTP.Metods.GET
{
    public class Ustat
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static async Task<string> ProcessGETU27(int gameID,long ID)
        {
            var data = await DatabaseManager.GetData(ID,gameID);
            JObject jsonData = new(
                new JProperty("_id", $"{gameID}_{ID}"),
                new JProperty("_rev", "4-addde246c7ec754c170df411553c64b9")
            );
            Type type = typeof(Stat);
            var fields = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(f => f.PropertyType == typeof(int) && f.Name != "GameID" && f.Name != "EugenID");
            foreach(var field in fields)
            {
                jsonData.Add(new JProperty($"@{field.Name}", $"{field.GetValue(data)}"));
            }
            return jsonData.ToString(Formatting.None);
        }
    }
}