using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Text;

namespace BlockifyLib.BlockifyLib.Launcher.Minecraft
{
    public enum LoginResult
    {
        Success, BadRequest, WrongAccount,
        NeedLogin, UnknownError
    }

    public class Login
    {
        public static readonly string DefaultLoginSessionFile =
            Minecraft.DefaultPath + "\\logintoken.json";

        public Login() : this(DefaultLoginSessionFile) { }

        public Login(string tokenpath)
        {
            TokenFile = tokenpath;
        }

        public string TokenFile;
        public bool SaveSession = true;

        private void WriteLogin(Session result) =>
            WriteLogin(result.Username, result.AccessToken, result.UUID, result.ClientToken);

        /* Save Login Session */
        private void WriteLogin(string us, string se, string id, string ct)
        {
            if (!SaveSession) return;

            JObject jobj = new JObject() {
                {"username", us},
                {"session", se},
                {"uuid", id },
                {"clientToken", ct}
            }; // create session json

            Directory.CreateDirectory(Path.GetDirectoryName(TokenFile));
            File.WriteAllText(TokenFile, jobj.ToString(), Encoding.UTF8);
        }

        public Session GetLocalToken()
        {
            Session session;

            if (!File.Exists(TokenFile))
            {
                var ClientToken = Guid.NewGuid().ToString().Replace("-", "");

                session = Session.createEmpty();
                session.ClientToken = ClientToken;

                WriteLogin(session);
            }
            else
            {
                var filedata = File.ReadAllText(TokenFile, Encoding.UTF8);
                try
                {
                    JObject job = JObject.Parse(filedata);
                    session = new Session()
                    {
                        AccessToken = job["session"]?.ToString(),
                        UUID = job["uuid"]?.ToString(),
                        Username = job["username"]?.ToString(),
                        ClientToken = job["clientToken"]?.ToString()
                    };
                }
                catch (Newtonsoft.Json.JsonReaderException)
                {
                    DeleteTokenFile();
                    session = GetLocalToken();
                }
            }

            return session;
        }

        private HttpWebResponse mojangRequest(string endpoint, string postdata)
        {
            HttpWebRequest http = WebRequest.CreateHttp("https://authserver.mojang.com/" + endpoint);
            http.ContentType = "application/json";
            http.Method = "POST";
            using (StreamWriter req = new StreamWriter(http.GetRequestStream()))
            {
                req.Write(postdata);
                req.Flush();
            }
            return http.GetResponseNoException();
        }

        public Session Authenticate(string id, string pw)
        {
            Session result = new Session();

            string ClientToken = GetLocalToken().ClientToken;

            JObject job = new JObject
            {
                { "username", id },
                { "password", pw },
                { "clientToken", ClientToken },

                { "agent", new JObject
                    {
                        { "name", "Minecraft" },
                        { "version", 1 }
                    }
                }
            };

            HttpWebResponse resHeader = mojangRequest("authenticate", job.ToString());

            using (StreamReader res = new StreamReader(resHeader.GetResponseStream()))
            {
                string Response = res.ReadToEnd();

                result.ClientToken = ClientToken;

                if (resHeader.StatusCode == HttpStatusCode.OK)
                {
                    JObject jObj = JObject.Parse(Response);
                    result.AccessToken = jObj["accessToken"].ToString();
                    result.UUID = jObj["selectedProfile"]["id"].ToString();
                    result.Username = jObj["selectedProfile"]["name"].ToString();

                    WriteLogin(result);
                    result.Result = LoginResult.Success;
                }
                else
                {
                    var json = JObject.Parse(Response);

                    var error = json["error"]?.ToString(); // error type
                    result._RawResponse = Response;
                    result.Message = json["message"]?.ToString() ?? ""; // detail error message

                    switch (error)
                    {
                        case "Method Not Allowed":
                        case "Not Found":
                        case "Unsupported Media Type":
                            result.Result = LoginResult.BadRequest;
                            break;
                        case "IllegalArgumentException":
                        case "ForbiddenOperationException":
                            result.Result = LoginResult.WrongAccount;
                            break;
                        default:
                            result.Result = LoginResult.UnknownError;
                            break;
                    }
                }

                return result;
            }
        }

        public Session TryAutoLogin()
        {
            Session result = Validate();
            if (result.Result != LoginResult.Success)
                result = Refresh();

            return result;
        }

        public Session Refresh()
        {
            Session result = new Session();

            try
            {
                Session session = GetLocalToken();
                JObject req = new JObject
                {
                    { "accessToken", session.AccessToken },
                    { "clientToken", session.ClientToken },
                    { "selectedProfile", new JObject()
                        {
                            { "id", session.UUID },
                            { "name", session.Username }
                        }
                    }
                };

                HttpWebResponse resHeader = mojangRequest("refresh", req.ToString());
                using (StreamReader res = new StreamReader(resHeader.GetResponseStream()))
                {
                    string response = res.ReadToEnd();
                    result._RawResponse = response;
                    JObject job = JObject.Parse(response);

                    result.AccessToken = job["accessToken"].ToString();
                    result.AccessToken = job["accessToken"].ToString();
                    result.UUID = job["selectedProfile"]["id"].ToString();
                    result.Username = job["selectedProfile"]["name"].ToString();
                    result.ClientToken = session.ClientToken;

                    WriteLogin(result);
                    result.Result = LoginResult.Success;
                }
            }
            catch
            {
                result.Result = LoginResult.UnknownError;
            }

            return result;
        }

        public Session Validate()
        {
            Session result = new Session();
            try
            {
                Session session = GetLocalToken();
                JObject job = new JObject
                {
                    { "accessToken", session.AccessToken },
                    { "clientToken", session.ClientToken }
                };

                HttpWebResponse resHeader = mojangRequest("validate", job.ToString());
                using (StreamReader res = new StreamReader(resHeader.GetResponseStream()))
                    if (resHeader.StatusCode == HttpStatusCode.NoContent) // StatusCode == 204
                    {
                        result.Result = LoginResult.Success;
                        result.AccessToken = session.AccessToken;
                        result.UUID = session.UUID;
                        result.Username = session.Username;
                        result.ClientToken = session.ClientToken;
                    }
                    else
                        result.Result = LoginResult.NeedLogin;
            }
            catch
            {
                result.Result = LoginResult.UnknownError;
            }

            return result;
        }

        public void DeleteTokenFile()
        {
            if (File.Exists(TokenFile))
                File.Delete(TokenFile);
        }

        public bool Invalidate()
        {
            Session session = GetLocalToken();

            JObject job = new JObject
            {
                { "accessToken", session.AccessToken },
                { "clientToken", session.ClientToken }
            };

            return mojangRequest("invalidate", job.ToString())
                .StatusCode == HttpStatusCode.OK;
        }

        public bool Signout(string id, string pw)
        {
            JObject job = new JObject
            {
                { "username", id },
                { "password", pw }
            };

            return mojangRequest("signout", job.ToString())
                .StatusCode == HttpStatusCode.NoContent;
        }
    }

    public static class HttpWebResponseExt
    {
        public static HttpWebResponse GetResponseNoException(this HttpWebRequest req)
        {
            try
            {
                return (HttpWebResponse)req.GetResponse();
            }
            catch (WebException we)
            {
                HttpWebResponse resp = we.Response as HttpWebResponse;
                if (resp == null)
                    throw;
                return resp;
            }
        }
    }
}
