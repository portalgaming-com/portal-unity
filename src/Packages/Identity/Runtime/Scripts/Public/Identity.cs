using System.Collections.Generic;
using System;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
using VoltstroStudios.UnityWebBrowser.Core;
#else
using Portal.Browser.Gree;
#endif
using Portal.Identity.Event;
using Portal.Browser.Core;
using Portal.Identity.Model;
using Portal.Identity.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Portal.Identity
{

    public class Identity
    {
        private const string TAG = "[Identity]";

        public static Identity Instance { get; private set; }

#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
        private readonly IWebBrowserClient webBrowserClient = new WebBrowserClient();
#else
        private readonly IWebBrowserClient webBrowserClient = new GreeBrowserClient();
#endif

        // Keeps track of the latest received deeplink
        private static string deeplink = null;
        private static bool readySignalReceived = false;
        private IdentityImpl identityImpl = null;

        public event OnAuthEventDelegate OnAuthEvent;

        private Identity()
        {
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            Application.quitting += OnQuit;
#elif UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Application.deepLinkActivated += OnDeepLinkActivated;
            if (!string.IsNullOrEmpty(Application.absoluteURL))
            {
                // Cold start and Application.absoluteURL not null so process Deep Link.
                OnDeepLinkActivated(Application.absoluteURL);
            }
#endif
        }

        /// <summary>
        /// Initializes Identity
        /// </summary>
        /// <param name="clientId">The client ID</param>
        /// <param name="redirectUri">(Android, iOS and macOS only) The URL to which auth will redirect the browser after authorization has been granted by the user</param>
        /// <param name="logoutRedirectUri">(Android, iOS and macOS only) The URL to which auth will redirect the browser after log out is complete</param>
        /// <param name="engineStartupTimeoutMs">(Windows only) Timeout time for waiting for the engine to start (in milliseconds)</param>
        public static UniTask<Identity> Init(
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            string clientId, string redirectUri = null, string logoutRedirectUri = null, int engineStartupTimeoutMs = 4000
#else
            string clientId, string redirectUri = null, string logoutRedirectUri = null
#endif
        )
        {
            if (Instance == null)
            {
                Debug.Log($"{TAG} Initializing Identity...");
                Instance = new Identity();
                // Wait until we get a ready signal
                return Instance.Initialize(
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                        engineStartupTimeoutMs
#endif
                    )
                    .ContinueWith(async () =>
                    {
                        Debug.Log($"{TAG} Waiting for ready signal...");
                        await UniTask.WaitUntil(() => readySignalReceived == true);
                    })
                    .ContinueWith(async () =>
                    {
                        if (readySignalReceived == true)
                        {
                            await Instance.GetIdentityImpl().Init(clientId, redirectUri, logoutRedirectUri, deeplink);
                            return Instance;
                        }
                        else
                        {
                            Debug.Log($"{TAG} Failed to initialize Identity");
                            throw new IdentityException("Failed to initialize Identity", IdentityErrorType.INITIALIZATION_ERROR);
                        }
                    });
            }
            else
            {
                readySignalReceived = true;
                return UniTask.FromResult(Instance);
            }
        }

        private async UniTask Initialize(
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
            int engineStartupTimeoutMs
#endif
        )
        {
            try
            {
                BrowserCommunicationsManager communicationsManager = new BrowserCommunicationsManager(webBrowserClient);
                communicationsManager.OnReady += () => readySignalReceived = true;
#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
                await ((WebBrowserClient)webBrowserClient).Init(engineStartupTimeoutMs);
#endif
                identityImpl = new IdentityImpl(communicationsManager);
                identityImpl.OnAuthEvent += OnIdentityAuthEvent;
            }
            catch (Exception ex)
            {
                // Reset values
                readySignalReceived = false;
                Instance = null;
                throw ex;
            }
        }

#if UNITY_STANDALONE_WIN || (UNITY_ANDROID && UNITY_EDITOR_WIN) || (UNITY_IPHONE && UNITY_EDITOR_WIN)
        private void OnQuit()
        {
            // Need to clean up UWB resources when quitting the game in the editor
            // as the child engine process would still be alive
            Debug.Log($"{TAG} Quitting the Player");
            ((WebBrowserClient)webBrowserClient).Dispose();
        }
#endif

        /// <summary>
        /// Sets the timeout time for waiting for each call to respond (in milliseconds).
        /// This only applies to functions that use the browser communications manager.
        /// </summary>
        public void SetCallTimeout(int ms)
        {
            GetIdentityImpl().communicationsManager.SetCallTimeout(ms);
        }

        /// <summary>
        /// Logs the user into Identity via device code auth. This will open the user's default browser and take them through Identity login.
        /// <param name="useCachedSession">If true, the saved access token or refresh token will be used to log the user in. If this fails, it will not fallback to device code auth.</param>
        /// </summary>
        public async UniTask<bool> Authenticate(bool useCachedSession = false, Nullable<long> timeoutMs = null)
        {
            return await GetIdentityImpl().Authenticate(useCachedSession, timeoutMs);
        }


#if UNITY_ANDROID || UNITY_IPHONE || UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        /// <summary>
        /// Connects the user into Identity via PKCE auth.
        /// </summary>
        public async UniTask LoginPKCE()
        {
            await GetIdentityImpl().LoginPKCE();
        }
#endif

        /// <summary>
        /// Logs the user out of Identity and removes any stored credentials.
        /// </summary>
        /// <param name="hardLogout">If false, the user will not be logged out of Identity in the browser. The default is true.</param>
        public async UniTask Logout(bool hardLogout = true)
        {
            await GetIdentityImpl().Logout(hardLogout);
        }

        /// <summary>
        /// Logs the user out of Identity and removes any stored credentials.
        /// Recommended to use when logging in using PKCE flow
        /// </summary>
        /// <param name="hardLogout">If false, the user will not be logged out of Identity in the browser. The default is true.</param>
        public async UniTask LogoutPKCE(bool hardLogout = true)
        {
            await GetIdentityImpl().LogoutPKCE(hardLogout);
        }

        /// <summary>
        /// Checks if credentials exist but does not check if they're valid
        /// <returns>
        /// True if there are crendentials saved
        /// </returns>
        /// </summary>
        public UniTask<bool> HasCredentialsSaved()
        {
            return GetIdentityImpl().HasCredentialsSaved();
        }

        /// <summary>
        /// Gets the currently saved access token without verifying its validity.
        /// <returns>
        /// The access token, otherwise null
        /// </returns>
        /// </summary>
        public UniTask<string> GetAccessToken()
        {
            return GetIdentityImpl().GetAccessToken();
        }

        /// <summary>
        /// Gets the currently saved ID token without verifying its validity.
        /// <returns>
        /// The ID token, otherwise null
        /// </returns>
        /// </summary>
        public UniTask<string> GetIdToken()
        {
            return GetIdentityImpl().GetIdToken();
        }

#if (UNITY_IPHONE && !UNITY_EDITOR) || (UNITY_ANDROID && !UNITY_EDITOR)
        /// <summary>
        /// Clears the underlying WebView resource cache
        /// Android: Note that the cache is per-application, so this will clear the cache for all WebViews used.
        /// <param name="includeDiskFiles">if false, only the RAM/in-memory cache is cleared</param>
        /// </summary>
        /// <returns></returns>
        public void ClearCache(bool includeDiskFiles)
        {
            GetIdentityImpl().ClearCache(includeDiskFiles);
        }

        /// <summary>
        /// Clears all the underlying WebView storage currently being used by the JavaScript storage APIs. 
        /// This includes Web SQL Database and the HTML5 Web Storage APIs.
        /// </summary>
        /// <returns></returns>
        public void ClearStorage()
        {
            GetIdentityImpl().ClearStorage();
        }
#endif

        private IdentityImpl GetIdentityImpl()
        {
            if (identityImpl != null)
            {
                return identityImpl;
            }
            throw new IdentityException("Identity not initialized");
        }

        private void OnDeepLinkActivated(string url)
        {
            deeplink = url;

            if (identityImpl != null)
            {
                GetIdentityImpl().OnDeepLinkActivated(url);
            }
        }

        private void OnIdentityAuthEvent(IdentityAuthEvent authEvent)
        {
            if (OnAuthEvent != null)
            {
                OnAuthEvent.Invoke(authEvent);
            }
        }
    }
}
