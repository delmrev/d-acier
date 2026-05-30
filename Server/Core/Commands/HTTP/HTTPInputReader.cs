using HTTP.Metods.GET;
using HTTP.Metods.POST;
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
                        Log.Warn("Unknown POST event type: {0}", eventType);
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
                }

                Log.Debug($"HTTP GET request : {request.RequestURL}");

                string gameID = "";
                string id = "";
                bool getId = false;
                for (int i = 1; i < request.RequestURL?.Length; i++)
                {
                    if ((request.RequestURL[i] == '_' ||request.RequestURL[i] == '/') && !getId)
                    {
                        getId = true;
                    }
                    else if (getId)
                    {
                        id += request.RequestURL[i];
                    }
                    else
                    {
                        gameID += request.RequestURL[i];
                    }
                }

                Log.Info($"GET gameID: {gameID}, id: {id}");

                switch (gameID)
                {
                    case "u0":
                    var tmp = U0.ProcessGETU0(id).Result;
                    if(tmp == "Unauthorized"){
                        response.StatusCode = 401;
                        response.StatusString = tmp;
                    } else
                        {
                            ConfigureConfirmJSONResponse(ref response);
                            response.Body = tmp;
                        }
                    break;
                    case "u27":
                        var newGameID = int.Parse(gameID[1..]);
                        long ID = long.Parse(id);
                        var data = DatabaseManager.GetData(ID,newGameID).Result;
                        if(data is null)
                        {
                           response.StatusCode = 404;
                           response.StatusString = "Object Not Found";
                        } else {
                            ConfigureConfirmJSONResponse(ref response);
                            response.Body = Ustat.ProcessGETU27(newGameID,ID).Result;
                        }
                    break;
                    case "motd":
                        response.StatusCode = 403;
                        response.StatusString = "Forbidden";
                        response.ContentType = "text/html";
                        response.Body =  Motd.ProcessMotd();
                    break;
                    default:
                        Log.Warn($"Unknown GET key: {gameID}");
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
