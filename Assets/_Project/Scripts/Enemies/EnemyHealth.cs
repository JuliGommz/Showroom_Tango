/*
====================================================================
* EnemyHealth - Enemy Health & Death System
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
* - Health values (Chaser: 30 HP, Shooter: 50 HP set in prefab)
* - Score value (10 points per kill)
* 
* [AI-ASSISTED]
* - Server-authority damage pattern
* - Spawn protection (500ms invulnerability)
* - Score attribution logic (player kills vs kamikaze)
* 
* [AI-GENERATED]
* - NetworkBehaviour FishNet implementation
* - SyncVar for networked health synchronization
* - ScoreManager integration
* - FishNet despawn pattern
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour, SyncVar)
* - ScoreManager (score tracking and attribution)
* 
* NOTES:
* - Spawn protection prevents instant despawn during network init
* - Supports both player kills and kamikaze deaths
* - Team score awarded even for non-player kills
====================================================================
*/

using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class EnemyHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 30;

    [Header("Score Value")]
    [SerializeField] private int scoreValue = 10;

    private readonly SyncVar<int> currentHealth = new SyncVar<int>();
    private bool isInitialized = false;
    private float spawnTime;

    private const float SPAWN_PROTECTION_DURATION = 0.5f;

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentHealth.Value = maxHealth;
        spawnTime = Time.time;
        isInitialized = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsServerStarted)
        {
            isInitialized = true;
        }
    }

    void Update()
    {
        if (IsServerStarted && !isInitialized)
        {
            if (Time.time >= spawnTime + SPAWN_PROTECTION_DURATION)
            {
                isInitialized = true;
            }
        }
    }

    [Server]
    public void TakeDamage(int damage, GameObject attackerPlayer = null)
    {
        if (!isInitialized) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            Die(attackerPlayer);
        }
    }

    [Server]
    private void Die(GameObject killerPlayer = null)
    {
        if (ScoreManager.Instance != null)
        {
            if (killerPlayer != null)
            {
                ScoreManager.Instance.AddKillScore(killerPlayer);
            }
            else
            {
                // Kamikaze death - team score only, no individual credit
                ScoreManager.Instance.AddKillScore(null);
            }
        }
        else
        {
            Debug.LogWarning("[EnemyHealth] ScoreManager not found - score not awarded");
        }

        ServerManager.Despawn(gameObject);
    }

    public int GetCurrentHealth() => currentHealth.Value;
    public int GetMaxHealth() => maxHealth;
}
