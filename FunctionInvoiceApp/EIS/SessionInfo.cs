using System;

namespace EIS
{
    public class SessionInfo
    {
        public readonly string AuthenticationToken;
        public readonly string SessionSecretKey;
        public readonly DateTime TokenExpiry;

        public SessionInfo(string authToken, string sessionSecretKey, DateTime tokenExpiry)
        {
            AuthenticationToken = authToken;
            SessionSecretKey = sessionSecretKey;
            TokenExpiry = tokenExpiry;
        }
    }
}
