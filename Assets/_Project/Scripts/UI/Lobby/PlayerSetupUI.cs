using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Managing;
using FishNet.Connection;

public class PlayerSetupUI : MonoBehaviour
{
    [Header("Player Identity")]
    [SerializeField] private int playerIndex = 0; // 0 = Player 1, 1 = Player 2
    [SerializeField] private TextMeshProUGUI tangoLabel;

    [Header("Name Input")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private int maxNameLength = 8;

    [Header("Color Selection")]
    [SerializeField] private Button[] colorButtons;
    [SerializeField] private Image selectedColorIndicator;

    [Header("Ready System")]
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color notReadyColor = Color.gray;

    [Header("Countdown Display")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Ownership Control")]
    [SerializeField] private GameObject interactableElements; // Parent containing buttons/input

    private readonly Color[] colorPresets = new Color[]
    {
        new Color(0.667f, 0f, 0.784f, 1f),  // Magenta
        new Color(0f, 1f, 1f, 1f),          // Cyan
        new Color(1f, 1f, 0f, 1f),          // Yellow
        new Color(0f, 1f, 0f, 1f),          // Green
        new Color(1f, 0f, 0f, 1f),          // Red
        new Color(0f, 0.5f, 1f, 1f)         // Blue
    };

    private Color selectedColor;
    private LobbyManager lobbyManager;
    private NetworkManager networkManager;
    private int myConnectionId = -1;
    private bool isMyPanel = false;

    void Start()
    {
        if (tangoLabel != null)
            tangoLabel.text = $"TANGO {playerIndex + 1}";

        if (nameInputField != null)
        {
            nameInputField.characterLimit = maxNameLength;
            nameInputField.text = $"Player {playerIndex + 1}";
            nameInputField.onValueChanged.AddListener(OnNameChanged);
        }

        SetupColorButtons();

        if (readyButton != null)
            readyButton.onClick.AddListener(OnReadyButtonClicked);

        selectedColor = colorPresets[0];
        if (selectedColorIndicator != null)
            selectedColorIndicator.color = selectedColor;

        // Hide countdown panel initially
        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        // Disable interaction until ownership determined
        SetInteractable(false);

        // EVENT-DRIVEN: Subscribe to LobbyManager ready event
        if (LobbyManager.Instance != null)
        {
            // Already initialized
            InitializeWithLobbyManager();
        }
        else
        {
            // Wait for initialization via event
            LobbyManager.OnInstanceReady += InitializeWithLobbyManager;
        }
    }

    void OnDestroy()
    {
        // Cleanup: Unsubscribe from events to prevent memory leaks
        LobbyManager.OnInstanceReady -= InitializeWithLobbyManager;

        // NULL SAFETY: Check if LobbyManager still exists
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnCountdownTick -= UpdateCountdownDisplay;
            LobbyManager.Instance.OnLobbyStateChanged -= OnLobbyStateChanged;
        }
    }

    private void InitializeWithLobbyManager()
    {
        // Unsubscribe after first call (event fires once)
        LobbyManager.OnInstanceReady -= InitializeWithLobbyManager;

        Debug.Log($"[PlayerSetupUI] Panel {playerIndex} - LobbyManager ready, starting network initialization");

        // Subscribe to countdown events
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnCountdownTick += UpdateCountdownDisplay;
            LobbyManager.Instance.OnLobbyStateChanged += OnLobbyStateChanged;
        }

        StartCoroutine(WaitForNetwork());
    }

