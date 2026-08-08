using System.Text.Json;

public class StunConfig
{
    public string Default {get; set;}
    public string[] Address { get; set;} = [];
    public static StunConfig Load(string path = "./Configuration/Stun.json")
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        using FileStream fs = new(path, FileMode.Open);

        return JsonSerializer.Deserialize<StunConfig>(fs, options)
               ?? throw new NullReferenceException("Failed to load configuration.");
    }
}