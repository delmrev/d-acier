using System.Text.Json;
using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.HTTP.GET
{
    public class Motd : IHTTPHandler
    {
        public async Task Process(HTTPRequestOptions request, HTTPResponseOptions response)
        {
            response.StatusCode = 200;
            response.StatusString = "OK";
            response.ContentType = "text/html";
            response.Body = "<html><body><h1>403 Forbidden</h1>\n Request forbidden by administrative rules. \n </body></html> \n";
        }
    }
}