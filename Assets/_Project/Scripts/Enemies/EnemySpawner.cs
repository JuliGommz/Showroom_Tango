/*
====================================================================
* EnemySpawner.cs - Wave-Based Enemy Spawning System
====================================================================
* Project: Bullet_Love
* Course: PRG - Game & Multimedia Design SRH Hochschule
* Developer: Julian Gomez
* Date: 2025-01-14
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* 
* [HUMAN-AUTHORED]
* - Wave structure (5 waves, 15→25→40→60→100 enemies)
* - Spawn timing (30% burst, 70% trickle)
* - Enemy ratio (70% Chaser, 30% Shooter)
* 
* [AI-ASSISTED]
* - Server-authority spawning
* - Circular spawn point calculation
* - Wave progression system
* - NetworkObject spawn pattern
====================================================================
*/

using FishNet.Object;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditorInternal.ReorderableList;

public class EnemySpawner : NetworkBehaviour
{
    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject chaserPrefab;
    [SerializeField] private GameObject shooterPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private int spawnPointCount = 8;

    [Header("Wave Configuration")]
    [SerializeField] private int currentWave = 1;
    [SerializeField] private int maxWaves = 5;

    private int[] waveEnemyCounts = { 15, 25, 40, 60, 100 };
    private float chaserSpawnWeight = 0.7f;

    private bool waveActive = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[EnemySpawner] Server started - ready to spawn");

        StartCoroutine(WaveSequence());
    }

    private IEnumerator WaveSequence()
    {
        yield return new WaitForSeconds(2f);

        for (int wave = 0; wave < maxWaves; wave++)
        {
            currentWave = wave + 1;
            Debug.Log($"[EnemySpawner] Starting Wave {currentWave}/{maxWaves}");

            yield return StartCoroutine(SpawnWave(currentWave));

            Debug.Log($"[EnemySpawner] Wave {currentWave} complete");

            if (currentWave < maxWaves)
            {
                yield return new WaitForSeconds(5f);
            }
        }

        Debug.Log("[EnemySpawner] All waves complete - VICTORY!");
    }

    private IEnumerator SpawnWave(int wave)
    {
        waveActive = true;

        int totalEnemies = waveEnemyCounts[wave - 1];
        int burstCount = Mathf.RoundToInt(totalEnemies * 0.3f);
        int trickleCount = totalEnemies - burstCount;

        for (int i = 0; i < burstCount; i++)
        {
            SpawnRandomEnemy();
            yield return new WaitForSeconds(0.2f);
        }

        float trickleInterval = 60f / trickleCount;

        for (int i = 0; i < trickleCount; i++)
        {
            SpawnRandomEnemy();
            yield return new WaitForSeconds(trickleInterval);
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
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        float x = Mathf.Cos(angle) * spawnRadius;
        float y = Mathf.Sin(angle) * spawnRadius;

        return new Vector2(x, y);
    }

    public int GetCurrentWave() => currentWave;
    public bool IsWaveActive() => waveActive;
}