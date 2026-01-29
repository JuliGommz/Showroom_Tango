/*
====================================================================
* EnemySpawner.cs - Wave-Based Enemy Spawning System
====================================================================
* Project: Showroom_Tango
* Course: PRG - Game & Multimedia Design SRH Hochschule
* Developer: Julian Gomez
* Date: 2025-01-14
* Version: 1.2 - Added ObserversRpc for wave cleared notification to clients

* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️

* [HUMAN-AUTHORED]
* - Wave structure (3 waves, 15→30→50 enemies)
* - Spawn timing (30% burst, 70% trickle)
* - Enemy ratio (70% Chaser, 30% Shooter)

* [AI-ASSISTED]
* - Server-authority spawning
* - Circular spawn point calculation
* - Wave progression system
* - NetworkObject spawn pattern
* - ObserversRpc for client wave cleared notifications (v1.2)
====================================================================
*/

using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
// REMOVED: using static UnityEditorInternal.ReorderableList; ← Build blocker removed

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject chaserPrefab;
    [SerializeField] private GameObject shooterPrefab;

    [Header("Enemy Bullet Pool")]
    [SerializeField] private BulletPool enemyBulletPool;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 33f; // Increased to match new boundaries (30 + 3 buffer)
    [SerializeField] private float minSpawnDistance = 5f;
    [SerializeField] private int spawnPointCount = 8;

    [Header("Wave Configuration")]
    private readonly SyncVar<int> currentWave = new SyncVar<int>(1);
    [SerializeField] private int maxWaves = 3;
    [SerializeField] private int[] waveEnemyCounts = { 60, 67, 107 };
    [SerializeField] [Range(0f, 1f)] private float chaserSpawnWeight = 0.7f; // 70% Chaser, 30% Shooter
    private bool waveActive = false;

    // Static event fired when a wave is fully cleared (all enemies dead, not just spawned)
    // Static ensures all subscribers receive the event regardless of which instance they found
    public static event System.Action<int> OnWaveCleared; // passes the wave number that was cleared

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[EnemySpawner] Server started - waiting for GameState.Playing");

        // Subscribe to game state changes instead of auto-starting
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (!IsServerStarted) return;

        if (newState == GameState.Playing)
        {
            Debug.Log("[EnemySpawner] Game started - beginning wave sequence");
            RestartWaves();
        }
        else if (newState == GameState.Lobby)
        {
            StopSpawning();
        }
    }

    private IEnumerator WaveSequence()
    {
        yield return new WaitForSeconds(2f);

        for (int wave = 1; wave <= maxWaves; wave++)
        {
            currentWave.Value = wave;
            Debug.Log($"[EnemySpawner] Starting Wave {wave}/{maxWaves}");
            yield return StartCoroutine(SpawnWave(wave));
            Debug.Log($"[EnemySpawner] Wave {wave} spawning complete - waiting for remaining enemies");

            // Wait until ALL enemies are dead (spawning is finished at this point)
            // Poll every 0.5s to avoid frame-perfect false zero counts
            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
                    break;
            }
            Debug.Log($"[EnemySpawner] Wave {wave} cleared!");

            // Fire event for ALL local listeners (including host's UI)
            Debug.Log($"[EnemySpawner] Firing OnWaveCleared event for wave {wave}. Subscribers: {(OnWaveCleared != null ? OnWaveCleared.GetInvocationList().Length : 0)}");
            OnWaveCleared?.Invoke(wave);

            // Notify all clients via RPC (clients will fire their local event)
            NotifyWaveClearedObserversRpc(wave);

            // Delay before next wave (but not after final wave)
            if (wave < maxWaves)
            {
                yield return new WaitForSeconds(3f);
            }
        }

        Debug.Log("[EnemySpawner] All waves complete - Wave counter stays at 3");
    }

    private IEnumerator SpawnWave(int wave)
    {
        waveActive = true;
        int totalEnemies = waveEnemyCounts[wave - 1];

        // Spread spawning evenly over 45 seconds with some randomization (reduced from 60s for faster pacing)
        float waveDuration = 45f;
        float baseInterval = waveDuration / totalEnemies;

        for (int i = 0; i < totalEnemies; i++)
        {
            SpawnRandomEnemy();

            // Add randomization (±30%) to prevent synchronized movement
            float randomFactor = Random.Range(0.7f, 1.3f);
            float delay = baseInterval * randomFactor;
            yield return new WaitForSeconds(delay);
        }

        waveActive = false;
    }

    [Server]
    private void SpawnRandomEnemy()
    {
        Vector2 spawnPosition = GetRandomSpawnPoint();
        GameObject prefabToSpawn;

        if (Random.value < chaserSpawnWeight)
        {
            prefabToSpawn = chaserPrefab;
        }
        else
        {
            prefabToSpawn = shooterPrefab;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError("[EnemySpawner] Enemy prefab is null!");
            return;
        }

        GameObject enemy = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        ServerManager.Spawn(enemy);
    }

    private Vector2 GetRandomSpawnPoint()
    {
        Vector2 spawnPos;
        int attempts = 0;

        do
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * spawnRadius;
            float y = Mathf.Sin(angle) * spawnRadius;
            spawnPos = new Vector2(x, y);
            attempts++;
        }
        while (IsTooCloseToPlayers(spawnPos) && attempts < 10);

        return spawnPos;
    }

    private bool IsTooCloseToPlayers(Vector2 position)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            if (Vector2.Distance(position, player.transform.position) < minSpawnDistance)
            {
                return true;
            }
        }
        return false;
    }

    public int GetCurrentWave() => currentWave.Value;
    public int GetMaxWaves() => maxWaves;
    public bool IsWaveActive() => waveActive;

    /// <summary>
    /// RPC to notify all clients when a wave is cleared.
    /// Clients subscribe to OnWaveCleared event locally.
    /// </summary>
    [ObserversRpc]
    private void NotifyWaveClearedObserversRpc(int clearedWave)
    {
        // Only fire event on clients (server already fired it)
        if (!IsServerStarted)
        {
            Debug.Log($"[EnemySpawner] Client received wave {clearedWave} cleared notification");
            OnWaveCleared?.Invoke(clearedWave);
        }
    }

    /// <summary>
    /// Stop all wave spawning (called during restart)
    /// </summary>
    [Server]
    public void StopSpawning()
    {
        StopAllCoroutines();
        waveActive = false;
        Debug.Log("[EnemySpawner] All spawning stopped");
    }

    /// <summary>
    /// Restart wave sequence (called after restart cleanup)
    /// </summary>
    [Server]
    public void RestartWaves()
    {
        currentWave.Value = 1;
        waveActive = false;
        StartCoroutine(WaveSequence());
        Debug.Log("[EnemySpawner] Wave sequence restarted");
    }

    /// <summary>
    /// Get enemy bullet pool for shooter enemies
    /// </summary>
    public BulletPool GetEnemyBulletPool() => enemyBulletPool;
}