    private System.Collections.IEnumerator WaitForNetwork()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("[PlayerSetupUI] NetworkManager not found!");
            yield break;
        }

        float timeout = 10f;
        float elapsed = 0f;

        while (!networkManager.IsClientStarted && elapsed < timeout)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (!networkManager.IsClientStarted)
        {
            Debug.LogError("[PlayerSetupUI] Client not started!");
            yield break;
        }

        elapsed = 0f;
        while (LobbyManager.Instance == null && elapsed < timeout)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (LobbyManager.Instance == null)
        {
            Debug.LogError("[PlayerSetupUI] LobbyManager not found!");
            yield break;
        }

        lobbyManager = LobbyManager.Instance;

        // Wait for player registration
        yield return new WaitForSeconds(0.5f);

        // Determine ownership
        DetermineOwnership();

        // Send initial name and color only if this is MY panel
        if (isMyPanel && lobbyManager != null)
        {
            if (nameInputField != null)
            {
                lobbyManager.UpdatePlayerNameServerRpc(nameInputField.text);
            }
            lobbyManager.UpdatePlayerColorServerRpc(selectedColor);
            Debug.Log($"[PlayerSetupUI] Player {playerIndex + 1} - Initial name/color sent (MY PANEL)");
        }
    }

    private void DetermineOwnership()
    {
        if (networkManager == null || networkManager.ClientManager == null)
        {
            Debug.LogError("[PlayerSetupUI] Cannot determine ownership - NetworkManager not ready");
            return;
        }

        NetworkConnection localConn = networkManager.ClientManager.Connection;
        if (localConn == null)
        {
            Debug.LogError("[PlayerSetupUI] Local connection is null");
            return;
        }

        myConnectionId = localConn.ClientId;

        // Get player count to determine which panel belongs to me
        int playerCount = lobbyManager.GetPlayerCount();

        // First player gets index 0, second player gets index 1
        // My connection determines which panel I own
        var playerData = lobbyManager.GetPlayerData();
        foreach (var kvp in playerData)
        {
            if (kvp.Key == myConnectionId)
            {
                // This connection's playerIndex matches my panel
                isMyPanel = (kvp.Value.playerIndex == playerIndex);
                Debug.Log($"[PlayerSetupUI] Panel {playerIndex} - ConnectionID: {myConnectionId}, IsMyPanel: {isMyPanel}");
                break;
            }
        }

        // Enable interaction ONLY if this is my panel
        SetInteractable(isMyPanel);
    }

    private void SetInteractable(bool interactable)
    {
        // Disable ALL input elements if not my panel
        if (nameInputField != null)
            nameInputField.interactable = interactable;

        if (colorButtons != null)
        {
            foreach (Button btn in colorButtons)
            {
                if (btn != null)
                    btn.interactable = interactable;
            }
        }

        if (readyButton != null)
            readyButton.interactable = interactable;

        // Optional: dim visuals for non-owned panel
        if (interactableElements != null)
        {
            CanvasGroup cg = interactableElements.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = interactableElements.AddComponent<CanvasGroup>();

            cg.alpha = interactable ? 1f : 0.5f; // Dim non-owned panel
        }

        Debug.Log($"[PlayerSetupUI] Panel {playerIndex} - Interactable: {interactable}");
    }

    private void SetupColorButtons()
    {
        if (colorButtons == null || colorButtons.Length == 0) return;

        for (int i = 0; i < colorButtons.Length && i < colorPresets.Length; i++)
        {
            Image buttonImage = colorButtons[i].GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = colorPresets[i];

            int index = i;
            colorButtons[i].onClick.AddListener(() => SelectColor(index));
        }
    }

    private void SelectColor(int colorIndex)
    {
        // OWNERSHIP CHECK: Only send if this is MY panel
        if (!isMyPanel)
        {
            Debug.LogWarning($"[PlayerSetupUI] Panel {playerIndex} - Not my panel, ignoring color selection");
            return;
        }

        if (colorIndex < 0 || colorIndex >= colorPresets.Length) return;

        selectedColor = colorPresets[colorIndex];
        if (selectedColorIndicator != null)
            selectedColorIndicator.color = selectedColor;

        if (lobbyManager != null && networkManager != null && networkManager.IsClientStarted)
        {
            lobbyManager.UpdatePlayerColorServerRpc(selectedColor);
            Debug.Log($"[PlayerSetupUI] Panel {playerIndex} (MY PANEL) - Color sent: {selectedColor}");
        }
    }

    private void OnNameChanged(string newName)
    {
        // OWNERSHIP CHECK
        if (!isMyPanel) return;

        if (lobbyManager != null && networkManager != null && networkManager.IsClientStarted)
        {
            lobbyManager.UpdatePlayerNameServerRpc(newName);
        }
    }

    private void OnReadyButtonClicked()
    {
        // OWNERSHIP CHECK
        if (!isMyPanel) return;

        if (lobbyManager != null && networkManager != null && networkManager.IsClientStarted)
        {
            lobbyManager.ToggleReadyServerRpc();
            Debug.Log($"[PlayerSetupUI] Panel {playerIndex} (MY PANEL) - Ready toggled");
        }
    }

    // Countdown display logic
    private void UpdateCountdownDisplay(int secondsRemaining)
    {
        if (countdownPanel == null) return;

        countdownPanel.SetActive(secondsRemaining > 0);

        if (secondsRemaining > 0 && countdownText != null)
        {
            countdownText.text = secondsRemaining.ToString();
        }
    }

    // Listen to lobby state changes
    private void OnLobbyStateChanged()
    {
        if (lobbyManager == null) return;

        // Hide countdown if cancelled
        if (!lobbyManager.IsCountdownActive() && countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }
    }

    // SERVER-AUTHORITATIVE: Update from server data
    public void UpdateFromServerData(PlayerLobbyData data)
    {
        // Update name
        if (nameInputField != null)
        {
            nameInputField.onValueChanged.RemoveListener(OnNameChanged);
            nameInputField.text = data.playerName;
            nameInputField.onValueChanged.AddListener(OnNameChanged);
        }

        // Update color
        selectedColor = data.playerColor;
        if (selectedColorIndicator != null)
            selectedColorIndicator.color = selectedColor;

        // Update ready visuals
        UpdateReadyVisuals(data.isReady);
    }

    private void UpdateReadyVisuals(bool isReady)
    {
        if (readyButtonText != null)
            readyButtonText.text = isReady ? "CANCEL" : "READY";

        if (readyButton != null)
        {
            ColorBlock colors = readyButton.colors;
            colors.normalColor = isReady ? readyColor : notReadyColor;
            readyButton.colors = colors;
        }

        // Only disable MY panel's controls when ready
        if (isMyPanel)
        {
            if (nameInputField != null)
                nameInputField.interactable = !isReady;

            if (colorButtons != null)
            {
                foreach (Button btn in colorButtons)
                {
                    if (btn != null)
                        btn.interactable = !isReady;
                }
            }
        }
    }
}
