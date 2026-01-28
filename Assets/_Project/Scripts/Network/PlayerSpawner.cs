/*
====================================================================
* PlayerSpawner - Spawns Players When Game Starts
====================================================================
* Project: Showroom_Tango (2-Player Top-Down Bullet-Hell)
* Course: PRG - Game & Multimedia Design
* Developer: Julian
* Date: 18.12.2025
* Version: 2.0 - Single-scene: spawns on state change, not scene load
*
* WICHTIG: KOMMENTIERUNG NICHT LOESCHEN!
*
* [HUMAN-AUTHORED]
* - Auto-spawn strategy on game start
* - Spawn point positioning
* - Single-scene architecture decision
*
* [AI-ASSISTED]
* - FishNet server connection event handling
* - Server-authority spawn pattern
* - GameStateManager event subscription
*
* [AI-GENERATED]
* - None
*
* NOTES:
* - Spawns players when GameState transitions to Playing
* - Reads player data from LobbyManager (same scene, no caching needed)
* - Despawn handled by GameStateManager restart sequence
====================================================================
*/

using UnityEngine;
using FishNet.Managing;
using FishNet.Connection;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 spawnOffset = new Vector3(2f, 0f, 0f);

    private NetworkManager networkManager;
    private int spawnedPlayers = 0;
    private bool hasSpawnedThisRound = false;

    void Start()
    {
        networkManager = FindFirstObjectByType<NetworkManager>();

        if (networkManager == null)
        {
            Debug.LogError("[PlayerSpawner] NetworkManager not found!");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] Player Prefab not assigned!");
            return;
        }

        // Subscribe to game state changes
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
        else
        {
            // Retry after a short delay if GameStateManager isn't ready yet
            Invoke(nameof(SubscribeToGameState), 0.5f);
        }
    }

    private void SubscribeToGameState()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
    }

    void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.Playing && !hasSpawnedThisRound)
        {
            if (networkManager != null && networkManager.IsServerStarted)
            {
                SpawnAllPlayers();
            }
        }
        else if (newState == GameState.Lobby)
        {
            // Reset for next round
            hasSpawnedThisRound = false;
            spawnedPlayers = 0;
        }
    }

    private void SpawnAllPlayers()
    {
        hasSpawnedThisRound = true;
        spawnedPlayers = 0;

        Debug.Log("[PlayerSpawner] Game started - spawning players for all connections");

        foreach (NetworkConnection conn in networkManager.ServerManager.Clients.Values)
        {
            if (conn.IsActive)
            {
                SpawnPlayerForConnection(conn);
            }
        }

        Debug.Log($"[PlayerSpawner] Spawned {spawnedPlayers} players");
    }

    private void SpawnPlayerForConnection(NetworkConnection conn)
    {
        Vector3 spawnPosition = spawnOffset * spawnedPlayers;

        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        networkManager.ServerManager.Spawn(playerInstance, conn);

        // Apply Lobby data directly from LobbyManager (same scene, no caching needed)
        if (LobbyManager.Instance != null)
        {
            var playerData = LobbyManager.Instance.GetPlayerData();
            if (playerData.TryGetValue(conn.ClientId, out PlayerLobbyData lobbyData))
            {
                PlayerController controller = playerInstance.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.ApplyName(lobbyData.playerName);
                    controller.ApplyColor(lobbyData.playerColor);
                    Debug.Log($"[PlayerSpawner] Applied lobby data: {lobbyData.playerName}, color: {lobbyData.playerColor}");
                }
            }
        }

        spawnedPlayers++;
        Debug.Log($"[PlayerSpawner] Spawned player for connection {conn.ClientId} at {spawnPosition}");
    }
}
