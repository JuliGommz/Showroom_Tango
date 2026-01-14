/*
====================================================================
* EnemyShooter.cs - Ranged Enemy AI
====================================================================
* Project: Bullet_Love
* Developer: Julian Gomez
* Date: 2025-01-14
* Version: 1.0
* 
* [HUMAN-AUTHORED]
* - Keep distance behavior (5-7 units optimal)
* - Shooting interval (2 seconds)
* - Movement speed (2 units/second, slower than Chaser)
====================================================================
*/

using UnityEngine;
using FishNet.Object;
using System.Collections;

public class EnemyShooter : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float optimalDistance = 6f;
    [SerializeField] private float tooCloseDistance = 5f;
    [SerializeField] private float tooFarDistance = 7f;

    [Header("Combat Settings")]
    [SerializeField] private float fireRate = 2f;

    private Transform targetPlayer;
    private Rigidbody2D rb;
    private float lastFireTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        FindNearestPlayer();
    }

    void FixedUpdate()
    {
        if (!IsServer) return;

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
            Vector2 toPlayer = (targetPlayer.position - transform.position).normalized;
            direction = new Vector2(-toPlayer.y, toPlayer.x);
        }

        Vector2 newPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    private void TryShoot()
    {
        if (Time.time < lastFireTime + fireRate) return;

        lastFireTime = Time.time;

        Debug.Log($"[EnemyShooter] BANG! (Bullet system Phase 4 later)");
    }
}