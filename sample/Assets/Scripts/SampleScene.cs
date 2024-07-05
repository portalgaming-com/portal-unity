using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Portal.Identity;
using UnityEngine.UI;
using Portal.Identity.Model;

public class InitIdentity : MonoBehaviour
{
    private Identity identity;

    public Button signinButton;

    public Button signinButtonPKCE;

    public Button mintButton;

    public Button requestSessionButton;


    async void Start()
    {
        Debug.Log("Start");
    }

    public async void OnLoginClickedPKCE()
    {
        string clientId = "cc497864-2dd2-4ca8-9584-0074ba321bb1";
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
        string clientId = "cc497864-2dd2-4ca8-9584-0074ba321bb1";
        identity = await Identity.Init(clientId);
        Debug.Log("Login button clicked");
        await identity.Authenticate();

        string idToken = await identity.GetIdToken();
        Debug.Log("idToken: " + idToken);
    }

    public async void OnMintClicked()
    {
        Debug.Log("Mint button clicked");
        string transactionHash = await identity.ExecuteTransaction(new TransactionRequest()
        {
            ChainId = 80002,
            ContractId = "con_f9cd72df-66d8-48c8-bf64-529ae02bdd15",
            PolicyId = "pol_c0638f95-2de5-491e-b9ae-bf4bb2f0917a",
            FunctionName = "mint",
            FunctionArgs =
            new List<string> { "0x37eC246fCD668400Df5dAA5362601dB613BAcC84" }
        });
        Debug.Log("mintedToken: " + transactionHash);
    }

    public async void OnRequestSessionClicked()
    {
        Debug.Log("Request session button clicked");
        await identity.RequestWalletSessionKey();
        Debug.Log("created session");
    }
}