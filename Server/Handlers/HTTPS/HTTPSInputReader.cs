using System.Text;
using HTTPS.Methods.POST;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

public static class HTTPSInputReader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task ReadTheInputHTTPS(HTTPRequestOptions request, HTTPResponseOptions response)
    {
        try
        {
            if (request.Method == "POST")
            {
                switch (request.RequestURL)
                {
                    case "/api/v1/AddEugnetExternalApiAccount":
                        await AddEugnetAccount.Process(request, response);
                        break;
                    case "/api/v1/LinkSteamAuth":
                        await LinkSteamAuth.Process(request, response);
                    break;
                    default:
                        response.StatusCode = 404;
                        response.StatusString = "Not found";
                        Log.Warn($"Not found the method: {request.RequestURL}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing HTTPS request");
            response.StatusCode = 500;
            response.StatusString = "Internal server error";
        }
    }

    public static void ConfigureResponseJson(HTTPResponseOptions response)
    {
        response.StatusCode = 200;
        response.ContentType = "application/json";
        JObject jsonData = new(new JProperty("result", "OK"));
        response.Body = jsonData.ToString(Formatting.None);
    }
    public static Dictionary<string, string> ParseMultipartFormData(byte[]? bodyBytes, string boundary)
    {
        var values = new Dictionary<string, string>();
        if (bodyBytes == null || bodyBytes.Length == 0) return values;

        byte[] boundaryBytes = Encoding.UTF8.GetBytes("--" + boundary);
        ReadOnlySpan<byte> span = bodyBytes.AsSpan();

        try
        {
            while (true)
            {
                int boundaryIndex = span.IndexOf(boundaryBytes);
                if (boundaryIndex == -1) break;

                span = span[(boundaryIndex + boundaryBytes.Length)..];

                if (span.Length >= 2 && span[0] == '-' && span[1] == '-')
                    break;

                if (span.Length >= 2 && span[0] == '\r' && span[1] == '\n')
                    span = span[2..];

                int headersEnd = span.IndexOf("\r\n\r\n"u8);
                if (headersEnd == -1) break;

                var headersSpan = span.Slice(0, headersEnd);
                string headersString = Encoding.UTF8.GetString(headersSpan);

                var dataSpan = span[(headersEnd + 4)..];
                
                int nextBoundary = dataSpan.IndexOf(boundaryBytes);
                if (nextBoundary == -1) break;

                var valueSpan = dataSpan[..nextBoundary];
                if (valueSpan.Length >= 2 && valueSpan[^2] == '\r' && valueSpan[^1] == '\n')
                {
                    valueSpan = valueSpan[..^2];
                }

                string nameKey = ExtractNameFromHeaders(headersString);
                
                if (!string.IsNullOrEmpty(nameKey))
                {
                    string valueStr = Encoding.UTF8.GetString(valueSpan);
                    values[nameKey] = valueStr;
                    Log.Debug($"Parsed Form Field: {nameKey} = {valueStr}");
                }

                span = dataSpan; 
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error parsing multipart/form-data");
        }

        return values;
    }
    private static string ExtractNameFromHeaders(string headers)
    {
        int nameIdx = headers.IndexOf("name=\"", StringComparison.OrdinalIgnoreCase);
        if (nameIdx == -1) return string.Empty;
        
        nameIdx += 6;
        int endIdx = headers.IndexOf('"', nameIdx);
        if (endIdx == -1) return string.Empty;
        
        return headers[nameIdx..endIdx];
    }
}