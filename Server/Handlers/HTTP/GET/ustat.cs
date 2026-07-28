using System.Text.Json.Nodes;
using Database;
using EugnetProtocol.Common.Interfaces;
namespace EugnetProtocol.HTTP.GET
{
    public class Ustat : IHTTPHandler
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
            if(int.TryParse(arguments[0][1..], out int gameID) && long.TryParse(arguments[1], out long ID))
            {
                var data = await DatabaseManager.GetData(ID,gameID);
                if(data.Count == 0)
                {
                    response.StatusCode = 404;
                    response.StatusString = "Object Not Found";
                    return;
                } 
                var user = await DatabaseManager.GetU0(ID);

                var jsonData = new JsonObject
                {
                    ["_id"] = $"u{gameID}_{ID}",
                    ["_rev"] = user?.Rev?.ToString()
                };

                foreach (var value in data)
                {
                    jsonData[value.Key] = value.Value.ToString();
                }
                response.Body = jsonData.ToJsonString();
                response.StatusCode = 200;
                response.StatusString = "OK";
                response.ContentType = "application/json";
            } else
            {
                throw new Exception();
            }
        }
    }
}