/*
====================================================================
* LobbyManager - Player Setup and Ready System
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-23
* Version: 1.3
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
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
* - Ready-up state machine with cancellation logic
* - Countdown coroutine lifecycle management
* - FishNet scene loading integration
* - Player disconnection handling
* 
* [AI-GENERATED]
* - Complete lobby management structure
* - Event-driven architecture
* 
* DEPENDENCIES:
* - FishNet (NetworkBehaviour, SyncVar, SyncDictionary)
* - GameStateManager (scene transition)
* 
* CRITICAL FIXES (v1.3):
* - Fixed infinite loop in OnPlayerDataChanged callback
* - Fixed coroutine cleanup on component destruction
* - Fixed player disconnection countdown cancellation
* - Replaced Unity SceneManager with FishNet SceneManager
* 
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

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }
    public static event System.Action OnInstanceReady;

    [Header("Settings")]
    [SerializeField] private int maxPlayers = 2;
    [SerializeField] private float countdownDuration = 3f;

    [Header("Player Data")]
    private readonly SyncDictionary<int, PlayerLobbyData> playerDataDict = new SyncDictionary<int, PlayerLobbyData>();
    private readonly SyncVar<bool> countdownActive = new SyncVar<bool>(false);
    private readonly SyncVar<int> countdownTime = new SyncVar<int>(3);

    private Coroutine countdownCoroutine;

    public delegate void LobbyStateChanged();
    public event LobbyStateChanged OnLobbyStateChanged;

    public delegate void CountdownTick(int secondsRemaining);
    public event CountdownTick OnCountdownTick;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        Color defaultColor = new Color(0.667f, 0f, 0.784f, 1f);
        RegisterPlayerServerRpc(defaultName, defaultColor);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterPlayerServerRpc(string playerName, Color playerColor, NetworkConnection conn = null)
    {
        if (conn == null)
        {
            Debug.LogError("[LobbyManager] RegisterPlayerServerRpc: Connection is null!");
            return;
        }

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
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerNameServerRpc(string newName, NetworkConnection conn = null)
    {
        if (conn == null) return;

        if (playerDataDict.TryGetValue(conn.ClientId, out PlayerLobbyData data))
        {
            data.playerName = newName;
            playerDataDict[conn.ClientId] = data;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerColorServerRpc(Color newColor, NetworkConnection conn = null)
    {
        if (conn == null)
        {
            Debug.LogWarning("[LobbyManager] UpdatePlayerColorServerRpc: Connection is null - ignoring");
            return;
        }

        if (playerDataDict.TryGetValue(conn.ClientId, out PlayerLobbyData data))
        {
            data.playerColor = newColor;
            playerDataDict[conn.ClientId] = data;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReadyServerRpc(NetworkConnection conn = null)
    {
        if (conn == null) return;

        if (playerDataDict.TryGetValue(conn.ClientId, out PlayerLobbyData data))
        {
            data.isReady = !data.isReady;
            playerDataDict[conn.ClientId] = data;
            CheckAllPlayersReady();
        }
    }

    [Server]
    private void CheckAllPlayersReady()
    {
        if (playerDataDict.Count < maxPlayers)
        {
            if (countdownActive.Value)
            {
                CancelCountdown();
            }
            return;
        }

        bool allReady = true;
        foreach (var kvp in playerDataDict)
        {
            if (!kvp.Value.isReady)
            {
                allReady = false;
                break;
            }
        }

        if (allReady && !countdownActive.Value)
        {
            StartCountdown();
        }
        else if (!allReady && countdownActive.Value)
        {
            CancelCountdown();
        }
    }

    [Server]
    private void StartCountdown()
    {
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
    }

    [ObserversRpc]
    private void RpcNotifyCountdownCancelled() { }

    [Server]
    private IEnumerator StartGameCountdown()
    {
        countdownActive.Value = true;

        for (int i = (int)countdownDuration; i > 0; i--)
        {
            countdownTime.Value = i;
            yield return new WaitForSeconds(1f);
        }

        countdownTime.Value = 0;
        StartGame();
    }

    [Server]
    private void StartGame()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.StartGameFromLobby();
        }
        else
        {
            Debug.LogError("[LobbyManager] GameStateManager not found!");
        }

        // Music automatically switches when scene changes (GameAudioManager is scene-based)
        // No manual music control needed - GameAudioManager.OnSceneLoaded handles it
    }

    [Server]
    public void ResetLobby()
    {
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

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (IsServerStarted)
        {
            CheckAllPlayersReady();
        }
    }

    private void OnDestroy()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }

    private void OnPlayerDataChanged(SyncDictionaryOperation op, int key, PlayerLobbyData value, bool asServer)
    {
        OnLobbyStateChanged?.Invoke();
    }

    private void OnCountdownStateChanged(bool prev, bool next, bool asServer)
    {
        OnLobbyStateChanged?.Invoke();
    }

    private void OnCountdownTimeChanged(int prev, int next, bool asServer)
    {
        OnCountdownTick?.Invoke(next);
    }

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
    public int playerIndex;
    public string playerName;
    public Color playerColor;
    public bool isReady;
}
