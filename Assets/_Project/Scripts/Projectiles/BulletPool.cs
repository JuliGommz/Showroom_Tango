/*
====================================================================
* BulletPool.cs - Object Pooling System for Projectiles
====================================================================
* Project: Bullet_Love
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
        GameObject bullet;

        if (pool.Count > 0)
        {
            bullet = pool.Dequeue();
        }
        else
        {
            Debug.LogWarning($"[BulletPool] Pool empty, creating new bullet for {bulletPrefab.name}");
            bullet = CreateNewBullet();
        }

        bullet.transform.position = position;
        bullet.transform.rotation = rotation;
        bullet.SetActive(true);

        // Spawn on network
        ServerManager.Spawn(bullet);

        return bullet;
    }

    /// <summary>
    /// Returns bullet to pool
    /// </summary>
    [Server]
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null) return;

        ServerManager.Despawn(bullet);
        bullet.SetActive(false);
        bullet.transform.SetParent(poolParent);
        pool.Enqueue(bullet);
    }
}