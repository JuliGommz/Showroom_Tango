/*
====================================================================
* Bullet.cs - Projectile Movement and Lifetime
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
* - Lifetime requirement (5 seconds max)
* - Speed values (bullet-hell pacing)
* 
* [AI-ASSISTED]
* - NetworkBehaviour implementation
* - Pooling return integration
* - Rotation for star bullet
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

    private float lifetimeTimer;
    private BulletPool ownerPool;

    public void Initialize(BulletPool pool)
    {
        ownerPool = pool;
        lifetimeTimer = 0f;
    }

    private void Update()
    {
        if (!IsServer) return;

        // Move forward
        transform.position += transform.up * speed * Time.deltaTime;

        // Rotate if enabled (for star bullet)
        if (rotateWhileMoving)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }

        // Lifetime check
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= lifetime)
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
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        // Hit detection will be implemented here in Phase 4
        // For now, just return to pool on any collision
        ReturnToPool();
    }
}