using Database;
using Database.Tables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace HTTPS.Methods.POST
{
    public class LinkExternalApiAuth
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static async Task Process(HTTPRequestOptions request, HTTPResponseOptions response)
        {
            var ContentTypeHeader = request.Headers["Content-Type"].Trim();
            if(ContentTypeHeader.Contains(';'))
            {
                var index = ContentTypeHeader.IndexOf(';');
                var BoundaryString = ContentTypeHeader[index..];
                index = BoundaryString.IndexOf(':');
                string boundary = BoundaryString[index..];
                boundary += "\r\n";
                Log.Debug($"{boundary}");
                var values = HTTPSInputReader.ParseMultipartFormData(request.Headers["Body"], boundary);
                if(values is null)
                {
                    response.StatusCode = 400;
                    response.StatusString = "Bad request";
                    return;
                }
                var steamID = values["extuserid"];
                var password = values["password"];
                var login = values["login"];
                u0? user = await  DatabaseManager.GetU0BySteamID(long.Parse(steamID));
                if(user is not null && user.Login == login && user.SteamID == long.Parse(steamID))
                    {
                        response.ContentType = "application/json";
                        JObject jsonData = new(new JProperty("result", "OK"));
                        response.Body = jsonData.ToString(Formatting.None);
                        }
                    } else
                    {
                        Log.Error("Dont have boundary in /api/v1/LinkExternalApiAuth");
                    }
        }
    }
}