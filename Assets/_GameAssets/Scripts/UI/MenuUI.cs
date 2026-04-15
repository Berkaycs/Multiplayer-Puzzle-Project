using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _joinContainer;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;

    private void OnEnable()
    {
        _hostButton.onClick.AddListener(OnHostButtonClicked);
        _joinButton.onClick.AddListener(OnJoinButtonClicked);
    }

    private void OnDisable()
    {
        _hostButton.onClick.RemoveListener(OnHostButtonClicked);
        _joinButton.onClick.RemoveListener(OnJoinButtonClicked);
    }

    private async void OnHostButtonClicked()
    {
        // Create a new dictionary of player data
        Dictionary<string, string> playerData = new Dictionary<string, string>()
        {
            { "GamerTag", "HostPlayer" }
        };

        // Create the lobby with the given max players, private status, and player data
        bool success = await LobbyManager.Instance.CreateLobby(4, true, playerData);

        // If the lobby creation succeeds, load the lobby scene
        if (success)
        {
            SceneManager.LoadScene(Consts.Scenes.LOBBY);
        }
    }

    private void OnJoinButtonClicked()
    {
        gameObject.SetActive(false);
        _joinContainer.SetActive(true);
    }
}
