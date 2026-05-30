using Database.Tables;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace HTTPS.Methods.POST
{
    public class AddEugnetAccount
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static async Task Process(HTTPRequestOptions request, HTTPResponseOptions response)
        {
            var ContentTypeHeader = request.Headers["Content-Type"].Trim();
            if(ContentTypeHeader.Contains(';'))
                {
                    Log.Debug(ContentTypeHeader);
                    var index = ContentTypeHeader.IndexOf(';')+1;
                    string BoundaryString = ContentTypeHeader[index..].Trim();
                    index = BoundaryString.IndexOf('=')+1;
                    if(index == -1)
                    {
                        response.StatusCode = 400;
                        response.StatusString = "Bad request";
                            return;
                        }
                        string boundary = "--";
                        boundary += BoundaryString[index..];
                        boundary += "\r\n";
                        var values = HTTPSInputReader.ParseMultipartFormData(request.Headers["Body"], boundary);
                        if(values is null)
                        {
                            response.StatusCode = 400;
                            response.StatusString = "Bad request";
                            return;
                        }
                            var steamID = values["extuserid"];
                            var nickname = values["nickname"];
                            var gameID = values["eappid"];
                            var login = values["email"];
                            if(await DatabaseManager.GetU0BySteamID(long.Parse(steamID)) is  not u0 user){
                                var EugenID = await DatabaseManager.CreateAccount(long.Parse(steamID),0);
                                if(EugenID == -1)
                                {
                                    return;
                                }
                                user = await DatabaseManager.GetU0(EugenID);
                                if(user is null)
                                {
                                    response.StatusCode = 500;
                                    response.StatusString = "Internal server error";
                                    return;
                                }
                                } else
                                {
                                    response.StatusCode = 200;
                                    response.StatusString = "OK";
                                    response.ContentType = "application/json";
                                    JObject jsonwData = new(new JProperty("extapi-request-failed"));
                                    response.Body = jsonwData.ToString(Formatting.None);
                                }
                            user.Name = nickname;
                            user.Login = login;
                            DatabaseManager.UpdateData(user);
                            response.StatusCode = 200;
                            response.StatusString = "OK";
                            response.ContentType = "application/json";
                            JObject jsonData = new(new JProperty("result", "OK"),
                            new JProperty("mmsid", user.EugenID));
                            response.Body = jsonData.ToString(Formatting.None);
                        } else
                        {
                            Log.Error("Dont have boundary in /api/v1/AddEugnetExternalApiAccount");
                        }
        }
    }
}