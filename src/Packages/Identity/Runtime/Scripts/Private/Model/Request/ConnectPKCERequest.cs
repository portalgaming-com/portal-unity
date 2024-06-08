using System;

namespace Portal.Identity.Model
{

    [Serializable]
    internal class ConnectPKCERequest
    {
        public string authorizationCode;
        public string state;
    }
}

