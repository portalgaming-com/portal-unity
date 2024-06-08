using System;
using Portal.Identity.Model;

namespace Portal.Identity.Model
{
    [Serializable]
    internal class InitRequestWithRedirectUri
    {
        public string clientId;
        public string environment;
        public string redirectUri;
        public string logoutRedirectUri;
    }

    [Serializable]
    internal class InitRequest
    {
        public string clientId;
        public string environment;

    }
}
