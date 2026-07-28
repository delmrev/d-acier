using System.Text.Json.Nodes;
using Database;
using EugnetProtocol.Common.Interfaces;
namespace EugnetProtocol.HTTP.GET
{
    public class U0 : IHTTPHandler
    {
        public async Task Process(HTTPRequestOptions request, HTTPResponseOptions response)
        {
            if (string.IsNullOrEmpty(request.RequestURL))
            {
                response.StatusCode = 403;
                response.StatusString = "Bad request";
                return;
            }
            var arguments = request.RequestURL.Split(['_','\\','/','?','&','='], StringSplitOptions.RemoveEmptyEntries);
            var data = await DatabaseManager.GetU0(long.Parse(arguments[1]));
            if(data is null)
            {
                response.StatusCode = 401;
                response.StatusString = "Unauthorized";
                return;
            }
            var jsonData = new JsonObject
            {
                ["_id"] = $"u0_{arguments[1]}",
                ["_rev"] = data.Rev?.ToString(),
                ["@name"] = data?.Name,
                ["@avatar"] = data?.Avatar
            };

            response.Body = jsonData.ToJsonString();
            response.StatusCode = 200;
            response.StatusString = "OK";
            response.ContentType = "application/json";
        }
    }
}