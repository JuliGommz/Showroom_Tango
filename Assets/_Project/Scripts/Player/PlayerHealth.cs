/*
====================================================================
* PlayerHealth - Player Health & Death System
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-15
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Death behavior (spectate until both players dead)
* - Visual feedback (hide sprites, keep colliders)
* - Max HP (100)
* 
* [AI-ASSISTED]
* - SyncVar HP tracking
* - Server-authority death handling
* - Game over check logic
* 
* [AI-GENERATED]
* - NetworkBehaviour FishNet implementation
* - Complete reset system
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour, SyncVar)
* 
* NOTES:
* - Dead players remain in scene as spectators
* - Sprites hidden but colliders active
* - Game over triggers when all players dead
====================================================================
*/

using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;

    private readonly SyncVar<int> currentHealth = new SyncVar<int>();
    private readonly SyncVar<bool> isDead = new SyncVar<bool>();

    public override void OnStartServer()
    {
        base.OnStartServer();
        currentHealth.Value = maxHealth;
        isDead.Value = false;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        isDead.OnChange += OnDeathStateChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        isDead.OnChange -= OnDeathStateChanged;
    }

    [Server]
    public void ApplyDamage(int damage)
    {
        if (isDead.Value)
        {
            Debug.LogWarning($"[PlayerHealth] {gameObject.name} is already dead, ignoring {damage} damage");
            return;
        }

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            Die();
        }
    }

    [Server]
    private void Die()
    {
        isDead.Value = true;
        CheckGameOver();
    }

    [Server]
    private void CheckGameOver()
    {
        PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        bool allDead = true;

        foreach (var player in allPlayers)
        {
            if (!player.isDead.Value)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            // Handled by GameStateManager
        }
    }

    private void OnDeathStateChanged(bool prev, bool next, bool asServer)
    {
        if (next)
        {
            // Hide sprites for spectator mode
            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sprite in sprites)
            {
                sprite.enabled = false;
            }
        }
    }

    public int GetCurrentHealth() => currentHealth.Value;
    public int GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead.Value;
    public bool IsAlive() => !isDead.Value;

    [ServerRpc(RequireOwnership = false)]
    public void ResetHealthServerRpc()
    {
        currentHealth.Value = maxHealth;
        isDead.Value = false;

        SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sprite in sprites)
        {
            sprite.enabled = true;
        }
    }
}
