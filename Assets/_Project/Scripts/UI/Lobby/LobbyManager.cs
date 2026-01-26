/*
====================================================================
* LobbyManager - Player Setup and Ready System
====================================================================
* Project: Showroom_Tango (2-Player Top-Down Bullet-Hell)
* Course: PRG - Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-23
* Version: 1.0 - Initial lobby implementation
*
* WICHTIG: KOMMENTIERUNG NICHT LOESCHEN!
* Diese detaillierte Authorship-Dokumentation ist fuer die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
*
* AUTHORSHIP CLASSIFICATION:
*
* [HUMAN-AUTHORED]
* - Lobby flow requirements (name/color selection, ready system)
* - Player limit (2 players)
* - Countdown duration (3 seconds)
* - Scene transition to Game scene
*
* [AI-ASSISTED]
* - NetworkBehaviour implementation
* - SyncVar synchronization for player data
* - Ready-up state machine
* - Countdown coroutine
* - Scene loading integration
*
* [AI-GENERATED]
* - Complete lobby management structure
*
* DEPENDENCIES:
* - FishNet.Object.NetworkBehaviour
* - FishNet.Managing.SceneManagement
* - PlayerLobbyData (custom data structure)
*
* NOTES:
* - Server-authority for game start
* - Both players must be ready before countdown
* - Player data persists to Game scene via PlayerController SyncVars
====================================================================
*/

