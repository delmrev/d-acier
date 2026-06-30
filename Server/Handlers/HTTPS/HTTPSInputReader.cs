using HTTPS.Methods.POST;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

public static class HTTPSInputReader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task ReadTheInputHTTP(HTTPRequestOptions request, HTTPResponseOptions response)
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

    public static void ConfigureResponseJson(ref HTTPResponseOptions response)
    {
        response.StatusCode = 200;
        response.ContentType = "application/json";
        JObject jsonData = new(new JProperty("result", "OK"));
        response.Body = jsonData.ToString(Formatting.None);
    }
    public static Dictionary<string, string>? ParseMultipartFormData(string body,string boundary)
    {
        if(body is null)
        {
            return null;
        }
        Dictionary<string, string> values = new();
        body = body.Trim();
        if (body.EndsWith("--"))
        {
            body = body.TrimEnd('-', '\r', '\n');
        }
        var blocks = body.Split(boundary, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < blocks.Length; i++)
        {
            Log.Debug(blocks[i]);
            var val = blocks[i].Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            var index = val[0].IndexOf(';')+1;
            var keyStr = val[0][index..];
            var l = keyStr.IndexOf('=')+1;
            var key = keyStr[l..];
            key = key.Trim('"');
            var dataType = val[0][..index];
            var KeyValueDatatype = dataType.Split(':');
            KeyValueDatatype[1] = KeyValueDatatype[1].Trim();
            if (KeyValueDatatype[1].EndsWith(";"))
            {
                KeyValueDatatype[1] = KeyValueDatatype[1].TrimEnd(';');
            }
            if(KeyValueDatatype[1] == "form-data")
            {
                Log.Debug(key);
                Log.Debug(val[1]);
                values.Add(key,val[1]);
            }
        }
        return values;
    }
}