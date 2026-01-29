/*
====================================================================
* EnemyShooter - Ranged Enemy AI
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-14
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Keep distance behavior (optimal: 6 units, too close: 4, too far: 10)
* - Shooting interval (2.5 seconds)
* - Movement speed (2 units/second, slower than Chaser)
* - Combat parameters (5 bullets, 10 damage, 8 speed, 15 range)
* 
* [AI-ASSISTED]
* - Patrol system when in optimal range
* - Target update optimization (0.5s intervals)
* - Spawn protection (0.5s invulnerability)
* 
* [AI-GENERATED]
* - NetworkBehaviour FishNet implementation
* - 360° star pattern bullet spawning
* - BulletPool integration
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour)
* - BulletPool (object pooling)
* - PlayerHealth (target validation)
* - EnemySpawner (bullet pool reference)
* 
* NOTES:
* - Patrols when in optimal range to avoid predictable behavior
* - Backs away if player too close, advances if too far
====================================================================
*/

using UnityEngine;
using FishNet.Object;

public class EnemyShooter : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float optimalDistance = 6f;
    [SerializeField] private float tooCloseDistance = 4f;
    [SerializeField] private float tooFarDistance = 10f;
    [SerializeField] private float shootingRange = 15f;

    [Header("Combat Settings")]
    [SerializeField] private float fireRate = 2.5f;
    [SerializeField] private int bulletCount = 5;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private int bulletDamage = 10;

    private Transform targetPlayer;
    private Rigidbody2D rb;
    private float lastFireTime;
    private BulletPool bulletPool;
    private bool isInitialized = false;
    private float spawnTime;
    private float targetUpdateTimer = 0f;
    private Vector2 patrolTarget;
    private float patrolTimer = 0f;

    private const float SPAWN_PROTECTION_DURATION = 0.5f;
    private const float TARGET_UPDATE_INTERVAL = 0.5f;
    private const float PATROL_CHANGE_INTERVAL = 3f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        spawnTime = Time.time;
        isInitialized = false;
        FindNearestPlayer();
        PickNewPatrolTarget();

        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            bulletPool = spawner.GetEnemyBulletPool();
        }
        else
        {
            Debug.LogWarning("[EnemyShooter] No EnemySpawner found - bullets will not be pooled");
        }
    }

    void FixedUpdate()
    {
        if (!IsServerStarted) return;

        if (!isInitialized)
        {
            if (Time.time >= spawnTime + SPAWN_PROTECTION_DURATION)
            {
                isInitialized = true;
            }
            else
            {
                return;
            }
        }

        targetUpdateTimer += Time.fixedDeltaTime;
        if (targetUpdateTimer >= TARGET_UPDATE_INTERVAL)
        {
            targetUpdateTimer = 0f;
            FindNearestPlayer();
        }

        if (targetPlayer != null)
        {
            PlayerHealth health = targetPlayer.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead())
            {
                targetPlayer = null;
                FindNearestPlayer();
            }
        }

        if (targetPlayer == null)
        {
            FindNearestPlayer();
            return;
        }

        HandleMovement();
        TryShoot();
    }

    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return;

        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject player in players)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null && health.IsDead()) continue;

            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = player.transform;
            }
        }

        targetPlayer = closest;
    }

    private void HandleMovement()
    {
        float distance = Vector2.Distance(transform.position, targetPlayer.position);
        Vector2 direction;

        if (distance < tooCloseDistance)
        {
            direction = (transform.position - targetPlayer.position).normalized;
        }
        else if (distance > tooFarDistance)
        {
            direction = (targetPlayer.position - transform.position).normalized;
        }
        else
        {
            patrolTimer += Time.fixedDeltaTime;
            if (patrolTimer >= PATROL_CHANGE_INTERVAL)
            {
                patrolTimer = 0f;
                PickNewPatrolTarget();
            }

            direction = (patrolTarget - rb.position).normalized;

            if (Vector2.Distance(rb.position, patrolTarget) < 1f)
            {
                PickNewPatrolTarget();
            }
        }

        Vector2 newPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    private void PickNewPatrolTarget()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(2f, 5f);
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        patrolTarget = rb.position + offset;
    }

    private void TryShoot()
    {
        if (Time.time < lastFireTime + fireRate) return;
        if (bulletPrefab == null) return;
        if (targetPlayer == null) return;

        float distance = Vector2.Distance(transform.position, targetPlayer.position);
        if (distance > shootingRange) return;

        lastFireTime = Time.time;

        float angleStep = 360f / bulletCount;
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = i * angleStep;
            float angleInRadians = angle * Mathf.Deg2Rad;
            Vector2 bulletDirection = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));

            float bulletAngle = Mathf.Atan2(bulletDirection.y, bulletDirection.x) * Mathf.Rad2Deg;
            Quaternion bulletRotation = Quaternion.Euler(0, 0, bulletAngle - 90f);

            GameObject bullet;

            if (bulletPool != null)
            {
                bullet = bulletPool.GetBullet(transform.position, bulletRotation);
            }
            else
            {
                bullet = Instantiate(bulletPrefab, transform.position, bulletRotation);
                ServerManager.Spawn(bullet);
            }

            Bullet bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(bulletPool);
            }
        }
    }
}
