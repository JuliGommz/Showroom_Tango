/*
====================================================================
* BulletPool.cs - Object Pooling System for Projectiles
====================================================================
* Project: Showroom_Tango
* Course: PRG - Game & Multimedia Design SRH Hochschule
* Developer: Julian Gomez
* Date: 2025-01-08
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* 
* [HUMAN-AUTHORED]
* - Pooling requirement identified (bullet-hell performance)
* - Pool size decisions (50 per type)
* 
* [AI-ASSISTED]
* - Queue-based pooling implementation
* - FishNet network spawn integration
* - Automatic pool expansion logic
====================================================================
*/

using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;

public class BulletPool : NetworkBehaviour
{
    [Header("Pool Configuration")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int initialPoolSize = 1000;
    [SerializeField] private Transform poolParent;

    private Queue<GameObject> pool = new Queue<GameObject>();

    // Diagnostics
    private int totalSpawned = 0;
    private int totalReturned = 0;
    private int totalExpansions = 0;
    private int nullEncountered = 0;
    private int activeEncountered = 0;

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[BulletPool] OnStartServer called - Starting initialization...");
        InitializePool();
        Debug.Log($"[BulletPool] Pool initialized with {pool.Count} bullets ready");
    }

    private void InitializePool()
    {
        if (poolParent == null)
        {
            poolParent = new GameObject($"{bulletPrefab.name}_Pool").transform;
        }

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBullet();
        }
    }

    private GameObject CreateNewBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, poolParent);
        bullet.SetActive(false);
        pool.Enqueue(bullet);
        return bullet;
    }

    /// <summary>
    /// Gets bullet from pool or creates new one if empty
    /// </summary>
    [Server]
    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        GameObject bullet = null;
        int attempts = 0;

        // Null-check loop with safety limit - re-enqueue invalid bullets
        while (bullet == null && pool.Count > 0 && attempts < 10)
        {
            GameObject candidate = pool.Dequeue();

            if (candidate != null && !candidate.activeInHierarchy)
            {
                // Valid inactive bullet found
                bullet = candidate;
                break;
            }
            else if (candidate != null && candidate.activeInHierarchy)
            {
                // Still active, put it back
                pool.Enqueue(candidate);
                activeEncountered++;
                Debug.LogWarning($"[BulletPool] Encountered active bullet in pool (count: {activeEncountered})");
            }
            else if (candidate == null)
            {
                // Destroyed bullet detected
                nullEncountered++;
                Debug.LogError($"[BulletPool] DIAGNOSTIC: Destroyed bullet found in pool! Total null encounters: {nullEncountered}");
            }

            attempts++;
        }

        // Auto-expansion if pool exhausted
        if (bullet == null)
        {
            bullet = CreateNewBullet();
            totalExpansions++;
            Debug.Log($"[BulletPool] Expanded pool for {bulletPrefab.name} (Total expansions: {totalExpansions})");
        }

        // Safe setup now that bullet is guaranteed valid
        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        bullet.SetActive(true);

        ServerManager.Spawn(bullet);
        totalSpawned++;

        // Periodic diagnostics every 100 spawns
        if (totalSpawned % 100 == 0)
        {
            int activeBullets = GameObject.FindGameObjectsWithTag("Bullet").Length;
            Debug.Log($"[BulletPool] DIAGNOSTICS - Spawned: {totalSpawned}, Returned: {totalReturned}, Active: {activeBullets}, Pool: {pool.Count}, Nulls: {nullEncountered}, Expansions: {totalExpansions}");
        }

        return bullet;
    }

    /// <summary>
    /// Returns bullet to pool
    /// </summary>
    [Server]
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null)
        {
            Debug.LogWarning("[BulletPool] Attempted to return null bullet");
            return;
        }

        ServerManager.Despawn(bullet, DespawnType.Pool); // ✅ Keeps GameObject alive
        bullet.SetActive(false);
        bullet.transform.SetParent(poolParent);
        pool.Enqueue(bullet);
        totalReturned++;
    }
}