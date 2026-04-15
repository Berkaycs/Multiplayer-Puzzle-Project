using UnityEngine;
using Unity.Services.Lobbies;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using System.Collections;
using System;
 
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private Lobby _currentLobby;
    private Coroutine _heartbeatCoroutine;
    private Coroutine _refreshLobbyCoroutine;

    private void Awake()
    {
        Instance = this;
    }

    // Creates a lobby with the given max players, private status, and player data
    public async Task<bool> CreateLobby(int maxPlayers, bool isPrivate, Dictionary<string, string> data)
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

        _heartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(_currentLobby.Id, 6f));
        _refreshLobbyCoroutine = StartCoroutine(RefreshLobbyCoroutine(_currentLobby.Id, 1f));

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
    private IEnumerator RefreshLobbyCoroutine(string lobbyId, float interval)
    {
        while (true)
        {
            // we cant use await here because we are in a coroutine, so we use a task instead to await manually
            Task<Lobby> getLobbyTask = LobbyService.Instance.GetLobbyAsync(lobbyId);
            yield return new WaitUntil(() => getLobbyTask.IsCompleted);
            Lobby newLobby = getLobbyTask.Result;
            if (newLobby.LastUpdated > _currentLobby.LastUpdated)
            {
                _currentLobby = newLobby;
            }

            yield return new WaitForSeconds(interval);
        }
    }

    public async Task<bool> JoinLobby(string joinCode)
    {
        Dictionary<string, string> playerData = new Dictionary<string, string>()
        {
            { "GamerTag", "JoinPlayer" }
        };

        JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions();
        Player player = new Player(AuthenticationService.Instance.PlayerId, null, SerializePlayerData(playerData));

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

        _refreshLobbyCoroutine = StartCoroutine(RefreshLobbyCoroutine(_currentLobby.Id, 1f));
        return true;
    }

    // Returns the join code of the current lobby
    public string GetJoinCode()
    {
        return _currentLobby.LobbyCode;
    }

    // Deletes the lobby when the application quits
    public void OnApplicationQuit()
    {
        if (_currentLobby != null && _currentLobby.HostId == AuthenticationService.Instance.PlayerId)
        {
            LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
        }
    }
}
