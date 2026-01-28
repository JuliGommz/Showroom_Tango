/*
====================================================================
* BulletPool.cs - FishNet Native Object Pooling Wrapper
====================================================================
* Project: Showroom_Tango
* Course: PRG - Game & Multimedia Design SRH Hochschule
* Developer: Julian Gomez
* Date: 2025-01-08
* Version: 2.1 - MonoBehaviour (scene object, no NetworkObject needed)
*
* WICHTIG: KOMMENTIERUNG NICHT LOESCHEN!
*
* [HUMAN-AUTHORED]
* - Pooling requirement identified (bullet-hell performance)
* - FishNet native pooling architecture decision
*
* [AI-ASSISTED]
* - FishNet DefaultObjectPool integration
* - ServerManager.Spawn/Despawn with DespawnType.Pool
* - Pre-warming via DefaultObjectPool.CacheObjects
* - MonoBehaviour refactor (scene object does not need NetworkBehaviour)
====================================================================
*/

using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Managing;

public class BulletPool : MonoBehaviour
{
    [Header("Pool Configuration")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int prewarmCount = 200;

    private NetworkObject bulletNetworkPrefab;
    private bool isInitialized = false;

    // Diagnostics
    private int totalSpawned = 0;
    private int totalReturned = 0;

    void Update()
    {
        // Wait for server to be active, then initialize once
        if (!isInitialized)
        {
            NetworkManager nm = InstanceFinder.NetworkManager;
            if (nm != null && nm.IsServerStarted)
            {
                Initialize(nm);
            }
        }
    }

    private void Initialize(NetworkManager nm)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("[BulletPool] bulletPrefab not assigned!");
            isInitialized = true; // Prevent retrying
            return;
        }

        bulletNetworkPrefab = bulletPrefab.GetComponent<NetworkObject>();
        if (bulletNetworkPrefab == null)
        {
            Debug.LogError("[BulletPool] bulletPrefab has no NetworkObject component!");
            isInitialized = true;
            return;
        }

        // Pre-warm FishNet's native pool
        if (nm.ObjectPool != null)
        {
            nm.ObjectPool.CacheObjects(bulletNetworkPrefab, prewarmCount, true);
            Debug.Log($"[BulletPool] Pre-warmed {prewarmCount} bullets in FishNet pool");
        }

        isInitialized = true;
        Debug.Log($"[BulletPool] Initialized: {gameObject.name}");
    }

    /// <summary>
    /// Gets bullet from FishNet's native pool, positions it, and spawns on network.
    /// Called from server context (WeaponManager [ServerRpc] or EnemyShooter [Server]).
    /// </summary>
    public GameObject GetBullet(Vector3 position, Quaternion rotation)
    {
        NetworkManager nm = InstanceFinder.NetworkManager;
        if (nm == null || !nm.IsServerStarted)
        {
            Debug.LogError("[BulletPool] Cannot get bullet - server not active!");
            return null;
        }

        if (bulletNetworkPrefab == null)
        {
            Debug.LogError("[BulletPool] bulletPrefab is null or not initialized!");
            return null;
        }

        // Retrieve from FishNet's native pool (reuses despawned objects)
        NetworkObject nob = nm.GetPooledInstantiated(bulletNetworkPrefab, position, rotation, true);
        GameObject bullet = nob.gameObject;
        nm.ServerManager.Spawn(bullet);

        totalSpawned++;

        if (totalSpawned % 100 == 0)
        {
            Debug.Log($"[BulletPool] DIAGNOSTICS - Spawned: {totalSpawned}, Returned: {totalReturned}");
        }

        return bullet;
    }

    /// <summary>
    /// Returns bullet to FishNet's native pool.
    /// Called from server context.
    /// </summary>
    public void ReturnBullet(GameObject bullet)
    {
        if (bullet == null)
        {
            Debug.LogWarning("[BulletPool] Attempted to return null bullet");
            return;
        }

        NetworkManager nm = InstanceFinder.NetworkManager;
        if (nm == null || !nm.IsServerStarted) return;

        NetworkObject nob = bullet.GetComponent<NetworkObject>();
        if (nob != null && nob.IsSpawned)
        {
            // DespawnType.Pool tells FishNet to store in DefaultObjectPool instead of destroying
            nm.ServerManager.Despawn(nob, DespawnType.Pool);
        }

        totalReturned++;
    }
}
