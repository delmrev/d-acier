using System.Text.Json;
using EugnetProtocol.Common.Interfaces;

namespace HTTPS.Methods.GET
{
    public class ServerStatus : IHTTPHandler
    {
        public async Task Process(HTTPRequestOptions request, HTTPResponseOptions response)
        {   
            var output = new
            {
                proxy_ips = new[] 
                { 
                    "178.32.126.73:21000", 
                    "178.32.126.73:21001" 
                }
            };
            response.Body = JsonSerializer.Serialize(output);
            response.StatusCode = 200;
            response.StatusString = "OK";
            response.ContentType = "application/json";
        }
    }
}