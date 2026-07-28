using System.Text.Json;
using Database;
using EugnetProtocol.Common.Interfaces;

namespace EugnetProtocol.HTTP.GET
{
    public class Design : IHTTPHandler
    {
        public async Task Process(HTTPRequestOptions request, HTTPResponseOptions response)
        {
            if (string.IsNullOrEmpty(request.RequestURL))
            {
                response.StatusCode = 403;
                response.StatusString = "Bad request";
                return;
            }
            var options = request.RequestURL.Split(['_','\\','/','?','&','='], StringSplitOptions.RemoveEmptyEntries);
            int gameId = int.Parse(options[1][2..]);
            int startKey = int.Parse(options[6]);
            int offset = startKey > 0 ? startKey-1 : 0;
            var usersList = await DatabaseManager.GetELOList(gameId,offset,int.Parse(options[8]));
            var rows = usersList.Select((user, i) => new
            {
                id = $"u{gameId}_{user.EugenID}",
                key = startKey + i,
                value = $"{user.Value}"
            });

            var output = new
            {
                total_rows = await DatabaseManager.GetEloCount(gameId),
                offset,
                rows
            };

            response.StatusCode = 200;
            response.StatusString = "OK";
            response.ContentType = "application/json";
            response.Body = JsonSerializer.Serialize(output);
        }
    }
}