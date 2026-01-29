/*
====================================================================
* Bullet - Projectile Movement and Lifetime
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-08
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Lifetime requirement (5 seconds max)
* - Speed values (bullet-hell pacing)
* - Max range limit (20 units)
* - Player/Enemy bullet type distinction
* 
* [AI-ASSISTED]
* - NetworkBehaviour implementation
* - Pooling return integration
* - Visual rotation for star bullet (decoupled from movement)
* - Score attribution system (owner tracking)
* 
* [AI-GENERATED]
* - Complete collision detection logic
* - Range checking system
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour)
* - BulletPool (pooling integration)
* - EnemyHealth, PlayerHealth (damage application)
* 
* NOTES:
* - Movement direction captured at spawn (unaffected by visual rotation)
* - Player bullets track owner for score attribution
* - Automatic return to pool on hit or timeout
* - Enemy bullets bypass pooling and despawn directly
====================================================================
*/

using UnityEngine;
using FishNet.Object;

public class Bullet : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private bool rotateWhileMoving = false;
    [SerializeField] private float rotationSpeed = 360f;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float maxRange = 20f;
    [SerializeField] private int damage = 10;

    [Header("Bullet Type")]
    [SerializeField] private bool isPlayerBullet = true;

    private float lifetimeTimer;
    private BulletPool ownerPool;
    private Vector3 spawnPosition;
    private Vector3 movementDirection;
    private GameObject ownerPlayer;

    public void Initialize(BulletPool pool, GameObject player = null)
    {
        ownerPool = pool;
        ownerPlayer = player;
        lifetimeTimer = 0f;
        spawnPosition = transform.position;

        // Capture direction at spawn (before rotation)
        movementDirection = transform.up;
    }

    public GameObject GetOwnerPlayer() => ownerPlayer;

    void FixedUpdate()
    {
        if (!IsServerStarted) return;

        // Move in straight line using stored direction
        transform.position += movementDirection * speed * Time.fixedDeltaTime;

        // Visual rotation (doesn't affect movement)
        if (rotateWhileMoving)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.fixedDeltaTime);
        }

        lifetimeTimer += Time.fixedDeltaTime;
        if (lifetimeTimer >= lifetime)
        {
            ReturnToPool();
            return;
        }

        float distanceTraveled = Vector3.Distance(transform.position, spawnPosition);
        if (distanceTraveled >= maxRange)
        {
            ReturnToPool();
        }
    }

    [Server]
    private void ReturnToPool()
    {
        if (ownerPool != null)
        {
            ownerPool.ReturnBullet(gameObject);
        }
        else
        {
            // Enemy bullets aren't pooled
            ServerManager.Despawn(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServerStarted) return;

        if (isPlayerBullet && collision.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage, ownerPlayer);
            }
            ReturnToPool();
            return;
        }

        if (!isPlayerBullet && collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.ApplyDamage(damage);
            }
            ReturnToPool();
            return;
        }
    }
}
