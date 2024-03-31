using BlockifyLib.Launcher.Minecraft.Mojang;
using BlockifyLib.Launcher.Minecraft.Mojang.Launcher;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Text;

namespace BlockifyLib.Launcher.Minecraft.Auth
{
    public enum LoginResult
    {
        Success, BadRequest, WrongAccount,
        NeedLogin, UnknownError, NoProfile
    }

    public class Login
    {
        public static readonly string DefaultLoginSessionFile = Path.Combine(MinecraftPath.GetOSDefaultPath(), "logintoken.json");

        public Login() : this(DefaultLoginSessionFile) { }

        public Login(string sessionCacheFilePath)
        {
            SessionCacheFilePath = sessionCacheFilePath;
        }

        public string SessionCacheFilePath { get; private set; }
        public bool SaveSession { get; set; } = true;

        private string CreateNewClientToken()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        private Session createNewSession()
        {
            var session = new Session();
            if (SaveSession)
            {
                session.ClientToken = CreateNewClientToken();
                writeSessionCache(session);
            }
            return session;
        }

        private void writeSessionCache(Session session)
        {
            if (!SaveSession) return;
            var directoryPath = Path.GetDirectoryName(SessionCacheFilePath);
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            var json = JsonConvert.SerializeObject(session);
            File.WriteAllText(SessionCacheFilePath, json, Encoding.UTF8);
        }

        public Session ReadSessionCache()
        {
            if (File.Exists(SessionCacheFilePath))
            {
                var fileData = File.ReadAllText(SessionCacheFilePath, Encoding.UTF8);
                try
                {
                    var session = JsonConvert.DeserializeObject<Session>(fileData, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }) ?? new Session();

                    if (SaveSession && string.IsNullOrEmpty(session.ClientToken))
                        session.ClientToken = CreateNewClientToken();

                    return session;
                }
                catch (JsonReaderException) // invalid json
                {
                    return createNewSession();
                }
            }
            else
            {
                return createNewSession();
            }
        }

        private HttpWebResponse mojangRequest(string endpoint, string postdata)
        {
            HttpWebRequest http = WebRequest.CreateHttp(MojangServer.Auth + endpoint);
            http.ContentType = "application/json";
            http.Method = "POST";

            using StreamWriter req = new StreamWriter(http.GetRequestStream());
            req.Write(postdata);
            req.Flush();

            HttpWebResponse res = http.GetResponseNoException();
            return res;
        }

        private LoginResponse parseSession(string json, string? clientToken)
        {
            var job = JObject.Parse(json); //json parse

            var profile = job["selectedProfile"];
            if (profile == null)
                return new LoginResponse(LoginResult.NoProfile, null, null, json);
            else
            {
                var session = new Session
                {
                    AccessToken = job["accessToken"]?.ToString(),
                    UUID = profile["id"]?.ToString(),
                    Username = profile["name"]?.ToString(),
                    UserType = "Mojang",
                    ClientToken = clientToken
                };

                writeSessionCache(session);
                return new LoginResponse(LoginResult.Success, session, null, null);
            }
        }

        private LoginResponse errorHandle(string json)
        {
            try
            {
                JObject job = JObject.Parse(json);

                string error = job["error"]?.ToString() ?? ""; // error type
                string errorMessage = job["message"]?.ToString() ?? ""; // detail error message
                LoginResult result;

                switch (error)
                {
                    case "Method Not Allowed":
                    case "Not Found":
                    case "Unsupported Media Type":
                        result = LoginResult.BadRequest;
                        break;
                    case "IllegalArgumentException":
                    case "ForbiddenOperationException":
                        result = LoginResult.WrongAccount;
                        break;
                    default:
                        result = LoginResult.UnknownError;
                        break;
                }

                return new LoginResponse(result, null, errorMessage, json);
            }
            catch (Exception ex)
            {
                return new LoginResponse(LoginResult.UnknownError, null, ex.ToString(), json);
            }
        }

        public LoginResponse Authenticate(string id, string pw)
        {
            string? clientToken = ReadSessionCache().ClientToken;
            return Authenticate(id, pw, clientToken);
        }

        public LoginResponse Authenticate(string id, string pw, string? clientToken)
        {
            JObject req = new JObject
            {
                { "username", id },
                { "password", pw },
                { "clientToken", clientToken },
                { "agent", new JObject
                    {
                        { "name", "Minecraft" },
                        { "version", 1 }
                    }
                }
            };

            HttpWebResponse resHeader = mojangRequest("authenticate", req.ToString());

            var stream = resHeader.GetResponseStream();
            if (stream == null)
                return new LoginResponse(
                    LoginResult.UnknownError,
                    null,
                    "null response stream",
                    null);

            using StreamReader res = new StreamReader(stream);
            string rawResponse = res.ReadToEnd();
            if (resHeader.StatusCode == HttpStatusCode.OK) // ResultCode == 200
                return parseSession(rawResponse, clientToken);
            else // fail to login
                return errorHandle(rawResponse);
        }

