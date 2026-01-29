/*
====================================================================
* PlayerSetupUI - Ownership-Based Lobby Panel Controller
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-23
* Version: 1.1
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Two-panel lobby design (Player 1 left, Player 2 right)
* - Name input (8 char limit)
* - Color selection (6 presets)
* - Ready button requirement
* 
* [AI-ASSISTED]
* - Ownership determination (disable other player's panel)
* - NetworkConnection-based ServerRpc calls
* - Event-driven countdown display
* - Coroutine for network initialization
* - UpdateFromServerData method (added 2026-01-29)
* 
* [AI-GENERATED]
* - Complete UI synchronization logic
* 
* DEPENDENCIES:
* - LobbyManager (ServerRpc calls)
* - FishNet NetworkManager
* - TextMeshPro
* 
* NOTES:
* - Each client sees BOTH panels but can only interact with THEIR panel
* - Ownership determined by playerIndex vs connection registry
* - Ready state disables name/color inputs
====================================================================
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Managing;
using FishNet.Connection;

public class PlayerSetupUI : MonoBehaviour
{
    [Header("Player Identity")]
    [SerializeField] private int playerIndex = 0;
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
    [SerializeField] private GameObject interactableElements;

    private readonly Color[] colorPresets = new Color[]
    {
        new Color(0.667f, 0f, 0.784f, 1f), // Magenta
        new Color(0f, 1f, 1f, 1f),         // Cyan
        new Color(1f, 1f, 0f, 1f),         // Yellow
        new Color(0f, 1f, 0f, 1f),         // Green
        new Color(1f, 0f, 0f, 1f),         // Red
        new Color(0f, 0.5f, 1f, 1f)        // Blue
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

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        SetInteractable(false);

        // Event-driven initialization
        if (LobbyManager.Instance != null)
        {
            InitializeWithLobbyManager();
        }
        else
        {
            LobbyManager.OnInstanceReady += InitializeWithLobbyManager;
        }
    }

    void OnDestroy()
    {
        LobbyManager.OnInstanceReady -= InitializeWithLobbyManager;

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnCountdownTick -= UpdateCountdownDisplay;
            LobbyManager.Instance.OnLobbyStateChanged -= OnLobbyStateChanged;
        }
    }

    private void InitializeWithLobbyManager()
    {
        LobbyManager.OnInstanceReady -= InitializeWithLobbyManager;

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnCountdownTick += UpdateCountdownDisplay;
            LobbyManager.Instance.OnLobbyStateChanged += OnLobbyStateChanged;
        }

        StartCoroutine(WaitForNetwork());
    }

    private System.Collections.IEnumerator WaitForNetwork()
    {
        networkManager = FindAnyObjectByType<NetworkManager>();
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
        yield return new WaitForSeconds(0.5f);

        DetermineOwnership();

        // Send initial data only if this is MY panel
        if (isMyPanel && lobbyManager != null)
        {
            if (nameInputField != null)
            {
                lobbyManager.UpdatePlayerNameServerRpc(nameInputField.text);
            }
            lobbyManager.UpdatePlayerColorServerRpc(selectedColor);
        }
    }

    private void DetermineOwnership()
    {
        if (networkManager == null || networkManager.ClientManager == null)
        {
            Debug.LogWarning("[PlayerSetupUI] Cannot determine ownership - NetworkManager not ready");
            return;
        }

        myConnectionId = networkManager.ClientManager.Connection.ClientId;
        var playerData = lobbyManager.GetPlayerData();

        isMyPanel = false;
        foreach (var kvp in playerData)
        {
            if (kvp.Value.playerIndex == playerIndex && kvp.Key == myConnectionId)
            {
                isMyPanel = true;
                break;
            }
        }

        SetInteractable(isMyPanel);
    }

    private void SetInteractable(bool interactable)
    {
        if (interactableElements != null)
        {
            interactableElements.SetActive(interactable);
        }
    }

    private void SetupColorButtons()
    {
        if (colorButtons == null) return;

        for (int i = 0; i < colorButtons.Length && i < colorPresets.Length; i++)
        {
            int index = i;
            if (colorButtons[i] != null)
            {
                Image btnImage = colorButtons[i].GetComponent<Image>();
                if (btnImage != null)
                {
                    btnImage.color = colorPresets[i];
                }
                colorButtons[i].onClick.AddListener(() => OnColorSelected(index));
            }
        }
    }

    private void OnNameChanged(string newName)
    {
        if (!isMyPanel || lobbyManager == null) return;
        lobbyManager.UpdatePlayerNameServerRpc(newName);
    }

    private void OnColorSelected(int colorIndex)
    {
        if (!isMyPanel || lobbyManager == null) return;

        selectedColor = colorPresets[colorIndex];
        if (selectedColorIndicator != null)
        {
            selectedColorIndicator.color = selectedColor;
        }
        lobbyManager.UpdatePlayerColorServerRpc(selectedColor);
    }

    private void OnReadyButtonClicked()
    {
        if (!isMyPanel || lobbyManager == null) return;
        lobbyManager.ToggleReadyServerRpc();
    }

    private void OnLobbyStateChanged()
    {
        if (lobbyManager == null) return;

        var playerData = lobbyManager.GetPlayerData();
        PlayerLobbyData? myData = null;

        foreach (var kvp in playerData)
        {
            if (kvp.Value.playerIndex == playerIndex)
            {
                myData = kvp.Value;
                break;
            }
        }

        if (!myData.HasValue) return;

        bool isReady = myData.Value.isReady;

        if (readyButtonText != null)
            readyButtonText.text = isReady ? "CANCEL" : "READY";

        if (readyButton != null)
        {
            ColorBlock colors = readyButton.colors;
            colors.normalColor = isReady ? readyColor : notReadyColor;
            readyButton.colors = colors;
        }

        // Disable controls when ready (only for MY panel)
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

        // Show/hide countdown
        bool countdownActive = lobbyManager.IsCountdownActive();
        if (countdownPanel != null)
        {
            countdownPanel.SetActive(countdownActive);
        }
    }

    private void UpdateCountdownDisplay(int secondsRemaining)
    {
        if (countdownText != null)
        {
            if (secondsRemaining > 0)
            {
                countdownText.text = $"STARTING IN {secondsRemaining}...";
            }
            else
            {
                countdownText.text = "GO!";
            }
        }
    }

    /// <summary>
    /// Updates UI panel with server-synchronized player data
    /// Called by LobbyUI when player data changes on server
    /// </summary>
    public void UpdateFromServerData(PlayerLobbyData data)
    {
        // Update name display (but not input field if this is my panel and I'm editing)
        if (nameInputField != null && (!isMyPanel || !nameInputField.isFocused))
        {
            nameInputField.text = data.playerName;
        }

        // Update ready button appearance
        if (readyButtonText != null)
            readyButtonText.text = data.isReady ? "CANCEL" : "READY";

        if (readyButton != null)
        {
            ColorBlock colors = readyButton.colors;
            colors.normalColor = data.isReady ? readyColor : notReadyColor;
            readyButton.colors = colors;
        }

        // Lock/unlock controls based on ready state (only for MY panel)
        if (isMyPanel)
        {
            if (nameInputField != null)
                nameInputField.interactable = !data.isReady;

            if (colorButtons != null)
            {
                foreach (Button btn in colorButtons)
                {
                    if (btn != null)
                        btn.interactable = !data.isReady;
                }
            }
        }
    }
}
