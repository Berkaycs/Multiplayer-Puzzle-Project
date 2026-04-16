using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Unity.Services.Lobbies.Models;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _joinCodeText;
    [SerializeField] private Button _readyButton;
    [SerializeField] private Image _mapImage;
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;
    [SerializeField] private TMP_Text _mapName;
    [SerializeField] private MapSelectionData _mapSelectionData;

    private int _currentMapIndex = 0;

    private void OnEnable()
    {
        if (LobbyManager.Instance.IsHost)
        {
            _readyButton.onClick.AddListener(OnReadyButtonClicked);
            _leftButton.onClick.AddListener(OnLeftButtonClicked);
            _rightButton.onClick.AddListener(OnRightButtonClicked);
        }

        LobbyManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
    }

    private void OnDisable()
    {
        if (LobbyManager.Instance.IsHost)
        {
            _readyButton.onClick.RemoveListener(OnReadyButtonClicked);
            _leftButton.onClick.RemoveListener(OnLeftButtonClicked);
            _rightButton.onClick.RemoveListener(OnRightButtonClicked);
        }

        LobbyManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
    }

    private void Start()
    {
        _joinCodeText.text = $"Join Code: {LobbyManager.Instance.GetJoinCode()}";

        if (!LobbyManager.Instance.IsHost)
        {
            _leftButton.gameObject.SetActive(false);
            _rightButton.gameObject.SetActive(false);
        }
    }

    private void OnLobbyUpdated(Lobby lobby)
    {
        _currentMapIndex = LobbyManager.Instance.GetMapIndex();
        UpdateMap();
    }

    private async void OnReadyButtonClicked()
    {
        bool succeed = await LobbyManager.Instance.SetPlayerReady();

        if (succeed)
        {
            _readyButton.gameObject.SetActive(false);
        }
    }

    private async void OnLeftButtonClicked()
    {
        if (_currentMapIndex - 1 > 0)
        {
            _currentMapIndex--;
        }
        else
        {
            _currentMapIndex = 0;
        }

        UpdateMap();
        await LobbyManager.Instance.SetSelectedMap(_currentMapIndex);
    }

    private async void OnRightButtonClicked()
    {
        int lastMapIndex = _mapSelectionData.Maps.Count - 1;

        if (_currentMapIndex + 1 < lastMapIndex)
        {
            _currentMapIndex++;
        }
        else
        {
            _currentMapIndex = lastMapIndex;
        }

        UpdateMap();
        await LobbyManager.Instance.SetSelectedMap(_currentMapIndex);
    }

    private void UpdateMap()
    {
        _mapImage.color = _mapSelectionData.Maps[_currentMapIndex].MapThumbnail;
        _mapName.text = _mapSelectionData.Maps[_currentMapIndex].MapName;
    }
}
