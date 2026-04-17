using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;

public class MenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _joinContainer;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private Button _rejoinButton;
    [SerializeField] private Button _leaveButton;

    private int _maxNumberOfPlayers = 4;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _hostButton.onClick.AddListener(OnHostButtonClicked);
        _joinButton.onClick.AddListener(OnJoinButtonClicked);
    }

    private void OnDisable()
    {
        _hostButton.onClick.RemoveListener(OnHostButtonClicked);
        _joinButton.onClick.RemoveListener(OnJoinButtonClicked);
    }

    private async void Start()
    {
        if (await LobbyManager.Instance.HasActiveLobbies())
        {
            _hostButton.gameObject.SetActive(false);
            _joinButton.gameObject.SetActive(false);

            _rejoinButton.gameObject.SetActive(true);
            _leaveButton.gameObject.SetActive(true);
            _rejoinButton.onClick.AddListener(OnRejoinButtonClicked);
            _leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        }
    }

    private async void OnHostButtonClicked()
    {
        // Create a new dictionary of player data
        LobbyManager.Instance.LocalLobbyPlayerData = new LobbyPlayerData();
        LobbyManager.Instance.LocalLobbyPlayerData  .Initialize(AuthenticationService.Instance.PlayerId, "HostPlayer");

        LobbyData lobbyData = new LobbyData();
        lobbyData.Initialize(0); // Default map index, you can change this as needed

        // Create the lobby with the given max players, private status, and player data
        bool success = await LobbyManager.Instance.CreateLobby(_maxNumberOfPlayers, true, LobbyManager.Instance.LocalLobbyPlayerData.Serialize(), lobbyData.Serialize());

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

    private async void OnRejoinButtonClicked()
    {
        bool succeeded = await LobbyManager.Instance.RejoinLobby();

        if (succeeded)
        {
            _= SceneManager.LoadSceneAsync(Consts.Scenes.LOBBY);
        }
    }

    private async void OnLeaveButtonClicked()
    {
        bool succeeded = await LobbyManager.Instance.LeaveAllLobby();
        if (succeeded)
        {
            _rejoinButton.gameObject.SetActive(false);
            _leaveButton.gameObject.SetActive(false);
            _hostButton.gameObject.SetActive(true);
            _joinButton.gameObject.SetActive(true);
        }
    }
}
