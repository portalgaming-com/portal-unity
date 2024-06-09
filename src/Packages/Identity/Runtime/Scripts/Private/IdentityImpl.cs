using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using Portal.Identity.Event;
using Portal.Identity.Model;
using Portal.Identity.Core;
using Portal.Identity.Helpers;
using Cysharp.Threading.Tasks;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Portal.Identity
{
    public class IdentityImpl
    {
        private const string TAG = "[Identity Implementation]";
        public readonly IBrowserCommunicationsManager communicationsManager;

        // Used for device code auth
        private DeviceConnectResponse deviceConnectResponse;

        // Used for PKCE
        private UniTaskCompletionSource<bool> pkceCompletionSource;
        private string redirectUri = null;
        private string logoutRedirectUri = null;

#if UNITY_ANDROID
        // Used for the PKCE callback
        internal static bool completingPKCE = false;
        internal static string loginPKCEUrl;
#endif

        // Used to prevent calling login/connect functions multiple times
        private bool isLoggedIn = false;

        public event OnAuthEventDelegate OnAuthEvent;

        public IdentityImpl(IBrowserCommunicationsManager communicationsManager)
        {
            this.communicationsManager = communicationsManager;
        }

        public async UniTask Init(string clientId, string redirectUri = null, string logoutRedirectUri = null, string deeplink = null)
        {
            this.redirectUri = redirectUri;
            this.logoutRedirectUri = logoutRedirectUri;
            this.communicationsManager.OnAuthPostMessage += OnDeepLinkActivated;
            this.communicationsManager.OnPostMessageError += OnPostMessageError;


            string initRequest;
            if (redirectUri != null && logoutRedirectUri != null)
            {
                InitRequestWithRedirectUri requestWithRedirectUri = new InitRequestWithRedirectUri()
                {
                    clientId = clientId,
                    redirectUri = redirectUri,
                    logoutRedirectUri = logoutRedirectUri,
                };
                initRequest = JsonUtility.ToJson(requestWithRedirectUri);
            }
            else
            {
                InitRequest request = new InitRequest()
                {
                    clientId = clientId,
                };
                initRequest = JsonUtility.ToJson(request);
            }

            string response = await communicationsManager.Call(IdentityFunction.INIT, initRequest);
            BrowserResponse initResponse = response.OptDeserializeObject<BrowserResponse>();

            if (initResponse.success == false)
            {
                throw new IdentityException(initResponse.error ?? "Unable to initialize Identity");
            }
            else if (deeplink != null)
            {
                OnDeepLinkActivated(deeplink);
            }

        }

        public async UniTask<bool> Authenticate(bool useCachedSession = false, Nullable<long> timeoutMs = null)
        {
            string functionName = "Authenticate";
            if (useCachedSession)
            {
                return await Reauthenticate();
            }
            else
            {
                try
                {
                    SendAuthEvent(IdentityAuthEvent.LoggingIn);

                    await InitializeDeviceCodeAuth(functionName);
                    await ConfirmCode(
                        IdentityAuthEvent.LoginOpeningBrowser, IdentityAuthEvent.PendingBrowserLogin, functionName,
                        IdentityFunction.AUTHENTICATE_CONFIRM_CODE, timeoutMs);


                    SendAuthEvent(IdentityAuthEvent.LoginSuccess);
                    isLoggedIn = true;
                    return true;
                }
                catch (Exception ex)
                {

                    SendAuthEvent(IdentityAuthEvent.LoginFailed);
                    throw ex;
                }
            }
        }

        private async UniTask<bool> Reauthenticate()
        {
            try
            {
                SendAuthEvent(IdentityAuthEvent.ReloggingIn);

                string callResponse = await communicationsManager.Call(IdentityFunction.REAUTHENTICATE);
                bool success = callResponse.GetBoolResponse() ?? false;

                SendAuthEvent(success ? IdentityAuthEvent.ReloginSuccess : IdentityAuthEvent.ReloginFailed);
                isLoggedIn = success;
                return success;
            }
            catch (Exception ex)
            {
                Debug.Log($"{TAG} Failed to login to Identity using saved credentials: {ex.Message}");
            }
            SendAuthEvent(IdentityAuthEvent.ReloginFailed);
            return false;
        }

        private async UniTask<ConnectResponse> InitializeDeviceCodeAuth(string callingFunction)
        {
            string callResponse = await communicationsManager.Call(IdentityFunction.INIT_DEVICE_FLOW);
            deviceConnectResponse = callResponse.OptDeserializeObject<DeviceConnectResponse>();
            if (deviceConnectResponse != null && deviceConnectResponse.success == true)
            {
                return new ConnectResponse()
                {
                    url = deviceConnectResponse.url,
                    code = deviceConnectResponse.code
                };
            }

            throw new IdentityException(deviceConnectResponse?.error ?? $"Something went wrong, please call {callingFunction} again", IdentityErrorType.AUTHENTICATION_ERROR);
        }

        private async UniTask ConfirmCode(
            IdentityAuthEvent openingBrowserAuthEvent, IdentityAuthEvent pendingAuthEvent,
            string callingFunction, string functionToCall, Nullable<long> timeoutMs = null)
        {
            if (deviceConnectResponse != null)
            {
                // Open URL for user to confirm
                SendAuthEvent(openingBrowserAuthEvent);
                OpenUrl(deviceConnectResponse.url);

                // Start polling for token
                SendAuthEvent(pendingAuthEvent);
                ConfirmCodeRequest request = new ConfirmCodeRequest()
                {
                    deviceCode = deviceConnectResponse.deviceCode,
                    interval = deviceConnectResponse.interval,
                    timeoutMs = timeoutMs
                };

                string callResponse = await communicationsManager.Call(
                    functionToCall,
                    JsonUtility.ToJson(request),
                    true // Ignore timeout, this flow can take minutes to complete. 15 minute expiry from Auth0.
                );
                BrowserResponse response = callResponse.OptDeserializeObject<BrowserResponse>();
                if (response == null || response?.success == false)
                {
                    throw new IdentityException(
                        response?.error ?? $"Unable to confirm code, call {callingFunction} again",
                        IdentityErrorType.AUTHENTICATION_ERROR
                    );
                }
            }
            else
            {
                throw new IdentityException($"Unable to confirm code, call {callingFunction} again", IdentityErrorType.AUTHENTICATION_ERROR);
            }
        }

        public async void OnDeepLinkActivated(string url)
        {
            try
            {
                Debug.Log($"{TAG} OnDeepLinkActivated URL: {url}");

                Uri uri = new Uri(url);
                string domain = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
                if (domain.EndsWith("/"))
                {
                    domain = domain.Remove(domain.Length - 1);
                }

                if (domain.Equals(logoutRedirectUri))
                {
                    HandleLogoutPKCESuccess();
                }
                else if (domain.Equals(redirectUri))
                {
                    await CompleteLoginPKCEFlow(url);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{TAG} OnDeepLinkActivated error {url}: {e.Message}");
            }
        }

        public UniTask<bool> LoginPKCE()
        {
            try
            {
                SendAuthEvent(IdentityAuthEvent.LoggingInPKCE);

                UniTaskCompletionSource<bool> task = new UniTaskCompletionSource<bool>();
                pkceCompletionSource = task;
                _ = LaunchAuthUrl();
                return task.Task;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to log in using PKCE flow: {ex.Message}";
                Debug.Log($"{TAG} {errorMessage}");

                SendAuthEvent(IdentityAuthEvent.LoginPKCEFailed);
                throw new IdentityException(errorMessage, IdentityErrorType.AUTHENTICATION_ERROR);
            }
        }

        private async UniTask LaunchAuthUrl()
        {
            try
            {
                string callResponse = await communicationsManager.Call(IdentityFunction.GET_PKCE_AUTH_URL);
                StringResponse response = callResponse.OptDeserializeObject<StringResponse>();

                if (response != null && response.success == true && response.result != null)
                {
                    string url = response.result.Replace(" ", "+");
#if UNITY_ANDROID && !UNITY_EDITOR
                    loginPKCEUrl = url;
                    SendAuthEvent(IdentityAuthEvent.LoginPKCELaunchingCustomTabs);
                    LaunchAndroidUrl(url);
#else
                    SendAuthEvent(IdentityAuthEvent.LoginPKCEOpeningWebView);
                    communicationsManager.LaunchAuthURL(url, redirectUri);
#endif
                    return;
                }
                else
                {
                    Debug.Log($"{TAG} Failed to get PKCE Auth URL");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"{TAG} Get PKCE Auth URL error: {e.Message}");
            }

            await UniTask.SwitchToMainThread();
            TrySetPKCEException(new IdentityException(
                "Something went wrong",
                IdentityErrorType.AUTHENTICATION_ERROR
            ));
        }

        public async UniTask CompleteLoginPKCEFlow(string uriString)
        {
#if UNITY_ANDROID
            completingPKCE = true;
#endif
            try
            {
                SendAuthEvent(IdentityAuthEvent.CompletingLoginPKCE);
                Uri uri = new Uri(uriString);
                string state = uri.GetQueryParameter("state");
                string authCode = uri.GetQueryParameter("code");

                if (String.IsNullOrEmpty(state) || String.IsNullOrEmpty(authCode))
                {
                    SendAuthEvent(IdentityAuthEvent.LoginPKCEFailed);
                    await UniTask.SwitchToMainThread();
                    TrySetPKCEException(new IdentityException(
                        "Uri was missing state and/or code.",
                        IdentityErrorType.AUTHENTICATION_ERROR
                    ));
                }
                else
                {
                    ConnectPKCERequest request = new ConnectPKCERequest()
                    {
                        authorizationCode = authCode,
                        state = state
                    };

                    string callResponse = await communicationsManager.Call(
                            IdentityFunction.LOGIN_PKCE,
                            JsonUtility.ToJson(request)
                        );

                    BrowserResponse response = callResponse.OptDeserializeObject<BrowserResponse>();
                    await UniTask.SwitchToMainThread();

                    if (response != null && response.success != true)
                    {
                        SendAuthEvent(IdentityAuthEvent.LoginPKCEFailed);
                        TrySetPKCEException(new IdentityException(
                            response.error ?? "Something went wrong",
                            IdentityErrorType.AUTHENTICATION_ERROR
                        ));
                    }
                    else
                    {
                        if (!isLoggedIn)
                        {
                            TrySetPKCEResult(true);
                        }

                        SendAuthEvent(IdentityAuthEvent.LoginPKCESuccess);
                        isLoggedIn = true;
                    }
                }
            }
            catch (Exception ex)
            {
                SendAuthEvent(IdentityAuthEvent.LoginPKCEFailed);
                // Ensure any failure results in completing the flow regardless.
                TrySetPKCEException(ex);
            }

            pkceCompletionSource = null;
#if UNITY_ANDROID
            completingPKCE = false;
#endif
        }

#if UNITY_ANDROID
        public void OnLoginPKCEDismissed(bool completing)
        {
            Debug.Log($"{TAG} On Login PKCE Dismissed");
            if (!completing && !isLoggedIn)
            {
                // User hasn't entered all required details (e.g. email address) into Identity yet
                Debug.Log($"{TAG} Login PKCE dismissed before completing the flow");
                TrySetPKCECanceled();
            }
            else
            {
                Debug.Log($"{TAG} Login PKCE dismissed by user or SDK");
            }
            loginPKCEUrl = null;
        }

        public void OnDeeplinkResult(string url)
        {
            OnDeepLinkActivated(url);
        }
#endif

        public async UniTask<string> GetLogoutUrl()
        {
            string response = await communicationsManager.Call(IdentityFunction.LOGOUT);
            string logoutUrl = response.GetStringResult();
            if (String.IsNullOrEmpty(logoutUrl))
            {
                throw new IdentityException("Failed to get logout URL", IdentityErrorType.AUTHENTICATION_ERROR);
            }
            else
            {
                return response.GetStringResult();
            }
        }

        public async UniTask Logout(bool hardLogout = true)
        {
            try
            {
                SendAuthEvent(IdentityAuthEvent.LoggingOut);

                string logoutUrl = await GetLogoutUrl();
                if (hardLogout)
                {
                    OpenUrl(logoutUrl);
                }

                SendAuthEvent(IdentityAuthEvent.LogoutSuccess);
                isLoggedIn = false;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to log out: {ex.Message}";
                Debug.Log($"{TAG} {errorMessage}");

                SendAuthEvent(IdentityAuthEvent.LogoutFailed);
                throw new IdentityException(errorMessage, IdentityErrorType.AUTHENTICATION_ERROR);
            }
        }

        public UniTask LogoutPKCE(bool hardLogout = true)
        {
            try
            {
                SendAuthEvent(IdentityAuthEvent.LoggingOutPKCE);

                UniTaskCompletionSource<bool> task = new UniTaskCompletionSource<bool>();
                pkceCompletionSource = task;
                LaunchLogoutPKCEUrl(hardLogout);
                return task.Task;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to log out: {ex.Message}";
                Debug.Log($"{TAG} {errorMessage}");

                SendAuthEvent(IdentityAuthEvent.LogoutPKCEFailed);
                throw new IdentityException(errorMessage, IdentityErrorType.AUTHENTICATION_ERROR);
            }
        }

        private async void HandleLogoutPKCESuccess()
        {
            await UniTask.SwitchToMainThread();
            if (isLoggedIn)
            {
                TrySetPKCEResult(true);
            }
            SendAuthEvent(IdentityAuthEvent.LogoutPKCESuccess);
            isLoggedIn = false;
            pkceCompletionSource = null;
        }

        private async void LaunchLogoutPKCEUrl(bool hardLogout)
        {
            string logoutUrl = await GetLogoutUrl();
            if (hardLogout)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                LaunchAndroidUrl(logoutUrl);
#else
                communicationsManager.LaunchAuthURL(logoutUrl, logoutRedirectUri);
#endif
            }
            else
            {
                HandleLogoutPKCESuccess();
            }
        }

        public async UniTask<bool> HasCredentialsSaved()
        {
            try
            {
                SendAuthEvent(IdentityAuthEvent.CheckingForSavedCredentials);
                string accessToken = await GetAccessToken();
                string idToken = await GetIdToken();
                SendAuthEvent(IdentityAuthEvent.CheckForSavedCredentialsSuccess);
                return accessToken != null && idToken != null;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to check if there are credentials saved: {ex.Message}";
                Debug.Log($"{TAG} {errorMessage}");
                SendAuthEvent(IdentityAuthEvent.CheckForSavedCredentialsFailed);
                return false;
            }
        }

        public async UniTask<string> GetAccessToken()
        {
            string response = await communicationsManager.Call(IdentityFunction.GET_ACCESS_TOKEN);
            return response.GetStringResult();
        }


        public async UniTask<string> GetIdToken()
        {
            string response = await communicationsManager.Call(IdentityFunction.GET_ID_TOKEN);
            return response.GetStringResult();
        }

        private async void OnPostMessageError(string id, string message)
        {
            if (id == "CallFromAuthCallbackError" && pkceCompletionSource != null)
            {
                await CallFromAuthCallbackError(id, message);
            }
            else
            {
                Debug.LogError($"{TAG} id: {id} err: {message}");
            }
        }

        private async UniTask CallFromAuthCallbackError(string id, string message)
        {
            await UniTask.SwitchToMainThread();

            if (message == "")
            {
                Debug.Log($"{TAG} Get PKCE Auth URL user cancelled");
                TrySetPKCECanceled();
            }
            else
            {
                Debug.Log($"{TAG} Get PKCE Auth URL error: {message}");
                TrySetPKCEException(new IdentityException(
                    "Something went wrong.",
                    IdentityErrorType.AUTHENTICATION_ERROR
                ));
            }

            pkceCompletionSource = null;
        }

        private void TrySetPKCEResult(bool result)
        {
            Debug.Log($"{TAG} Trying to set PKCE result to {result}...");
            if (pkceCompletionSource != null)
            {
                pkceCompletionSource.TrySetResult(result);
            }
            else
            {
                Debug.LogError($"{TAG} PKCE completed with {result} but unable to bind result");
            }
        }

        private void TrySetPKCEException(Exception exception)
        {
            Debug.Log($"{TAG} Trying to set PKCE exception...");
            if (pkceCompletionSource != null)
            {
                pkceCompletionSource.TrySetException(exception);
            }
            else
            {
                Debug.LogError($"{TAG} {exception.Message}");
            }
        }

        private void TrySetPKCECanceled()
        {
            Debug.Log($"{TAG} Trying to set PKCE canceled...");
            if (pkceCompletionSource != null)
            {
                pkceCompletionSource.TrySetCanceled();
            }
            else
            {
                Debug.LogWarning($"{TAG} PKCE canceled");
            }
        }

        private void SendAuthEvent(IdentityAuthEvent authEvent)
        {
            Debug.Log($"{TAG} Send auth event: {authEvent}");
            if (OnAuthEvent != null)
            {
                OnAuthEvent.Invoke(authEvent);
            }
        }

        protected virtual void OpenUrl(string url)
        {
            Application.OpenURL(url);
        }

#if UNITY_ANDROID
        private void LaunchAndroidUrl(string url)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass customTabLauncher = new AndroidJavaClass("com.portal.unity.PortalActivity");
            customTabLauncher.CallStatic("startActivity", activity, url, new AndroidPKCECallback((PKCECallback)this));
        }
#endif

#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        public void ClearCache(bool includeDiskFiles)
        {
            communicationsManager.ClearCache(includeDiskFiles);
        }

        public void ClearStorage()
        {
            communicationsManager.ClearStorage();
        }
#endif

    }

