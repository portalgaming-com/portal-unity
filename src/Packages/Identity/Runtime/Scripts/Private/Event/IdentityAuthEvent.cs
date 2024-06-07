namespace Portal.Identity.Event
{
    public delegate void OnAuthEventDelegate(IdentityAuthEvent authEvent);

    public enum IdentityAuthEvent
    {
        #region Device Code Authorization

        /// <summary>
        /// Started the login process using Device Code Authorization flow
        /// </summary>
        LoggingIn,
        /// <summary>
        /// Failed to log in using Device Code Authorization flow
        /// </summary>
        LoginFailed,
        /// <summary>
        /// Successfully logged in using Device Code Authorization flow
        /// </summary>
        LoginSuccess,
        /// <summary>
        /// Opening Identity login in an external browser for login via Device Code Authorization flow
        /// </summary>
        LoginOpeningBrowser,
        /// <summary>
        /// Waiting for user to complete Identity login in an external browser
        /// </summary>
        PendingBrowserLogin,

        /// <summary>
        /// Waiting for user to complete Identity login in the browser
        /// </summary>
        PendingBrowserLoginAndProviderSetup,

        /// <summary>
        /// Started the log out process using an external browser
        /// </summary>
        LoggingOut,
        /// <summary>
        /// Failed to log out using an external browser
        /// </summary>
        LogoutFailed,
        /// <summary>
        /// Successfully logged out using an external browser
        /// </summary>
        LogoutSuccess,

        #endregion

        #region Using saved credentials
        /// <summary>
        /// Started the re-login process using saved credentials
        /// </summary>
        ReloggingIn,
        /// <summary>
        /// Failed to re-login using saved credentials
        /// </summary>
        ReloginFailed,
        /// <summary>
        /// Successfully re-logged in using saved credentials
        /// </summary>
        ReloginSuccess,

        /// <summary>
        /// Started the reconnect process using saved credentials
        /// </summary>
        Reconnecting,
        /// <summary>
        /// Failed to reconnect using saved credentials
        /// </summary>
        ReconnectFailed,
        /// <summary>
        /// Successfully reconnected in using saved credentials
        /// </summary>
        ReconnectSuccess,

        #endregion

        /// <summary>
        /// Started to the process of checking whether there are any stored credentials
        /// (does not check if they're still valid or not)
        /// </summary>
        CheckingForSavedCredentials,
        /// <summary>
        /// Failed to check whether there are any stored credentials
        /// (does not check if they're still valid or not)
        /// </summary>
        CheckForSavedCredentialsFailed,
        /// <summary>
        /// Successfully checked whether there are any stored credentials
        /// (does not check if they're still valid or not)
        /// </summary>
        CheckForSavedCredentialsSuccess
    }
}