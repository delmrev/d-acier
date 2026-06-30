using Database;
using EugnetProtocol.HTTP.GET;
using EugnetProtocol.HTTP.POST;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

public static class HTTPInputReader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static void ReadTheInputHTTP(HTTPRequestOptions request, ref HTTPResponseOptions response)
    {
        try
        {
            if (request.Method == "POST")
            {
                JObject data = JObject.Parse(request.Headers["Body"]);
                var events = (JArray)data["events"];
                if (events is null || events.Count == 0)
                {
                    Log.Warn("POST request with empty events array");
                    response.StatusCode = 400;
                    response.StatusString = "Bad request";
                }

                var firstObject = (JObject)events[0];
                string eventType = firstObject["event"]?.Value<string>() ?? "undefined";
                Log.Info("POST event type: {0}", eventType);

                switch (eventType)
                {
                    case "start":
                        HTTPEventStart.AcceptTheRequest();
                        ConfigureConfirmJSONResponse(ref response);
                        break;

                    case "hardware_config":
                        HTTPEventHardwareConfig.AcceptTheRequest();
                        ConfigureConfirmJSONResponse(ref response);
                    break;
                    case "login_success":
                        ConfigureConfirmJSONResponse(ref response);
                    break;
                    default:
                        Log.Info($"Unknown POST event type: {eventType}, writing default response");
                        ConfigureConfirmJSONResponse(ref response);
                        break;
                }
            }
            else if (request.Method == "GET")
            {
                if (string.IsNullOrEmpty(request.RequestURL))
                {
                    response.StatusCode = 403;
                    response.StatusString = "Bad request";
                    return;
                }
                Log.Debug($"HTTP GET request : {request.RequestURL}");
                var arguments = request.RequestURL.Split(['_','\\','/','?','&','='],StringSplitOptions.RemoveEmptyEntries);
                switch (arguments[0])
                {
                    case "u0":
                    var tmp = U0.ProcessGETU0(arguments[1]).Result;
                    if(tmp == "Unauthorized"){
                        response.StatusCode = 401;
                        response.StatusString = tmp;
                    } else
                        {
                            ConfigureConfirmJSONResponse(ref response);
                            response.Body = tmp;
                        }
                    break;
                    case string s when s.StartsWith("u") && s.Length > 1 && int.TryParse(s[1..], out int game_id):
                        long EugenID = long.Parse(arguments[1]);
                        var data = DatabaseManager.GetData(EugenID,game_id).Result;
                        if(data.Count == 0)
                        {
                           response.StatusCode = 404;
                           response.StatusString = "Object Not Found";
                        } else {
                            ConfigureConfirmJSONResponse(ref response);
                            response.Body = Ustat.ProcessGETUStat(EugenID,game_id).Result;
                        }
                    break;
                    case "design":
                        response.Body = Design.Process(arguments).Result;
                        response.StatusCode = 200;
                        response.StatusString = "OK";
                        response.ContentType = "application/json";
                    break;
                    case "motd":
                        response.StatusCode = 403;
                        response.StatusString = "Forbidden";
                        response.ContentType = "text/html";
                        response.Body =  Motd.ProcessMotd();
                    break;
                    default:
                        Log.Warn($"Unknown GET key: {arguments[0]}");
                        response.StatusCode = 404;
                        response.StatusString = "Not found";
                        break;
                }
            }
            else
            {
                Log.Warn("Unknown HTTP method: {0}", request.Method);
                response.StatusCode = 405;
                response.StatusString = "Method not allowed";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing HTTP request");
            response.StatusCode = 500;
            response.StatusString = "Internal server error";
        }
    }

    public static void ConfigureConfirmJSONResponse(ref HTTPResponseOptions response)
    {
        response.StatusCode = 200;
        response.StatusString = "OK";
        response.ContentType = "application/json";
        JObject jsonData = new(new JProperty("result", "OK"));
        response.Body = jsonData.ToString(Formatting.None);
    }
}
