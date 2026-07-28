using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.HTTP.POST
{
    public class SteelDivision : IHTTPHandler
    {
        public async Task Process(HTTPRequestOptions request, HTTPResponseOptions response)
        {
            response.StatusCode = 200;
            response.StatusString = "OK";
            response.ContentType = "application/json";
            response.Body = """{"result":"OK"}""";
        }
    }
}