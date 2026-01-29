/*
====================================================================
* WeaponManager - Multi-Weapon System Controller
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-08
* Version: 1.1
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - 3-weapon slot limitation
* - Priority targeting system (nearest, 2nd nearest, 3rd nearest)
* - Directional offset strategy
* 
* [AI-ASSISTED]
* - Auto-targeting implementation
* - Weapon slot management
* - Enemy proximity detection with sorting
* - Local space projectile offset calculation
* 
* [AI-GENERATED]
* - Complete NetworkBehaviour integration
* - BulletPool delayed finding pattern
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour)
* - BulletPool (object pooling)
* - WeaponConfig (ScriptableObject data)
* - PlayerHealth (death state checking)
* 
* NOTES:
* - Auto-fire system (no manual shooting required)
* - Priority-based targeting distributes fire across multiple enemies
* - Local space offset ensures proper bullet positioning regardless of rotation
====================================================================
*/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FishNet.Object;

public class WeaponManager : NetworkBehaviour
{
    [Header("Weapon Slots (Max 3)")]
    [SerializeField] private List<WeaponConfig> equippedWeapons = new List<WeaponConfig>();

    [Header("References")]
    [SerializeField] private BulletPool bulletPool;
    [SerializeField] private Transform playerTransform;

    [Header("Enemy Detection")]
    [SerializeField] private LayerMask enemyLayer;

    private Dictionary<WeaponConfig, float> weaponLastFireTime = new Dictionary<WeaponConfig, float>();
    private PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();

        foreach (var weapon in equippedWeapons)
        {
            weaponLastFireTime[weapon] = 0f;
        }

        if (bulletPool == null)
        {
            StartCoroutine(FindBulletPoolDelayed());
        }
    }

    private System.Collections.IEnumerator FindBulletPoolDelayed()
    {
        // Wait for NetworkObjects to initialize (race condition fix)
        float timeout = 5f;
        float elapsed = 0f;

        while (bulletPool == null && elapsed < timeout)
        {
            BulletPool[] allPools = FindObjectsByType<BulletPool>(FindObjectsSortMode.None);
            foreach (BulletPool pool in allPools)
            {
                if (pool.gameObject.name.Contains("Player"))
                {
                    bulletPool = pool;
                    yield break;
                }
            }

            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }

        if (bulletPool == null)
        {
            Debug.LogError("[WeaponManager] Player BulletPool not found after timeout! Weapon system disabled.");
            enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (playerHealth != null && playerHealth.IsDead()) return;

        AutoFireAllWeapons();
    }

    private void AutoFireAllWeapons()
    {
        for (int i = 0; i < equippedWeapons.Count; i++)
        {
            WeaponConfig weapon = equippedWeapons[i];

            if (Time.time < weaponLastFireTime[weapon] + weapon.CurrentFireRate) continue;

            List<GameObject> enemiesInRange = FindEnemiesInRange(weapon.range);
            if (enemiesInRange.Count == 0) continue;

            GameObject target = GetTargetForWeapon(i, enemiesInRange);
            if (target == null) continue;

            // Transform offset to player's local space
            Vector3 rotatedOffset = playerTransform.TransformDirection(weapon.firePointOffset);
            Vector3 firePosition = playerTransform.position + rotatedOffset;

            Vector2 directionToTarget = (target.transform.position - firePosition).normalized;
            float baseAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            float finalAngle = baseAngle + weapon.directionAngleOffset;
            Quaternion rotation = Quaternion.Euler(0, 0, finalAngle - 90f);

            weaponLastFireTime[weapon] = Time.time;
            FireWeaponServerRpc(firePosition, rotation, weapon.bulletSprite.name);
        }
    }

    private List<GameObject> FindEnemiesInRange(float range)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            playerTransform.position,
            range,
            enemyLayer
        );

        List<GameObject> enemies = new List<GameObject>();
        foreach (var col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                enemies.Add(col.gameObject);
            }
        }

        // Sort by distance for priority targeting
        enemies = enemies.OrderBy(e =>
            Vector2.Distance(playerTransform.position, e.transform.position)
        ).ToList();

        return enemies;
    }

    private GameObject GetTargetForWeapon(int weaponIndex, List<GameObject> enemies)
    {
        // Fallback to nearest if more weapons than enemies
        if (weaponIndex >= enemies.Count)
        {
            return enemies[0];
        }

        return enemies[weaponIndex];
    }

    [ServerRpc]
    private void FireWeaponServerRpc(Vector3 position, Quaternion rotation, string bulletSpriteName)
    {
        if (bulletPool == null)
        {
            Debug.LogError("BulletPool reference missing!");
            return;
        }

        GameObject bullet = bulletPool.GetBullet(position, rotation);
        if (bullet != null)
        {
            SpriteRenderer sr = bullet.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Sprite sprite = Resources.Load<Sprite>($"Sprites/{bulletSpriteName}");
                if (sprite != null) sr.sprite = sprite;
            }

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(bulletPool, gameObject);
            }
        }
    }

    public bool AddWeapon(WeaponConfig weapon)
    {
        if (equippedWeapons.Count >= 3)
        {
            Debug.LogWarning("Maximum 3 weapons equipped!");
            return false;
        }

        equippedWeapons.Add(weapon);
        weaponLastFireTime[weapon] = Time.time;
        return true;
    }

    public void UpgradeFireRate()
    {
        foreach (var weapon in equippedWeapons)
        {
            weapon.fireRateUpgrades++;
        }
    }

    public void UpgradeCooldown()
    {
        foreach (var weapon in equippedWeapons)
        {
            weapon.cooldownUpgrades++;
        }
    }
}
