using System;

namespace EIS
{
    public class SessionInfo
    {
        public string AuthenticationToken { get; private set; }
        public string SessionSecretKey { get; private set; }
        public DateTime TokenExpiry { get; private set; }

        public SessionInfo() {
            TokenExpiry = DateTime.MinValue;
        }
        public SessionInfo(string authToken, string sessionSecretKey, DateTime tokenExpiry)
        {
            AuthenticationToken = authToken;
            SessionSecretKey = sessionSecretKey;
            TokenExpiry = tokenExpiry;
        }

        public void Update(SessionInfo sessionInfo)
        {
            AuthenticationToken = sessionInfo.AuthenticationToken;
            SessionSecretKey = sessionInfo.SessionSecretKey;
            TokenExpiry = sessionInfo.TokenExpiry;
        }
    }
}
