/*
====================================================================
* GameStateManager - Game State Flow Controller
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-20
* Version: 2.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Game state requirements (Lobby, Playing, GameOver, Victory)
* - 3-wave victory condition
* - Both-players-dead game over condition
* - Single-scene architecture decision
* 
* [AI-ASSISTED]
* - SyncVar state synchronization
* - Server-authority state transitions
* - Event system for UI integration
* - State-gated visibility control
* 
* [AI-GENERATED]
* - NetworkBehaviour FishNet implementation
* - Complete implementation structure
* - Restart sequence coordination
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour, SyncVar)
* - EnemySpawner (wave tracking)
* - ScoreManager (score reset)
* - LobbyManager (player re-registration)
* 
* NOTES:
* - Single-scene architecture: Lobby+Game merged
* - State-gated objects control visibility
* - Server authoritative state machine
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
        ApplyStateVisibility(currentState.Value);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
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

    [Server]
    public void StartGameFromLobby()
    {
        if (currentState.Value != GameState.Lobby)
        {
            Debug.LogWarning("[GameStateManager] StartGameFromLobby called but not in Lobby state");
            return;
        }

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
        }
    }

    private void HandleStateChange(GameState prev, GameState next, bool asServer)
    {
        ApplyStateVisibility(next);
        OnGameStateChanged?.Invoke(next);
    }

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

    [ServerRpc(RequireOwnership = false)]
    public void RequestRestartServerRpc()
    {
        StartCoroutine(RestartGameSequence());
    }

    [Server]
    private System.Collections.IEnumerator RestartGameSequence()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            FishNet.Object.NetworkObject nob = player.GetComponent<FishNet.Object.NetworkObject>();
            if (nob != null && nob.IsSpawned)
            {
                ServerManager.Despawn(nob);
            }
        }

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

        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }

        yield return null;

        currentState.Value = GameState.Lobby;

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.ResetLobby();
        }
    }

    public GameState GetCurrentState() => currentState.Value;
    public bool IsLobby() => currentState.Value == GameState.Lobby;
    public bool IsPlaying() => currentState.Value == GameState.Playing;
    public bool IsGameOver() => currentState.Value == GameState.GameOver;
    public bool IsVictory() => currentState.Value == GameState.Victory;
}
