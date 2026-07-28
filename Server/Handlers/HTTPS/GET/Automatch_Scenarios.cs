using System.Text.Json;
using EugnetProtocol.Common.Interfaces;
using NLog;

namespace HTTPS.Methods.GET
{
    public class AutomatchScenarios : IHTTPHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(HTTPRequestOptions request, HTTPResponseOptions response)
        {   
            var arguments = request.RequestURL.Split(['_','\\','/','?','&','='], StringSplitOptions.RemoveEmptyEntries);
            var confPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Configuration/Automatch",
                $"{arguments[6]}.json"
            );
            try
            {
                response.Body = File.ReadAllText(confPath);
            }
            catch (Exception ex)
            {
                Log.Warn(ex);
                response.Body = "[]";
            }
            response.StatusCode = 200;
            response.StatusString = "OK";
            response.ContentType = "application/json";
        }
    }
}