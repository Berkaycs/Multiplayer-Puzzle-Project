using System;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbySpawner : MonoBehaviour
{
    [SerializeField] private List<LobbyPlayer> _players;

    private void OnEnable()
    {
        // We want to subscribe to the lobby updated event
        // so that we can update the lobby player UI when the data changes
        LobbyManager.Instance.OnLobbyUpdated += OnLobbyUpdated;
    }

    private void OnDisable()
    {
        // Unsubscribe from the lobby updated event to prevent memory leaks
        LobbyManager.Instance.OnLobbyUpdated -= OnLobbyUpdated;
    }

    private void Start()
    {
        OnLobbyUpdated();
    }

    private void OnLobbyUpdated()
    {
        List<LobbyPlayerData> lobbyPlayerDatas = LobbyManager.Instance.GetLobbyPlayerDatas();

        for (int i = 0; i < _players.Count; i++)
        {
            if (i < lobbyPlayerDatas.Count)
            {
                // If there is player data for this index, set the player data and enable the player UI
                _players[i].SetData(lobbyPlayerDatas[i]);
            }
        }
    }
}
