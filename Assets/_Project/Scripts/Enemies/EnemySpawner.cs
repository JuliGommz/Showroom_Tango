/*
====================================================================
* EnemySpawner - Wave-Based Enemy Spawning System
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-14
* Version: 1.2
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Wave structure (3 waves: 60→67→107 enemies)
* - Spawn timing (45 seconds per wave with ±30% randomization)
* - Enemy ratio (70% Chaser, 30% Shooter)
* - Spawn radius (33 units, min player distance: 5 units)
* 
* [AI-ASSISTED]
* - Circular spawn point calculation
* - Wave progression system
* - ObserversRpc for client notifications (v1.2)
* - GameStateManager integration
* 
* [AI-GENERATED]
* - Server-authority NetworkObject spawning
* - Coroutine-based wave sequencing
* - Static event system for wave cleared notifications
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour, SyncVar)
* - GameStateManager (game state synchronization)
* - BulletPool (enemy bullet management)
* 
* NOTES:
* - Wave clears only when ALL enemies dead (not just spawned)
* - 3 second delay between waves
* - Static event ensures all subscribers receive notifications
====================================================================
*/

using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject chaserPrefab;
    [SerializeField] private GameObject shooterPrefab;

    [Header("Enemy Bullet Pool")]
    [SerializeField] private BulletPool enemyBulletPool;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 33f;
    [SerializeField] private float minSpawnDistance = 5f;
    [SerializeField] private int spawnPointCount = 8;

    [Header("Wave Configuration")]
    private readonly SyncVar<int> currentWave = new SyncVar<int>(1);
    [SerializeField] private int maxWaves = 3;
    [SerializeField] private int[] waveEnemyCounts = { 60, 67, 107 };
    [SerializeField] [Range(0f, 1f)] private float chaserSpawnWeight = 0.7f;

    private bool waveActive = false;

    public static event System.Action<int> OnWaveCleared;

    public override void OnStartServer()
    {
        base.OnStartServer();
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
            yield return StartCoroutine(SpawnWave(wave));

            while (true)
            {
                yield return new WaitForSeconds(0.5f);
                if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
                    break;
            }

            OnWaveCleared?.Invoke(wave);
            NotifyWaveClearedObserversRpc(wave);

            if (wave < maxWaves)
            {
                yield return new WaitForSeconds(3f);
            }
        }
    }

    private IEnumerator SpawnWave(int wave)
    {
        waveActive = true;
        int totalEnemies = waveEnemyCounts[wave - 1];
        float waveDuration = 45f;
        float baseInterval = waveDuration / totalEnemies;

        for (int i = 0; i < totalEnemies; i++)
        {
            SpawnRandomEnemy();

            // Randomize spawn timing to prevent synchronized movement
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

    [ObserversRpc]
    private void NotifyWaveClearedObserversRpc(int clearedWave)
    {
        if (!IsServerStarted)
        {
            OnWaveCleared?.Invoke(clearedWave);
        }
    }

    [Server]
    public void StopSpawning()
    {
        StopAllCoroutines();
        waveActive = false;
    }

    [Server]
    public void RestartWaves()
    {
        currentWave.Value = 1;
        waveActive = false;
        StartCoroutine(WaveSequence());
    }

    public BulletPool GetEnemyBulletPool() => enemyBulletPool;
}
