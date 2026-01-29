/*
====================================================================
* EnemyChaser - Melee Enemy AI
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
* - Simple follow behavior concept
* - Movement speed (3 units/second)
* - Collision damage (20 HP)
* - Kamikaze behavior (dies on collision)
* 
* [AI-ASSISTED]
* - Speed randomization (±30%) to prevent horde synchronization
* - Random offset system (0.5-1.5 units) to prevent stacking
* - Target update optimization (0.5s intervals)
* 
* [AI-GENERATED]
* - NetworkBehaviour FishNet implementation
* - Server-authority movement pattern
* - Collision damage integration with PlayerController
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour)
* - PlayerController (damage application)
* - EnemyHealth (self-destruction)
* - PlayerHealth (target validation)
* 
* NOTES:
* - Dies immediately on player collision (kamikaze enemy)
* - Speed/offset randomization prevents synchronized movement
====================================================================
*/

using UnityEngine;
using FishNet.Object;

public class EnemyChaser : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float speedVariation = 0.3f;

    [Header("Damage Settings")]
    [SerializeField] private int collisionDamage = 20;
    [SerializeField] private bool diesOnCollision = true;

    private Transform targetPlayer;
    private Rigidbody2D rb;
    private float targetUpdateTimer = 0f;
    private float actualMoveSpeed;
    private Vector2 randomOffset;
    private bool isInitialized = false;
    private float spawnTime;

    private const float TARGET_UPDATE_INTERVAL = 0.5f;
    private const float SPAWN_PROTECTION_DURATION = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("[EnemyChaser] Rigidbody2D missing!");
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        spawnTime = Time.time;
        isInitialized = false;

        // Randomize speed to prevent synchronized horde movement
        actualMoveSpeed = moveSpeed * Random.Range(1f - speedVariation, 1f + speedVariation);

        // Random offset prevents perfect convergence on player
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float offsetDist = Random.Range(0.5f, 1.5f);
        randomOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * offsetDist;

        FindNearestPlayer();
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

        MoveTowardsPlayer();
    }

    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0)
        {
            Debug.LogWarning("[EnemyChaser] No players found!");
            return;
        }

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

    private void MoveTowardsPlayer()
    {
        if (targetPlayer == null) return;

        Vector2 targetPosition = (Vector2)targetPlayer.position + randomOffset;
        Vector2 direction = (targetPosition - rb.position).normalized;
        Vector2 newPosition = rb.position + direction * actualMoveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServerStarted) return;
        if (!isInitialized) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamageServerRpc(collisionDamage);
            }

            if (diesOnCollision)
            {
                EnemyHealth health = GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.TakeDamage(int.MaxValue, attackerPlayer: null);
                }
            }
        }
    }
}
