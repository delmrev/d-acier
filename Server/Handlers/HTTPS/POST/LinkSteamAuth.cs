using System.Text.Json;
using Database;
using EugnetProtocol.Common.Interfaces;
using NLog;
namespace HTTPS.Methods.POST
{
    public class LinkSteamAuth : IHTTPHandler
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public async Task Process(HTTPRequestOptions request, HTTPResponseOptions response)
        {
            if (!request.Headers.TryGetValue("content-type", out var contentType) || !contentType.Contains("boundary="))
                {
                    Log.Warn("Missing or invalid Content-Type header");
                    response.StatusCode = 400;
                    response.StatusString = "Bad Request";
                    return;
                }

                string boundary = contentType.Split("boundary=")[1].Trim();

                var values = HttpsServer.ParseMultipartFormData(request.BodyBytes, boundary);

                if (values == null || !values.TryGetValue("steamid", out string? value) || !values.TryGetValue("login", out string? login) || !values.TryGetValue("eappid", out string? value1) || !values.TryGetValue("password", out string? password))
                {
                    Log.Warn("Invalid multipart form data");
                    response.StatusCode = 400;
                    response.StatusString = "Bad Request";
                    return;
                }

                if (!long.TryParse(value, out long steamID) || !int.TryParse(value1, out int gameID))
                {
                    response.StatusCode = 400;
                    response.StatusString = "Invalid parameter format";
                    return;
                }
                long eugenID = 0;
                var user = await DatabaseManager.GetU0BySteamID(steamID);

                if (user == null)
                {
                    eugenID = await DatabaseManager.CreateAccount(steamID, 0);
                    if (eugenID == -1)
                    {
                        response.StatusCode = 500;
                        response.StatusString = "Internal Server Error";
                        return;
                    }
                    var info = await DatabaseManager.CreateClientInfo(eugenID);
                    info.Login = login;
                    info.Password = password;
                    await DatabaseManager.UpdateData(info);
                } else
                {
                    eugenID = user.EugenID;
                }
                
                var data = await DatabaseManager.GetData(eugenID, gameID);
                if (data.Count == 0)
                {
                    await DatabaseManager.CreateAccount(steamID, gameID);
                }

                user = await DatabaseManager.GetU0(eugenID);
                if (user != null)
                {
                    user.Name = login;
                    await DatabaseManager.UpdateData(user);
                }

                response.StatusCode = 200;
                response.ContentType = "application/json";
                var jsonResponse = new
                {
                    result = "OK",
                    mmsid = eugenID
                };

                response.Body = JsonSerializer.Serialize(jsonResponse);
                response.StatusCode = 200;
                response.StatusString = "OK";
                response.ContentType = "application/json";
                
                Log.Info($"Account created successfully for SteamID: {steamID}, EugenID: {eugenID}");
        }
    }
}