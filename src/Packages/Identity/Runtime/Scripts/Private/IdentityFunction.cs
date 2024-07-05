namespace Portal.Identity
{
    public static class IdentityFunction
    {
        public const string INIT = "init";
        public const string INIT_DEVICE_FLOW = "initDeviceFlow";
        public const string REAUTHENTICATE = "reauthenticate";
        public const string AUTHENTICATE_PKCE = "authenticatePKCE";
        public const string GET_PKCE_AUTH_URL = "getPKCEAuthUrl";
        public const string AUTHENTICATE_CONFIRM_CODE = "authenticateConfirmCode";
        public const string GET_ACCESS_TOKEN = "getAccessToken";
        public const string GET_ID_TOKEN = "getIdToken";
        public const string LOGOUT = "logout";
        public const string REQUEST_WALLET_SESSION_KEY = "requestWalletSessionKey";
        public const string EXECUTE_TRANSACTION = "executeTransaction";
    }
}
