public class ConfigData
{
    public string? ip { get; set; }
    public ushort HTTPPort {get; set;}
    public ushort STUNPort {get; set;}
    public ushort HTTPSPort {get; set;}
    public ushort TCPPort {get; set;}
    public bool EnableDebug {get; set;}
}