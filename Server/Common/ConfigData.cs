using System.Text.Json;

public class ConfigData
{
    public ServerConfig Server { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public SSLConfig SSL { get; set; } = new();

    public static ConfigData Load(string path = "./Configuration/Server.json")
    {
        var options = new JsonSerializerOptions
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        using FileStream fs = new(path, FileMode.Open);

        return JsonSerializer.Deserialize<ConfigData>(fs, options)
               ?? throw new NullReferenceException("Failed to load configuration.");
    }
}

public class ServerConfig
{
    public string Address { get; set; } = "0.0.0.0";
    public ushort HTTP { get; set; }
    public ushort HTTPS { get; set; }
    public ushort STUN { get; set; }
    public ushort TCP { get; set; }

    public int TCPKeepAliveTime {get; set;}
    public int TCPKeepAliveInterval {get; set;}
    public int TCPKeepAliveRetryCount {get; set;}

    public int HTTPKeepAliveTime {get; set;}
    public int HTTPKeepAliveInterval {get; set;}
    public int HTTPKeepAliveRetryCount {get; set;}
}

public class LoggingConfig
{
    public bool EnableDebug { get; set; }
}

public class SSLConfig
{
    public string Certificate { get; set; } = string.Empty;
}