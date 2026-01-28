/*
====================================================================
* GameStateManager.cs - Manages Game States and Flow
====================================================================
* Project: Showroom_Tango
* Course: PRG - Game & Multimedia Design SRH Hochschule
* Developer: Julian Gomez
* Date: 2025-01-20
* Version: 2.0 - Single-scene architecture (Lobby+Game merged)
*
* WICHTIG: KOMMENTIERUNG NICHT LOESCHEN!
*
* [HUMAN-AUTHORED]
* - Game state requirements (Lobby, Playing, GameOver, Victory)
* - 3-wave victory condition
* - Both-players-dead game over condition
* - Single-scene architecture decision
*
* [AI-ASSISTED]
* - NetworkBehaviour implementation
* - SyncVar state synchronization
* - Server-authority state transitions
* - Event system for UI integration
* - State-gating: lobby/game object visibility control
*
* [AI-GENERATED]
* - Complete implementation structure
====================================================================
*/

using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public enum GameState
{
    Lobby,
    Playing,
    GameOver,
    Victory
}

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Game State")]
    private readonly SyncVar<GameState> currentState = new SyncVar<GameState>(GameState.Lobby);

    [Header("References")]
    [SerializeField] private EnemySpawner enemySpawner;

    [Header("Single-Scene: State-Gated Objects")]
    [Tooltip("Root object containing all Lobby UI (Lobby_Canvas)")]
    [SerializeField] private GameObject lobbyRoot;
    [Tooltip("Root object containing all Game-world objects (HUD, enemies, etc.)")]
    [SerializeField] private GameObject gameRoot;

    // Events for UI to subscribe to
    public delegate void GameStateChanged(GameState newState);
    public event GameStateChanged OnGameStateChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        currentState.OnChange += HandleStateChange;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        currentState.OnChange -= HandleStateChange;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Apply current state visibility on client join
        ApplyStateVisibility(currentState.Value);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Start in Lobby state - do NOT auto-start game
        currentState.Value = GameState.Lobby;
        ApplyStateVisibility(GameState.Lobby);
    }

    void Update()
    {
        if (!IsServerStarted) return;

        if (currentState.Value == GameState.Playing)
        {
            CheckVictoryCondition();
            CheckGameOverCondition();
        }
    }

    /// <summary>
    /// Called by LobbyManager when countdown completes.
    /// Transitions from Lobby -> Playing state.
    /// </summary>
    [Server]
    public void StartGameFromLobby()
    {
        if (currentState.Value != GameState.Lobby)
        {
            Debug.LogWarning("[GameStateManager] StartGameFromLobby called but not in Lobby state");
            return;
        }

        Debug.Log("[GameStateManager] Transitioning Lobby -> Playing");
        currentState.Value = GameState.Playing;
    }

    [Server]
    private void CheckVictoryCondition()
    {
        if (enemySpawner == null) return;

        if (enemySpawner.GetCurrentWave() >= 3 && !enemySpawner.IsWaveActive())
        {
            int enemiesRemaining = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (enemiesRemaining == 0)
            {
                currentState.Value = GameState.Victory;
                Debug.Log("[GameStateManager] VICTORY!");
            }
        }
    }

    [Server]
    private void CheckGameOverCondition()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players.Length == 0)
        {
            currentState.Value = GameState.GameOver;
            Debug.Log("[GameStateManager] GAME OVER - No players remaining");
            return;
        }

        int alivePlayers = 0;
        foreach (GameObject playerObj in players)
        {
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();
            if (health != null && health.IsAlive())
            {
                alivePlayers++;
            }
        }

        if (alivePlayers == 0)
        {
            currentState.Value = GameState.GameOver;
            Debug.Log("[GameStateManager] GAME OVER - All players dead");
        }
    }

    private void HandleStateChange(GameState prev, GameState next, bool asServer)
    {
        Debug.Log($"[GameStateManager] State changed: {prev} -> {next}");
        ApplyStateVisibility(next);
        OnGameStateChanged?.Invoke(next);

        // ✅ NEW: Sync audio with game state
        if (GameAudioManager.Instance != null)
        {
            switch (next)
            {
                case GameState.Lobby:
                    GameAudioManager.Instance.SetGameState(GameAudioManager.GameState.Lobby);
                    break;
                case GameState.Playing:
                    GameAudioManager.Instance.SetGameState(GameAudioManager.GameState.Playing);
                    break;
            }
        }
        else
        {
            Debug.LogWarning("[GameStateManager] GameAudioManager.Instance is NULL - cannot sync audio");
        }
    }


    /// <summary>
    /// Controls which root objects are visible based on game state.
    /// Lobby: lobby UI visible, game world hidden.
    /// Playing/GameOver/Victory: lobby UI hidden, game world visible.
    /// </summary>
    private void ApplyStateVisibility(GameState state)
    {
        bool isLobby = (state == GameState.Lobby);

        if (lobbyRoot != null)
        {
            lobbyRoot.SetActive(isLobby);
        }

        if (gameRoot != null)
        {
            gameRoot.SetActive(!isLobby);
        }
    }

    /// <summary>
    /// Request restart from any client
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestRestartServerRpc()
    {
        Debug.Log("[GameStateManager] Restart requested - returning to Lobby state");
        StartCoroutine(RestartGameSequence());
    }

    [Server]
    private System.Collections.IEnumerator RestartGameSequence()
    {
        // Step 1: Reset score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        // Step 2: Despawn all players
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            FishNet.Object.NetworkObject nob = player.GetComponent<FishNet.Object.NetworkObject>();
            if (nob != null && nob.IsSpawned)
            {
                ServerManager.Despawn(nob);
            }
        }

        // Step 3: Destroy all enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            FishNet.Object.NetworkObject nob = enemy.GetComponent<FishNet.Object.NetworkObject>();
            if (nob != null && nob.IsSpawned)
            {
                ServerManager.Despawn(nob);
            }
            else
            {
                Destroy(enemy);
            }
        }

        // Step 4: Stop enemy spawner
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }

        yield return null;

        // Step 5: Return to Lobby state
        currentState.Value = GameState.Lobby;

        // Step 6: Re-register players in lobby
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.ResetLobby();
        }

        Debug.Log("[GameStateManager] Restart complete - back to Lobby");
    }

    // Public getters
    public GameState GetCurrentState() => currentState.Value;
    public bool IsLobby() => currentState.Value == GameState.Lobby;
    public bool IsPlaying() => currentState.Value == GameState.Playing;
    public bool IsGameOver() => currentState.Value == GameState.GameOver;
    public bool IsVictory() => currentState.Value == GameState.Victory;
}
