using System.Text.Json;

public class JsonReader
{
    public static T ReadJson<T>()
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        using (FileStream fs = new("./config/config.json", FileMode.Open))
        {
            return JsonSerializer.Deserialize<T>(fs,options) ?? throw new NullReferenceException();
        }
    }
}