#if UNITY_ANDROID
    public interface PKCECallback
    {

        /// <summary>
        /// Called when the Android Chrome Custom Tabs is hidden. 
        /// Note that you won't be able to tell whether it was closed by the user or the SDK.
        /// <param name="completing">True if the user has entered everything required (e.g. email address),
        /// Chrome Custom Tabs have closed, and the SDK is trying to complete the PKCE flow.
        /// See <see cref="IdentityImpl.CompleteLoginPKCEFlow"></param>
        /// </summary>
        void OnLoginPKCEDismissed(bool completing);

        void OnDeeplinkResult(string url);
    }

    class AndroidPKCECallback : AndroidJavaProxy
    {
        private PKCECallback callback;

        public AndroidPKCECallback(PKCECallback callback) : base("com.portal.unity.PortalActivity$Callback")
        {
            this.callback = callback;
        }

        async void onCustomTabsDismissed(string url)
        {
            await UniTask.SwitchToMainThread();

            // To differentiate what triggered this
            if (url == IdentityImpl.loginPKCEUrl)
            {
                // Custom tabs dismissed for login flow
                callback.OnLoginPKCEDismissed(IdentityImpl.completingPKCE);
            }
        }

        async void onDeeplinkResult(string url)
        {
            await UniTask.SwitchToMainThread();
            callback.OnDeeplinkResult(url);
        }
    }
#endif
}