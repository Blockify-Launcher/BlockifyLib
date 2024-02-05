namespace BlockifyLib.Launcher.Minecraft.Auth
{
    public class Session
    {
        public string Username { get; internal set; }
        public string AccessToken { get; internal set; }
        public string UUID { get; internal set; }
        public string ClientToken { get; internal set; }

        public LoginResult Result { get; internal set; }
        public string Message { get; internal set; }

        public string _RawResponse { get; internal set; }

        public static Session GetOfflineSession(string username)
        {
            Session login = new Session()
            {
                Username = username,
                AccessToken = "access_token",
                UUID = "user_uuid",
                Result = LoginResult.Success,
                Message = "",
                ClientToken = ""
            };
            return login;
        }

        internal static Session createEmpty()
        {
            Session session = new Session()
            {
                Username = "",
                AccessToken = "",
                UUID = "",
                ClientToken = ""
            };
            return session;
        }
    }
}