using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LobbyManager - v1.1 - Added null-safety for menu transitions
/// </summary>
public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // EVENT-DRIVEN: Notify when singleton ready
    public static event System.Action OnInstanceReady;

    [Header("Settings")]
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private string gameSceneName = "Game";

    [Header("Player Data")]
    private readonly SyncDictionary<int, PlayerLobbyData> playerDataDict = new SyncDictionary<int, PlayerLobbyData>();
    private readonly SyncVar<bool> countdownActive = new SyncVar<bool>(false);
    private readonly SyncVar<int> countdownTime = new SyncVar<int>(3);

    // Events for UI
    public delegate void LobbyStateChanged();
    public event LobbyStateChanged OnLobbyStateChanged;

    public delegate void CountdownTick(int secondsRemaining);
    public event CountdownTick OnCountdownTick;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[LobbyManager] Instance created");

            // Fire event for subscribers
            OnInstanceReady?.Invoke();
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        playerDataDict.OnChange += OnPlayerDataChanged;
        countdownActive.OnChange += OnCountdownStateChanged;
        countdownTime.OnChange += OnCountdownTimeChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        playerDataDict.OnChange -= OnPlayerDataChanged;
        countdownActive.OnChange -= OnCountdownStateChanged;
        countdownTime.OnChange -= OnCountdownTimeChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        string defaultName = "Player";
        Color defaultColor = new Color(0.667f, 0f, 0.784f, 1f); // Magenta default

        RegisterPlayerServerRpc(defaultName, defaultColor);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterPlayerServerRpc(string playerName, Color playerColor, NetworkConnection conn = null)
    {
        // NULL SAFETY: Guard against missing connection
        if (conn == null)
        {
            Debug.LogError("[LobbyManager] RegisterPlayerServerRpc: Connection is null!");
            return;
        }

        // Guard: prevent duplicate registration
        if (playerDataDict.ContainsKey(conn.ClientId))
        {
            Debug.LogWarning($"[LobbyManager] Player {conn.ClientId} already registered");
            return;
        }

        int playerIndex = playerDataDict.Count;
        if (playerIndex >= maxPlayers)
        {
            Debug.LogWarning($"[LobbyManager] Cannot register player - lobby full ({maxPlayers} max)");
            return;
        }

        PlayerLobbyData data = new PlayerLobbyData
        {
            connectionId = conn.ClientId,
            playerIndex = playerIndex,
            playerName = playerName,
            playerColor = playerColor,
            isReady = false
        };

        playerDataDict.Add(conn.ClientId, data);
        Debug.Log($"[LobbyManager] Player registered: {playerName} (Index {playerIndex}, Color {playerColor})");
        OnLobbyStateChanged?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerNameServerRpc(string newName, NetworkConnection conn = null)
    {
        // NULL SAFETY
        if (conn == null) return;

        if (playerDataDict.TryGetValue(conn.ClientId, out PlayerLobbyData data))
        {
            data.playerName = newName;
            playerDataDict[conn.ClientId] = data;
            Debug.Log($"[LobbyManager] Player {conn.ClientId} name updated: {newName}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerColorServerRpc(Color newColor, NetworkConnection conn = null)
    {
        // NULL SAFETY
        if (conn == null)
        {
            Debug.LogWarning("[LobbyManager] UpdatePlayerColorServerRpc: Connection is null - ignoring");
            return;
        }

        if (playerDataDict.TryGetValue(conn.ClientId, out PlayerLobbyData data))
        {
            data.playerColor = newColor;
            playerDataDict[conn.ClientId] = data;
            Debug.Log($"[LobbyManager] Player {conn.ClientId} color updated: {newColor}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(NetworkConnection conn = null)
    {
        // NULL SAFETY
        if (conn == null) return;

        if (playerDataDict.TryGetValue(conn.ClientId, out PlayerLobbyData data))
        {
            data.isReady = !data.isReady;
            playerDataDict[conn.ClientId] = data;
            Debug.Log($"[LobbyManager] Player {conn.ClientId} ready: {data.isReady}");

            CheckAllPlayersReady();
        }
    }

    [Server]
    private void CheckAllPlayersReady()
    {
        if (playerDataDict.Count < maxPlayers)
        {
            Debug.Log($"[LobbyManager] Waiting for more players ({playerDataDict.Count}/{maxPlayers})");
            return;
        }

        foreach (var kvp in playerDataDict)
        {
            if (!kvp.Value.isReady)
            {
                Debug.Log($"[LobbyManager] Player {kvp.Key} not ready yet");
                return;
            }
        }

        // All players ready - start countdown
        Debug.Log("[LobbyManager] All players ready! Starting countdown");
        StartCoroutine(StartGameCountdown());
    }

    [Server]
    private IEnumerator StartGameCountdown()
    {
        countdownActive.Value = true;

        for (int i = (int)countdownDuration; i > 0; i--)
        {
            countdownTime.Value = i;
            Debug.Log($"[LobbyManager] Starting in {i}...");
            yield return new WaitForSeconds(1f);
        }

        countdownTime.Value = 0;
        Debug.Log("[LobbyManager] Countdown complete - loading Game scene");

        StartGame();
    }

    [Server]
    private void StartGame()
    {
        // Load Game scene for all clients
        UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        Debug.Log($"[LobbyManager] Loading scene: {gameSceneName}");
    }

    // Event handlers
    private void OnPlayerDataChanged(SyncDictionaryOperation op, int key, PlayerLobbyData value, bool asServer)
    {
        Debug.Log($"[LobbyManager] Player data changed: {op} for player {key}");
        OnLobbyStateChanged?.Invoke();
    }

    private void OnCountdownStateChanged(bool prev, bool next, bool asServer)
    {
        Debug.Log($"[LobbyManager] Countdown active: {next}");
        OnLobbyStateChanged?.Invoke();
    }

    private void OnCountdownTimeChanged(int prev, int next, bool asServer)
    {
        Debug.Log($"[LobbyManager] Countdown: {next}");
        OnCountdownTick?.Invoke(next);
    }

    // Public getters
    public Dictionary<int, PlayerLobbyData> GetPlayerData()
    {
        Dictionary<int, PlayerLobbyData> result = new Dictionary<int, PlayerLobbyData>();
        foreach (var kvp in playerDataDict)
        {
            result.Add(kvp.Key, kvp.Value);
        }
        return result;
    }

    public bool IsCountdownActive() => countdownActive.Value;
    public int GetCountdownTime() => countdownTime.Value;
    public int GetPlayerCount() => playerDataDict.Count;
}

[System.Serializable]
public struct PlayerLobbyData
{
    public int connectionId;
    public int playerIndex; // 0 = Player 1, 1 = Player 2
    public string playerName;
    public Color playerColor;
    public bool isReady;
}
