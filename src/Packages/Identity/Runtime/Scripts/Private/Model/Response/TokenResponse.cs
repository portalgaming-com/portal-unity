using System;

namespace Portal.Identity.Model
{
    [Serializable]
    public class TokenResponse
    {
        public string accessToken;
        public string refreshToken;
        public string idToken;
        public string tokenType;
        public int expiresIn;
    }
}