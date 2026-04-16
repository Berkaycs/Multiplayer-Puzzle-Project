using UnityEngine;
using Unity.Services.Lobbies;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System;
using System.Threading;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public event Action<Lobby> OnLobbyUpdated;

    private Lobby _currentLobby;

    private Coroutine _heartbeatCoroutine;
    private CancellationTokenSource _updateLobbySource;

    private List<LobbyPlayerData> _lobbyPlayerDatas = new List<LobbyPlayerData>();
    private LobbyPlayerData _localLobbyPlayerData = new LobbyPlayerData();
    private LobbyData _lobbyData;

    public LobbyPlayerData LocalLobbyPlayerData { get => _localLobbyPlayerData; set => _localLobbyPlayerData = value; }

    public bool IsHost => _localLobbyPlayerData.Id == _currentLobby.HostId;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        CancelAndDisposeUpdateLobbySource();
    }

    private void CancelAndDisposeUpdateLobbySource()
    {
        if (_updateLobbySource == null)
            return;

        try
        {
            _updateLobbySource.Cancel();
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        _updateLobbySource.Dispose();
        _updateLobbySource = null;
    }

    // Creates a lobby with the given max players, private status, and player data
    public async Task<bool> CreateLobby(int maxPlayers, bool isPrivate, Dictionary<string, string> data, Dictionary<string, string> lobbyData)
    {
        // Serialize the player data into a dictionary of PlayerDataObject
        Dictionary<string, PlayerDataObject> playerData = SerializePlayerData(data);

        // Create a new player with the given player data
        Player player = new Player(
            AuthenticationService.Instance.PlayerId, 
            null, 
            playerData
        );

        // Create a new create lobby options with the given player and private status
        CreateLobbyOptions options = new CreateLobbyOptions()
        {
            Data = SerializeLobbyData(lobbyData),
            IsPrivate = isPrivate,
            Player = player
        };

        // Try to create the lobby
        try
        {
            _currentLobby = await LobbyService.Instance.CreateLobbyAsync("Lobby", maxPlayers, options);
        }
        // If the lobby creation fails, log the error and return false
        catch (Exception e)
        {
            Debug.LogError("Error creating lobby: " + e.Message);
            return false;
        }

        Debug.Log("Lobby created with ID: " + _currentLobby.Id);

        UpdateLobby(_currentLobby);

        _heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(_currentLobby.Id, 6f));

        PeriodicallyRefreshLobby();

        return true;
    }


    // Serializes the player data into a dictionary of PlayerDataObject
    // This is used to send the player data to the lobby service
    private Dictionary<string, PlayerDataObject> SerializePlayerData(Dictionary<string, string> data)
    {
        Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>();

        foreach (var (key, value) in data)
        {
            playerData.Add(key, new PlayerDataObject(
                PlayerDataObject.VisibilityOptions.Member,
                value
            ));
        }

        return playerData;
    }

    private Dictionary<string, DataObject> SerializeLobbyData(Dictionary<string, string> data)
    {
        Dictionary<string, DataObject> lobbyData = new Dictionary<string, DataObject>();
        foreach (var (key, value) in data)
        {
            lobbyData.Add(key, new DataObject(
                DataObject.VisibilityOptions.Member,
                value
            ));
        }
        return lobbyData;
    }

    // Sends a heartbeat ping to the lobby service to keep the lobby alive
    private IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float interval)
    {
        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
            yield return new WaitForSeconds(interval);
        }
    }

    // Refreshes the lobby data from the lobby service
    private async void PeriodicallyRefreshLobby()
    {
        _updateLobbySource = new CancellationTokenSource();
        await Task.Delay(1000);

        while (!_updateLobbySource.IsCancellationRequested && _currentLobby != null)
        {
            // we cant use await here because we are in a coroutine, so we use a task instead to await manually
            _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
            UpdateLobby(_currentLobby);
            await Task.Delay(1000);
        }
    }

    public async Task<bool> JoinLobby(string joinCode, Dictionary<string, string> playerData)
    {
        JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions();

        Player player = new Player(
            AuthenticationService.Instance.PlayerId, 
            null, 
            SerializePlayerData(playerData)
        );

        options.Player = player;

        try
        {
            _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode, options);

        }
        catch (Exception e)
        {
            Debug.LogError("Error joining lobby: " + e.Message);
            return false;
        }

        UpdateLobby(_currentLobby);

        PeriodicallyRefreshLobby();
        return true;
    }

    private void UpdateLobby(Lobby lobby)
    {
        List<Dictionary<string, PlayerDataObject>> playersData = GetPlayersData();
        _lobbyPlayerDatas.Clear();

        foreach (Dictionary<string, PlayerDataObject> playerData in playersData)
        {
            LobbyPlayerData lobbyPlayerData = new LobbyPlayerData();
            lobbyPlayerData.Initialize(playerData);

            if (lobbyPlayerData.Id == AuthenticationService.Instance.PlayerId)
            {
                _localLobbyPlayerData = lobbyPlayerData;
            }

            _lobbyPlayerDatas.Add(lobbyPlayerData);
        }

        _lobbyData = new LobbyData();
        _lobbyData.Initialize(lobby.Data);

        OnLobbyUpdated?.Invoke(lobby);
    }

    public async Task<bool> UpdatePlayerData(string id, Dictionary<string, string> data)
    {
        Dictionary<string, PlayerDataObject> playerData = SerializePlayerData(data);

        UpdatePlayerOptions options = new UpdatePlayerOptions()
        {
            Data = playerData
        };

        try
        {
            await LobbyService.Instance.UpdatePlayerAsync(_currentLobby.Id, id, options);

        }
        catch (Exception e)
        {
            Debug.LogError("Error updating player data: " + e.Message);
            return false;
        }

        OnLobbyUpdated?.Invoke(_currentLobby);

        return true;
    }

    public async Task<bool> UpdateLobbyData(Dictionary<string, string> data)
    {
        Dictionary<string, DataObject> lobbyData = SerializeLobbyData(data);

        UpdateLobbyOptions options = new UpdateLobbyOptions()
        {
            Data = lobbyData
        };
        try
        {
            _currentLobby = await LobbyService.Instance.UpdateLobbyAsync(_currentLobby.Id, options);
        }
        catch (Exception e)
        {
            Debug.LogError("Error updating lobby data: " + e.Message);
            return false;
        }

        OnLobbyUpdated?.Invoke(_currentLobby);

        return true;
    }

    private List<Dictionary<string, PlayerDataObject>> GetPlayersData()
    {
        List<Dictionary<string, PlayerDataObject>> playersData = new List<Dictionary<string, PlayerDataObject>>();

        foreach (Player player in _currentLobby.Players)
        {
            playersData.Add(player.Data);
        }

        return playersData;
    }

    public List<LobbyPlayerData> GetLobbyPlayerDatas()
    {
        return _lobbyPlayerDatas;
    }

    // Returns the join code of the current lobby
    public string GetJoinCode()
    {
        return _currentLobby.LobbyCode;
    }

    public int GetMapIndex()
    {
        return _lobbyData.MapIndex;
    }

    public async Task<bool> SetPlayerReady()
    {
        _localLobbyPlayerData.IsReady = true;
        return await UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize());
    }

    public async Task<bool> SetSelectedMap(int mapIndex)
    {
        _lobbyData.MapIndex = mapIndex;
        return await UpdateLobbyData(_lobbyData.Serialize());
    }

    // Deletes the lobby when the application quits
    public void OnApplicationQuit()
    {
        CancelAndDisposeUpdateLobbySource();

        if (_currentLobby != null && _currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
        }
    }
}
