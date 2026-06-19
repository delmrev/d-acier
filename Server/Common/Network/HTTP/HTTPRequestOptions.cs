public class HTTPRequestOptions
{
    public string? Method;
    public string? RequestURL;
    public string? RequestVersion;
    public Dictionary<string,string> Headers = new();
}