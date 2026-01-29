/*
====================================================================
* BulletPool - FishNet Native Object Pooling Wrapper
====================================================================
* Project: Showroom_Tango
* Course: PRG - Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-08
* Version: 2.1
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Pooling requirement identified (bullet-hell performance)
* - FishNet native pooling architecture decision
* 
* [AI-ASSISTED]
* - FishNet DefaultObjectPool integration
* - ServerManager.Spawn/Despawn with DespawnType.Pool
* - Pre-warming via ObjectPool.CacheObjects
* - MonoBehaviour refactor (scene object does not need NetworkBehaviour)
* 
* [AI-GENERATED]
* - Complete implementation
* 
* DEPENDENCIES:
* - FishNet (NetworkManager, ObjectPool, NetworkObject)
* 
* NOTES:
* - MonoBehaviour (not NetworkBehaviour) - scene-persistent pooling system
* - Pre-warms 200 bullets on server start
* - DespawnType.Pool returns objects to pool instead of destroying
* - CS0618 warning about CacheObjects is a FishNet internal issue (ignore)
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

    private int totalSpawned = 0;
    private int totalReturned = 0;

    void Update()
    {
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
            isInitialized = true;
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
        }

        isInitialized = true;
    }

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

        // Retrieve from FishNet's native pool
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
            nm.ServerManager.Despawn(nob, DespawnType.Pool);
        }

        totalReturned++;
    }
}