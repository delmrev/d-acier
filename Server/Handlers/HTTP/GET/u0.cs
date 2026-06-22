using Database;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace EugnetProtocol.HTTP.GET
{
    public class U0
    {
        public static async Task<string> ProcessGETU0(string ID)
        {
            var data = await DatabaseManager.GetU0(long.Parse(ID));
            if(data is null)
            {
                return "Unauthorized";
            }
            JObject jsonData = new(
                new JProperty("_id", $"u0_{ID}"),
                new JProperty("_rev", "4-def22e51543f0d06ed42d91c7488d310"),
                new JProperty("@name", $"{data?.Name}"),
                new JProperty("@avatar", $"{data?.Avatar}")
            );
            return jsonData.ToString(Formatting.None);
        }
    }
}