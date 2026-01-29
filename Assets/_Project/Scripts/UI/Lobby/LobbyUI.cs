/*
====================================================================
* LobbyUI - Main Lobby Screen Controller
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-01-23
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Lobby layout (2 player panels + center status)
* - Countdown display style
* - Rankings button placement
* - Waiting message format
* 
* [AI-ASSISTED]
* - LobbyManager event subscription
* - Countdown visual updates
* - Player panel synchronization
* - Retry connection pattern
* 
* [AI-GENERATED]
* - Complete UI orchestration logic
* 
* DEPENDENCIES:
* - TMPro (TextMeshPro)
* - LobbyManager (network lobby state)
* - PlayerSetupUI (individual player panels)
* - FishNet.Managing (NetworkManager)
* 
* NOTES:
* - Updates in real-time based on LobbyManager events
* - Countdown uses consistent visual style
* - Rankings button placeholder for future PHP integration
====================================================================
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Managing;

public class LobbyUI : MonoBehaviour
{
    [Header("Player Setup Panels")]
    [SerializeField] private PlayerSetupUI player1SetupUI;
    [SerializeField] private PlayerSetupUI player2SetupUI;

    [Header("Center Status Display")]
    [SerializeField] private GameObject statusPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Color countdownColor = new Color(0.667f, 0f, 0.784f, 1f);

    [Header("Rankings")]
    [SerializeField] private Button rankingsButton;
    [SerializeField] private GameObject rankingsPlaceholderPanel;

    private NetworkManager networkManager;

    void Start()
    {
        networkManager = FindAnyObjectByType<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("[LobbyUI] NetworkManager not found!");
            return;
        }

        if (countdownText != null)
        {
            countdownText.color = countdownColor;
        }

        if (countdownPanel != null)
        {
            countdownPanel.SetActive(false);
        }

        if (rankingsButton != null)
        {
            rankingsButton.onClick.AddListener(OnRankingsButtonClicked);
        }

        if (rankingsPlaceholderPanel != null)
        {
            rankingsPlaceholderPanel.SetActive(false);
        }

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyStateChanged += UpdateLobbyDisplay;
            LobbyManager.Instance.OnCountdownTick += UpdateCountdownDisplay;
        }
        else
        {
            Debug.LogWarning("[LobbyUI] LobbyManager not found yet - will retry");
            Invoke(nameof(RetryLobbyManagerConnection), 1f);
        }

        UpdateLobbyDisplay();
    }

    void OnDestroy()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyStateChanged -= UpdateLobbyDisplay;
            LobbyManager.Instance.OnCountdownTick -= UpdateCountdownDisplay;
        }

        if (rankingsButton != null)
        {
            rankingsButton.onClick.RemoveListener(OnRankingsButtonClicked);
        }
    }

    private void RetryLobbyManagerConnection()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnLobbyStateChanged += UpdateLobbyDisplay;
            LobbyManager.Instance.OnCountdownTick += UpdateCountdownDisplay;
            UpdateLobbyDisplay();
        }
        else
        {
            Debug.LogError("[LobbyUI] LobbyManager still not found!");
        }
    }

    private void UpdateLobbyDisplay()
    {
        if (LobbyManager.Instance == null) return;

        int playerCount = LobbyManager.Instance.GetPlayerCount();
        bool countdownActive = LobbyManager.Instance.IsCountdownActive();

        if (statusText != null)
        {
            if (countdownActive)
            {
                statusText.text = "";
            }
            else if (playerCount < 2)
            {
                statusText.text = $"Waiting for players... ({playerCount}/2)";
            }
            else
            {
                statusText.text = "Waiting for players...";
            }
        }

        if (countdownPanel != null && statusPanel != null)
        {
            countdownPanel.SetActive(countdownActive);
            statusPanel.SetActive(!countdownActive);
        }

        UpdatePlayerPanels();
    }

    private void UpdatePlayerPanels()
    {
        if (LobbyManager.Instance == null) return;

        var playerData = LobbyManager.Instance.GetPlayerData();
        foreach (var kvp in playerData)
        {
            PlayerLobbyData data = kvp.Value;

            if (data.playerIndex == 0 && player1SetupUI != null)
            {
                player1SetupUI.UpdateFromServerData(data);
            }
            else if (data.playerIndex == 1 && player2SetupUI != null)
            {
                player2SetupUI.UpdateFromServerData(data);
            }
        }
    }

    private void UpdateCountdownDisplay(int secondsRemaining)
    {
        if (countdownText != null)
        {
            if (secondsRemaining > 0)
            {
                countdownText.text = secondsRemaining.ToString();
            }
            else
            {
                countdownText.text = "";
            }
        }
    }

    private void OnRankingsButtonClicked()
    {
        if (rankingsPlaceholderPanel != null)
        {
            rankingsPlaceholderPanel.SetActive(true);
        }
    }

    public void CloseRankingsPlaceholder()
    {
        if (rankingsPlaceholderPanel != null)
        {
            rankingsPlaceholderPanel.SetActive(false);
        }
    }
}
