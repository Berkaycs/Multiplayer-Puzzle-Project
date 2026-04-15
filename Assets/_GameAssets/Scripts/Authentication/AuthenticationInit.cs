using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.SceneManagement;

public class AuthenticationInit : MonoBehaviour
{
    // is updating
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            AuthenticationService.Instance.SignedIn += OnSignedIn;
            
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            if (AuthenticationService.Instance.IsSignedIn)
            {
                string userName = PlayerPrefs.GetString("UserName");

                if (string.IsNullOrEmpty(userName))
                {
                    userName = "Player";
                    PlayerPrefs.SetString("UserName", userName);
                }

                // _ = means we don't need to wait for the scene to load because there's no code that depends on it
                _ = SceneManager.LoadSceneAsync(Consts.Scenes.MAIN_MENU); 
            }
        }
    }

    private void OnSignedIn()
    {
        Debug.Log("Signed in");
        Debug.Log("User ID: " + AuthenticationService.Instance.PlayerId);
        Debug.Log("Access Token: " + AuthenticationService.Instance.AccessToken);
    }
}
