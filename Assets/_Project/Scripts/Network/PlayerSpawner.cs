/*
====================================================================
* PlayerSpawner - Player Spawning System
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-12-18
* Version: 2.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Auto-spawn strategy on game start
* - Spawn point positioning (2-unit offset)
* - Single-scene architecture decision
* 
* [AI-ASSISTED]
* - FishNet server connection event handling
* - Server-authority spawn pattern
* - GameStateManager event subscription
* - LobbyManager data integration
* 
* [AI-GENERATED]
* - Complete NetworkConnection iteration
* - Player data application pattern
* 
* DEPENDENCIES:
* - FishNet.Managing (NetworkManager)
* - FishNet.Connection (NetworkConnection)
* - GameStateManager (state change events)
* - LobbyManager (player customization data)
* - PlayerController (name/color application)
* 
* NOTES:
* - Spawns players when GameState transitions to Playing
* - Reads player data from LobbyManager (same scene)
* - Despawn handled by GameStateManager restart sequence
* - Retry mechanism for GameStateManager subscription
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

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
        else
        {
            // Retry if GameStateManager not ready (initialization race condition)
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
            hasSpawnedThisRound = false;
            spawnedPlayers = 0;
        }
    }

    private void SpawnAllPlayers()
    {
        hasSpawnedThisRound = true;
        spawnedPlayers = 0;

        foreach (NetworkConnection conn in networkManager.ServerManager.Clients.Values)
        {
            if (conn.IsActive)
            {
                SpawnPlayerForConnection(conn);
            }
        }
    }

    private void SpawnPlayerForConnection(NetworkConnection conn)
    {
        Vector3 spawnPosition = spawnOffset * spawnedPlayers;
        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        networkManager.ServerManager.Spawn(playerInstance, conn);

        // Apply lobby customization data (name, color)
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
                }
            }
        }

        spawnedPlayers++;
    }
}
