using UnityEngine;
using Unity.Services.Lobbies;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System;
using System.Threading;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public event Action OnLobbyUpdated;
    public event Action OnLobbyReady;

    private Lobby _currentLobby;

    private Coroutine _heartbeatCoroutine;
    private CancellationTokenSource _updateLobbySource;

    private List<LobbyPlayerData> _lobbyPlayerDatas = new List<LobbyPlayerData>();
    private LobbyPlayerData _localLobbyPlayerData = new LobbyPlayerData();
    private LobbyData _lobbyData;

    public LobbyPlayerData LocalLobbyPlayerData { get => _localLobbyPlayerData; set => _localLobbyPlayerData = value; }

    public bool IsHost => _localLobbyPlayerData.Id == _currentLobby.HostId;

    private int _maxNumberOfPlayers = 4;

    private bool _inGame;
    private bool _wasDisconnected;

    private string _previousRelayCode;

    /// <summary> Reduces Lobby API rate limits (429); only host + client each poll. </summary>
    private const float LobbyRefreshInitialDelaySeconds = 2f;
    private const float LobbyRefreshIntervalSeconds = 3f;

    private List<string> _joinedLobbiesId;

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

    public async Task<bool> HasActiveLobbies()
    {
        await LobbyService.Instance.GetJoinedLobbiesAsync();

        if (_joinedLobbiesId.Count > 0)
        {
            return true;
        }

        return false;
    }

    // Creates a lobby with the given max players, private status, and player data
    public async Task<bool> CreateLobby(int maxPlayers, bool isPrivate, Dictionary<string, string> data, Dictionary<string, string> lobbyData)
    {
        if (_heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
            _heartbeatCoroutine = null;
        }

        CancelAndDisposeUpdateLobbySource();

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
        CancelAndDisposeUpdateLobbySource();

        CancellationTokenSource cts = new CancellationTokenSource();
        _updateLobbySource = cts;
        CancellationToken token = cts.Token;

        int rateLimitBackoffSeconds = 3;

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(LobbyRefreshInitialDelaySeconds), token);

            while (!token.IsCancellationRequested && _currentLobby != null)
            {
                try
                {
                    _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
                    rateLimitBackoffSeconds = 3;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    if (IsRateLimitedException(e))
                    {
                        Debug.LogWarning($"Lobby GetLobby rate limited; waiting {rateLimitBackoffSeconds}s before retry.");
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(rateLimitBackoffSeconds), token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                        rateLimitBackoffSeconds = Math.Min(rateLimitBackoffSeconds * 2, 30);
                        continue;
                    }

                    Debug.LogWarning("Lobby refresh failed: " + e.Message);
                    break;
                }

                await UpdateLobby(_currentLobby);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(LobbyRefreshIntervalSeconds), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the refresh loop is replaced or torn down.
        }
    }

    private static bool IsRateLimitedException(Exception e)
    {
        for (Exception current = e; current != null; current = current.InnerException)
        {
            string message = current.Message ?? string.Empty;
            if (message.IndexOf("429", StringComparison.Ordinal) >= 0
                || message.IndexOf("Too Many Requests", StringComparison.OrdinalIgnoreCase) >= 0
                || (message.IndexOf("Rate", StringComparison.OrdinalIgnoreCase) >= 0
                    && message.IndexOf("limit", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return true;
            }
        }

        return false;
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

    public async Task<bool> RejoinLobby()
    {
        try
        {
            _currentLobby = await LobbyService.Instance.ReconnectToLobbyAsync(_joinedLobbiesId[0]);
            _= UpdateLobby(_currentLobby);
        }
        catch (Exception e)
        {
            Debug.LogError("Error rejoining lobby: " + e.Message);
            return false;
        }

        PeriodicallyRefreshLobby();
        return true;
    }

    public async Task<bool> LeaveAllLobby()
    {
        string playerId = AuthenticationService.Instance.PlayerId;

        foreach (string lobbyId in _joinedLobbiesId)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);

            }
            catch (Exception e)
            {
                Debug.LogError("Error leaving lobby: " + e.Message);
                return false;
            }
        }

        return true;
    }

    public async Task StartGame()
    {
        string relayJoinCode = await RelayManager.Instance.CreateRelay(_maxNumberOfPlayers);
        _inGame = true;

        _lobbyData.RelayJoinCode = relayJoinCode;
        await UpdateLobbyData(_lobbyData.Serialize());

        string allocationId = RelayManager.Instance.GetAllocationId();
        string conncetionData = RelayManager.Instance.GetConnectionData();

        _localLobbyPlayerData.IsReady = false;

        await UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize(), allocationId, conncetionData);

        _ = SceneManager.LoadSceneAsync(_lobbyData.SceneName);
    }

    private async Task UpdateLobby(Lobby lobby)
    {
        List<Dictionary<string, PlayerDataObject>> playersData = GetPlayersData();
        _lobbyPlayerDatas.Clear();

        int numberOfPlayerReady = 0;

        foreach (Dictionary<string, PlayerDataObject> playerData in playersData)
        {
            LobbyPlayerData lobbyPlayerData = new LobbyPlayerData();
            lobbyPlayerData.Initialize(playerData);

            if (lobbyPlayerData.IsReady)
            {
                numberOfPlayerReady++;
            }

            if (lobbyPlayerData.Id == AuthenticationService.Instance.PlayerId)
            {
                _localLobbyPlayerData = lobbyPlayerData;
            }

            _lobbyPlayerDatas.Add(lobbyPlayerData);
        }

        _lobbyData = new LobbyData();
        _lobbyData.Initialize(lobby.Data);

        OnLobbyUpdated?.Invoke();

        if (numberOfPlayerReady == lobby.Players.Count)
        {
            // We can start the game
            OnLobbyReady?.Invoke();
        }

        if (_lobbyData.RelayJoinCode != default && !_inGame)
        {
            if (_wasDisconnected)
            {
                if (_lobbyData.RelayJoinCode != _previousRelayCode)
                {
                    await JoinRelayServer(_lobbyData.RelayJoinCode);
                    _ = SceneManager.LoadSceneAsync(_lobbyData.SceneName);
                }
            }
            else
            {
                await JoinRelayServer(_lobbyData.RelayJoinCode);
                _ = SceneManager.LoadSceneAsync(_lobbyData.SceneName);
            }   
        }
    }

    private async Task<bool> JoinRelayServer(string relayJoinCode)
    {
        _inGame = true;
        await RelayManager.Instance.JoinRelay(relayJoinCode);

        string allocationId = RelayManager.Instance.GetAllocationId();
        string conncetionData = RelayManager.Instance.GetConnectionData();

        _localLobbyPlayerData.IsReady = false;

        await UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize(), allocationId, conncetionData);
        return true;
    }

    // in params default means that if the parameter is not provided, it will be set to the default value of the type (null for reference types, 0 for value types, etc.)
    public async Task<bool> UpdatePlayerData(string id, Dictionary<string, string> data, string allocationId = default, string connectionData = default)
    {
        Dictionary<string, PlayerDataObject> playerData = SerializePlayerData(data);

        UpdatePlayerOptions options = new UpdatePlayerOptions()
        {
            Data = playerData,
            AllocationId = allocationId,
            ConnectionInfo = connectionData
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

        OnLobbyUpdated?.Invoke();

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

        OnLobbyUpdated?.Invoke();

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

    public async Task<bool> SetSelectedMap(int mapIndex, string sceneName)
    {
        _lobbyData.MapIndex = mapIndex;
        _lobbyData.SceneName = sceneName;
        return await UpdateLobbyData(_lobbyData.Serialize());
    }

    public async void GoBackToLobby(bool wasDisconnected)
    {
        _inGame = false;
        _wasDisconnected = wasDisconnected;

        if (_wasDisconnected)
        {
            _previousRelayCode = _lobbyData.RelayJoinCode;
        }

        _localLobbyPlayerData.IsReady = false;
        await UpdatePlayerData(_localLobbyPlayerData.Id, _localLobbyPlayerData.Serialize());
        _= SceneManager.LoadSceneAsync(Consts.Scenes.LOBBY);
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
