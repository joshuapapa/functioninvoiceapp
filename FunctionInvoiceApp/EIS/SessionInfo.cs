namespace EIS
{
    public class SessionInfo
    {
        public readonly string AuthenticationToken;
        public readonly string SessionSecretKey;

        public SessionInfo(string authToken, string sessionSecretKey)
        {
            AuthenticationToken = authToken;
            SessionSecretKey = sessionSecretKey;
        }
    }
}
