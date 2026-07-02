using System.Text;
using Database;
using EugnetProtocol.HTTP.GET;
using EugnetProtocol.HTTP.POST;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

public static class HTTPInputReader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public static async Task ReadTheInputHTTP(HTTPRequestOptions request, HTTPResponseOptions response)
    {
        try
        {
            if (request.Method == "POST")
            {
                if (request.BodyBytes == null || request.BodyBytes.Length == 0)
                {
                    Log.Warn("POST request with empty body");
                    response.StatusCode = 400;
                    response.StatusString = "Bad request";
                    return;
                }

                string bodyText = Encoding.UTF8.GetString(request.BodyBytes);
                JObject data = JObject.Parse(bodyText);
                var events = (JArray?)data["events"];
                
                if (events is null || events.Count == 0)
                {
                    Log.Warn("POST request with empty events array");
                    response.StatusCode = 400;
                    response.StatusString = "Bad request";
                    return;
                }

                var firstObject = (JObject)events[0];
                string eventType = firstObject["event"]?.Value<string>() ?? "undefined";
                Log.Info("POST event type: {0}", eventType);

                switch (eventType)
                {
                    case "start":
                        HTTPEventStart.AcceptTheRequest();
                        ConfigureConfirmJSONResponse(response);
                        break;

                    case "hardware_config":
                        HTTPEventHardwareConfig.AcceptTheRequest();
                        ConfigureConfirmJSONResponse(response);
                        break;
                        
                    case "login_success":
                        ConfigureConfirmJSONResponse(response);
                        break;
                        
                    default:
                        Log.Info($"Unknown POST event type: {eventType}, writing default response");
                        ConfigureConfirmJSONResponse(response);
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
                var arguments = request.RequestURL.Split(['_','\\','/','?','&','='], StringSplitOptions.RemoveEmptyEntries);
                
                if (arguments.Length == 0)
                {
                    response.StatusCode = 400;
                    response.StatusString = "Bad request";
                    return;
                }

                switch (arguments[0])
                {
                    case "u0":
                        if (arguments.Length > 1)
                        {
                            var tmp = await U0.ProcessGETU0(arguments[1]);
                            if(tmp == "Unauthorized")
                            {
                                response.StatusCode = 401;
                                response.StatusString = tmp;
                            } 
                            else
                            {
                                ConfigureConfirmJSONResponse(response);
                                response.Body = tmp;
                            }
                        }
                        break;
                        
                    case string s when s.StartsWith("u") && s.Length > 1 && int.TryParse(s[1..], out int game_id):
                        if (arguments.Length > 1 && long.TryParse(arguments[1], out long EugenID))
                        {
                            var data = await DatabaseManager.GetData(EugenID, game_id);
                            if(data.Count == 0)
                            {
                               response.StatusCode = 404;
                               response.StatusString = "Object Not Found";
                            } 
                            else 
                            {
                                ConfigureConfirmJSONResponse(response);
                                response.Body = await Ustat.ProcessGETUStat(EugenID, game_id);
                            }
                        }
                        break;
                        
                    case "design":
                        response.Body = await Design.Process(arguments);
                        response.StatusCode = 200;
                        response.StatusString = "OK";
                        response.ContentType = "application/json";
                        break;
                        
                    case "motd":
                        response.StatusCode = 403;
                        response.StatusString = "Forbidden";
                        response.ContentType = "text/html";
                        response.Body = Motd.ProcessMotd();
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

    public static void ConfigureConfirmJSONResponse(HTTPResponseOptions response)
    {
        response.StatusCode = 200;
        response.StatusString = "OK";
        response.ContentType = "application/json";
        JObject jsonData = new(new JProperty("result", "OK"));
        response.Body = jsonData.ToString(Formatting.None);
    }
}
