/*
====================================================================
* PlayerController - Player Movement and Network Synchronization
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-20
* Version: 1.3
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Component-first architecture
* - Owner-only input handling strategy
* - SyncVar selection (PlayerName, CurrentHP, PlayerColor)
* - Mouse rotation approach
* - Boundary clamping values (±30x, ±20y)
* 
* [AI-ASSISTED]
* - Unity New Input System integration
* - Server-authority movement pattern
* - FishNet 4.x SyncVar syntax
* - Dead code removal (manual shooting)
* 
* [AI-GENERATED]
* - NetworkBehaviour FishNet implementation
* - Complete event subscription pattern
* 
* DEPENDENCIES:
* - FishNet.Object (NetworkBehaviour, SyncVar)
* - UnityEngine.InputSystem (Unity Input System)
* - WeaponManager (auto-fire system)
* - PlayerHealth (death state)
* 
* NOTES:
* - Uses Unity New Input System
* - Server-authority for position
* - Owner-only input processing
* - Mouse rotation in world space
* - Shooting handled by WeaponManager
====================================================================
*/

using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Synchronized Network Variables")]
    private readonly SyncVar<string> playerName = new SyncVar<string>("Player");
    private readonly SyncVar<int> currentHP = new SyncVar<int>(100);
    private readonly SyncVar<Color> playerColor = new SyncVar<Color>(Color.white);

    private Rigidbody2D rb;
    private Camera mainCamera;
    private PlayerHealth playerHealth;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private Vector2 moveInput;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        playerName.OnChange += OnPlayerNameChanged;
        currentHP.OnChange += OnHPChanged;
        playerColor.OnChange += OnColorChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        playerName.OnChange -= OnPlayerNameChanged;
        currentHP.OnChange -= OnHPChanged;
        playerColor.OnChange -= OnColorChanged;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
        {
            PlayerInput input = GetComponent<PlayerInput>();
            if (input != null)
            {
                input.enabled = false;
            }
            return;
        }

        InitializeInput();
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();

        if (rb == null)
        {
            Debug.LogError("PlayerController: Rigidbody2D component not found!");
        }
        if (playerHealth == null)
        {
            Debug.LogError("PlayerController: PlayerHealth component not found!");
        }
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("PlayerController: Main Camera not found!");
        }
    }

    private void InitializeInput()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            moveAction = playerInput.actions["Movement"];
            if (moveAction == null)
            {
                Debug.LogError("PlayerController: Movement action not found in Input Actions!");
            }
        }
        else
        {
            Debug.LogError("PlayerController: PlayerInput component not found!");
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        if (moveAction != null)
        {
            moveInput = moveAction.ReadValue<Vector2>();
        }

        RotateTowardsMouse();
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        if (rb != null)
        {
            Vector2 movement = moveInput * moveSpeed;
            rb.linearVelocity = movement;

            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -30f, 30f);
            pos.y = Mathf.Clamp(pos.y, -20f, 20f);
            transform.position = pos;
        }
    }

    private void RotateTowardsMouse()
    {
        if (mainCamera == null) return;

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        Vector2 direction = (mouseWorldPos - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Subtract 90 because sprite faces up by default
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    private void OnPlayerNameChanged(string prev, string next, bool asServer) { }
    private void OnHPChanged(int prev, int next, bool asServer) { }

    private void OnColorChanged(Color prev, Color next, bool asServer)
    {
        ApplyColorTint(next);
    }

    private void ApplyColorTint(Color color)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            sr.color = color;
        }
    }

    [Server]
    public void ApplyName(string name)
    {
        playerName.Value = name;
    }

    [Server]
    public void ApplyColor(Color color)
    {
        playerColor.Value = color;
        ApplyColorTint(color);
    }

    [ServerRpc]
    private void SetPlayerColorServerRpc(Color color)
    {
        playerColor.Value = color;
    }

    [ServerRpc]
    public void SetPlayerNameServerRpc(string name)
    {
        playerName.Value = name;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        if (playerHealth != null)
        {
            playerHealth.ApplyDamage(damage);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestGameRestartServerRpc()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.RequestRestartServerRpc();
        }
    }

    public string GetPlayerName() => playerName.Value;
    public int GetCurrentHP() => currentHP.Value;
    public Color GetPlayerColor() => playerColor.Value;
}
