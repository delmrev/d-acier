using EugnetProtocol.Common.Interfaces;
using EugnetProtocol.HTTP.GET;
using EugnetProtocol.HTTP.POST;
using HTTPS.Methods.GET;
using HTTPS.Methods.POST;
using NLog;

public class HTTPManager
{
    private readonly Dictionary<string, IHTTPHandler> _handlers = new();
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public HTTPManager()
    {
        _handlers.Add("/api/v1/LinkSteamAuth", new LinkSteamAuth());
        _handlers.Add("/api/v1/AddEugnetExternalApiAccount",new AddEugnetAccount());
        _handlers.Add("/api/v1/steeldivision", new SteelDivision());
        _handlers.Add("/api/v1/serverstatus", new ServerStatus());
        _handlers.Add("/api/v1/automatch_scenarios", new AutomatchScenarios());
        _handlers.Add("motd", new Motd());
        _handlers.Add("design", new Design());
        _handlers.Add("u0",new U0());
        _handlers.Add("u", new Ustat());
    }
    public async Task Handle(HTTPRequestOptions request, HTTPResponseOptions response)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RequestURL))
            {
                response.StatusCode = 403;
                response.StatusString = "Bad request";
                return;
            }
            if(GetHandler(request, out var handler))
            {
                await handler.Process(request, response);
            } else
            {
                response.StatusCode = 404;
                response.StatusString = "Not found";
                Log.Warn($"Not found the method: {request.RequestURL}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing HTTPS request");
            response.StatusCode = 500;
            response.StatusString = "Internal server error";
        }
    }
    private bool GetHandler(HTTPRequestOptions request,out IHTTPHandler? result)
    {
        if(_handlers.TryGetValue(request.RequestURL ?? "undefined", out var handler))
        {
            result = handler;
            return true;
        }
        if (request.Method == "GET")
        {
            var arguments = request.RequestURL.Split(['_','\\','/','?','&','='], StringSplitOptions.RemoveEmptyEntries);
            if(_handlers.TryGetValue(arguments[0] ?? "undefined", out handler))
            {
                result = handler;
                return true;
            }
            if(arguments[0].StartsWith('u') && arguments[0].Length > 1)
            {
                result = _handlers["u"];
                return true;
            }
            arguments = request.RequestURL.Split(['?','&','='], StringSplitOptions.RemoveEmptyEntries);
            if(_handlers.TryGetValue(arguments[0] ?? "undefined", out handler))
            {
                result = handler;
                return true;
            }
            
        }
        result = null;
        return false;
    }
}