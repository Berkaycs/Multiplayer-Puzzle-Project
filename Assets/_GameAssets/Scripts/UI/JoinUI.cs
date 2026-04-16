using System;
using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class JoinUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _joinCodeInputField;
    [SerializeField] private Button _submitJoinCodeButton;

    private void OnEnable()
    {
        _submitJoinCodeButton.onClick.AddListener(OnSubmitJoinCodeClicked);
    }

    private void OnDisable()
    {
        _submitJoinCodeButton.onClick.RemoveListener(OnSubmitJoinCodeClicked);
    }

    private async void OnSubmitJoinCodeClicked()
    {
        // Create a new dictionary of player data
        LobbyManager.Instance.LocalLobbyPlayerData = new LobbyPlayerData();
        LobbyManager.Instance.LocalLobbyPlayerData.Initialize(AuthenticationService.Instance.PlayerId, "JoinPlayer");

        string joinCode = _joinCodeInputField.text.Trim();

        bool success = await LobbyManager.Instance.JoinLobby(joinCode, LobbyManager.Instance.LocalLobbyPlayerData.Serialize());

        if (success)
        {
            // we dont need await here because the lobby manager will handle the transition
            // to the lobby scene once we have successfully joined
            _ = SceneManager.LoadSceneAsync(Consts.Scenes.LOBBY);
        }
    }
}
