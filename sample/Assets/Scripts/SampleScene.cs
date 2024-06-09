using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Portal.Identity;
using UnityEngine.UI;

public class InitIdentity : MonoBehaviour
{
    private Identity identity;

    public Button signinButton;

    public Button signinButtonPKCE;


    async void Start()
    {
        Debug.Log("Start");
    }

    public async void OnLoginClickedPKCE()
    {
        string clientId = "AQvtQYRvAaprNQMh0cb3VnDGdiaTn0fS";
        string redirectUri = "mygame://callback";
        string logoutRedirectUri = "mygame://logout";
        identity = await Identity.Init(clientId, redirectUri, logoutRedirectUri);
        Debug.Log("Login button clicked");
        await identity.LoginPKCE();

        string idToken = await identity.GetIdToken();
        Debug.Log("idToken: " + idToken);
    }

    public async void OnLoginClicked()
    {
        string clientId = "AQvtQYRvAaprNQMh0cb3VnDGdiaTn0fS";
        identity = await Identity.Init(clientId);
        Debug.Log("Login button clicked");
        await identity.Authenticate();

        string idToken = await identity.GetIdToken();
        Debug.Log("idToken: " + idToken);
    }
}