        public LoginResponse TryAutoLogin()
        {
            Session session = ReadSessionCache();
            return TryAutoLogin(session);
        }

        public LoginResponse TryAutoLogin(Session session)
        {
            try
            {
                LoginResponse result = Validate(session);
                if (result.Result != LoginResult.Success)
                    result = Refresh(session);
                return result;
            }
            catch (Exception ex)
            {
                return new LoginResponse(LoginResult.UnknownError, null, ex.ToString(), null);
            }
        }

        public LoginResponse TryAutoLoginFromMojangLauncher()
        {
            var mojangAccounts = MojangLauncherAccounts.FromDefaultPath();
            var activeAccount = mojangAccounts?.GetActiveAccount();

            if (activeAccount == null)
                return new LoginResponse(LoginResult.NeedLogin, null, null, null);

            return TryAutoLogin(activeAccount.ToSession());
        }

        public LoginResponse TryAutoLoginFromMojangLauncher(string accountFilePath)
        {
            var mojangAccounts = MojangLauncherAccounts.FromFile(accountFilePath);
            var activeAccount = mojangAccounts?.GetActiveAccount();

            if (activeAccount == null)
                return new LoginResponse(LoginResult.NeedLogin, null, null, null);

            return TryAutoLogin(activeAccount.ToSession());
        }

        public LoginResponse Refresh()
        {
            Session session = ReadSessionCache();
            return Refresh(session);
        }

        public LoginResponse Refresh(Session session)
        {
            JObject req = new JObject
                {
                    { "accessToken", session.AccessToken },
                    { "clientToken", session.ClientToken },
                    { "selectedProfile", new JObject
                        {
                            { "id", session.UUID },
                            { "name", session.Username }
                        }
                    }
                };

            HttpWebResponse resHeader = mojangRequest("refresh", req.ToString());
            var stream = resHeader.GetResponseStream();
            if (stream == null)
                return new LoginResponse(
                    LoginResult.UnknownError,
                    null,
                    "null response stream",
                    null);

            using StreamReader res = new StreamReader(stream);
            string rawResponse = res.ReadToEnd();

            if ((int)resHeader.StatusCode / 100 == 2)
                return parseSession(rawResponse, session.ClientToken);
            else
                return errorHandle(rawResponse);
        }

        public LoginResponse Validate()
        {
            Session session = ReadSessionCache();
            return Validate(session);
        }

        public LoginResponse Validate(Session session)
        {
            JObject req = new JObject
                {
                    { "accessToken", session.AccessToken },
                    { "clientToken", session.ClientToken }
                };

            HttpWebResponse resHeader = mojangRequest("validate", req.ToString());
            if (resHeader.StatusCode == HttpStatusCode.NoContent) // StatusCode == 204
                return new LoginResponse(LoginResult.Success, session, null, null);
            else
                return new LoginResponse(LoginResult.NeedLogin, null, null, null);
        }

        public void DeleteTokenFile()
        {
            if (File.Exists(SessionCacheFilePath))
                File.Delete(SessionCacheFilePath);
        }

        public bool Invalidate()
        {
            Session session = ReadSessionCache();
            return Invalidate(session);
        }

        public bool Invalidate(Session session)
        {
            JObject job = new JObject
            {
                { "accessToken", session.AccessToken },
                { "clientToken", session.ClientToken }
            };

            HttpWebResponse res = mojangRequest("invalidate", job.ToString());
            return res.StatusCode == HttpStatusCode.NoContent; // 204
        }

        public bool Signout(string id, string pw)
        {
            JObject job = new JObject
            {
                { "username", id },
                { "password", pw }
            };

            HttpWebResponse res = mojangRequest("signout", job.ToString());
            return res.StatusCode == HttpStatusCode.NoContent; // 204
        }
    }

    internal static class HttpWebResponseExt
    {
        public static HttpWebResponse GetResponseNoException(this HttpWebRequest req)
        {
            try
            {
                return (HttpWebResponse)req.GetResponse();
            }
            catch (WebException we)
            {
                HttpWebResponse? resp = we.Response as HttpWebResponse;
                if (resp == null)
                    throw;
                return resp;
            }
        }
    }

    public class LoginResponse
    {
        public LoginResponse(LoginResult result, Session? session, string? errormsg, string? rawresponse)
        {
            Result = result;
            Session = session;
            ErrorMessage = errormsg;
            RawResponse = rawresponse;
        }

        public LoginResult Result { get; private set; }
        public Session? Session { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? RawResponse { get; private set; }

        public bool IsSuccess => Result == LoginResult.Success;
    }
}
