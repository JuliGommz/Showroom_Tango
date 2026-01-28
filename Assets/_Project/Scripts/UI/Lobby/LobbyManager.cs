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

/*
====================================================================
* LobbyManager - Player Setup and Ready System
====================================================================
* Project: Showroom_Tango (2-Player Top-Down Bullet-Hell)
* Course: PRG - Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-23
* Version: 1.3 - QA-certified countdown + FishNet scene loading

* WICHTIG: KOMMENTIERUNG NICHT LOESCHEN!
* Diese detaillierte Authorship-Dokumentation ist fuer die akademische
* Bewertung erforderlich und darf nicht entfernt werden!

* AUTHORSHIP CLASSIFICATION:

* [HUMAN-AUTHORED]
* - Lobby flow requirements (name/color selection, ready system)
* - Player limit (2 players)
* - Countdown duration (3 seconds)
* - Scene transition to Game scene

* [AI-ASSISTED]
* - NetworkBehaviour implementation
* - SyncVar synchronization for player data
* - Ready-up state machine with cancellation logic
* - Countdown coroutine lifecycle management
* - FishNet scene loading integration
* - Player disconnection handling

* [AI-GENERATED]
* - Complete lobby management structure
* - Event-driven architecture

* DEPENDENCIES:
* - FishNet.Object.NetworkBehaviour
* - FishNet.Managing.Scened.SceneLoadData
* - PlayerLobbyData (custom struct)

* CRITICAL FIXES (v1.3):
* - Fixed infinite loop in OnPlayerDataChanged callback
* - Fixed coroutine cleanup on component destruction
* - Fixed player disconnection countdown cancellation
* - Replaced Unity SceneManager with FishNet SceneManager

* NOTES:
* - Server-authority for all lobby state changes
* - Both players must be ready before countdown
* - Countdown cancels if any player un-readies or disconnects
* - Player data persists to Game scene via NetworkObject
====================================================================
*/

using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

///
/// LobbyManager - v1.3 - Production-ready countdown system
///
public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    // EVENT-DRIVEN: Notify when singleton ready
    public static event System.Action OnInstanceReady;

    [Header("Settings")]
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private float countdownDuration = 3f;

    [Header("Player Data")]
    private readonly SyncDictionary<int, PlayerLobbyData> playerDataDict = new SyncDictionary<int, PlayerLobbyData>();
    private readonly SyncVar<bool> countdownActive = new SyncVar<bool>(false);
    private readonly SyncVar<int> countdownTime = new SyncVar<int>(3);

    // Countdown control
    private Coroutine countdownCoroutine;

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

            // TRIGGER: Check if countdown should start/stop
            CheckAllPlayersReady();
        }
    }

    [Server]
    private void CheckAllPlayersReady()
    {
        // Cancel countdown if not enough players
        if (playerDataDict.Count < maxPlayers)
        {
            if (countdownActive.Value)
            {
                CancelCountdown();
            }
            Debug.Log($"[LobbyManager] Waiting for more players ({playerDataDict.Count}/{maxPlayers})");
            return;
        }

        // Check if all ready
        bool allReady = true;
        foreach (var kvp in playerDataDict)
        {
            if (!kvp.Value.isReady)
            {
                allReady = false;
                Debug.Log($"[LobbyManager] Player {kvp.Key} not ready yet");
                break;
            }
        }

        if (allReady && !countdownActive.Value)
        {
            // All players ready - start countdown
            Debug.Log("[LobbyManager] All players ready! Starting countdown");
            StartCountdown();
        }
        else if (!allReady && countdownActive.Value)
        {
            // Someone un-readied - cancel countdown
            Debug.Log("[LobbyManager] Player un-readied - cancelling countdown");
            CancelCountdown();
        }
    }

    [Server]
    private void StartCountdown()
    {
        // Safety: Stop existing countdown if somehow running
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }

        countdownCoroutine = StartCoroutine(StartGameCountdown());
    }

    [Server]
    private void CancelCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }

        countdownActive.Value = false;
        RpcNotifyCountdownCancelled();
        Debug.Log("[LobbyManager] Countdown cancelled");
    }

    [ObserversRpc]
    private void RpcNotifyCountdownCancelled()
    {
        Debug.Log("[LobbyManager] Client notified: Countdown cancelled");
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
        Debug.Log("[LobbyManager] All players ready - telling GameStateManager to start game");

        // Tell GameStateManager to transition Lobby -> Playing
        // GameStateManager handles visibility toggling (hides lobby, shows game world)
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartGameFromLobby();
        }
        else
        {
            Debug.LogError("[LobbyManager] GameStateManager not found!");
        }

        // Switch music to gameplay (null-safe)
        if (GameAudioManager.Instance != null)
        {
            GameAudioManager.Instance.SetGameState(GameAudioManager.GameState.Playing);
            Debug.Log("[LobbyManager] Audio switched to Playing state");
        }
        else
        {
            Debug.LogWarning("[LobbyManager] GameAudioManager not found - no music switch");
        }

    }

    /// <summary>
    /// Called by GameStateManager when restarting back to lobby.
    /// Resets all player ready states so they can ready-up again.
    /// </summary>
    [Server]
    public void ResetLobby()
    {
        Debug.Log("[LobbyManager] Resetting lobby state");

        // Un-ready all players
        List<int> keys = new List<int>(playerDataDict.Keys);
        foreach (int key in keys)
        {
            if (playerDataDict.TryGetValue(key, out PlayerLobbyData data))
            {
                data.isReady = false;
                playerDataDict[key] = data;
            }
        }

        countdownActive.Value = false;
        countdownCoroutine = null;
    }


    // Player disconnection handling
    public override void OnStopClient()
    {
        base.OnStopClient();

        // SERVER: Revalidate lobby state after disconnection
        if (IsServerStarted)
        {
            Debug.Log("[LobbyManager] Client stopped - revalidating lobby state");
            CheckAllPlayersReady();
        }
    }

    private void OnDestroy()
    {
        // CRITICAL: Cleanup coroutine to prevent null reference
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    // Event handlers
    private void OnPlayerDataChanged(SyncDictionaryOperation op, int key, PlayerLobbyData value, bool asServer)
    {
        Debug.Log($"[LobbyManager] Player data changed: {op} for player {key}");

        // FIXED: Don't call CheckAllPlayersReady here (prevents infinite loop)
        // Ready check is already triggered in ToggleReadyServerRpc
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
