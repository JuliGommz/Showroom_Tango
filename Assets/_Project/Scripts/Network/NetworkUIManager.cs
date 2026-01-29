/*
====================================================================
* NetworkUIManager - Network Connection UI Controller
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-12-17
* Version: 1.1
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - UI toggle pattern design (Start/Stop functionality)
* - Color feedback strategy (green=connected, white=disconnected)
* - Test-friendly approach (buttons remain visible)
* 
* [AI-ASSISTED]
* - Button reference caching in Awake()
* - Null-safety validation
* 
* [AI-GENERATED]
* - Complete implementation structure
* 
* DEPENDENCIES:
* - FishNet.Managing (NetworkManager)
* - UnityEngine.UI (Button)
* - TMPro (TMP_Text)
* 
* NOTES:
* - Buttons remain visible during testing
* - Color change provides visual connection state feedback
* - Stop functionality allows reconnection without restart
====================================================================
*/

using FishNet.Managing;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkUIManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color disconnectedColor = Color.white;

    private TMP_Text hostButtonText;
    private TMP_Text clientButtonText;
    private Image hostButtonImage;
    private Image clientButtonImage;
    private bool hostRunning = false;
    private bool clientRunning = false;

    void Awake()
    {
        if (hostButton != null)
        {
            hostButtonText = hostButton.GetComponentInChildren<TMP_Text>();
            hostButtonImage = hostButton.GetComponent<Image>();

            if (hostButtonText == null)
            {
                Debug.LogError("NetworkUIManager: Host button text component not found!");
            }
            if (hostButtonImage == null)
            {
                Debug.LogError("NetworkUIManager: Host button image component not found!");
            }
        }
        else
        {
            Debug.LogError("NetworkUIManager: Host button reference is null!");
        }

        if (clientButton != null)
        {
            clientButtonText = clientButton.GetComponentInChildren<TMP_Text>();
            clientButtonImage = clientButton.GetComponent<Image>();

            if (clientButtonText == null)
            {
                Debug.LogError("NetworkUIManager: Client button text component not found!");
            }
            if (clientButtonImage == null)
            {
                Debug.LogError("NetworkUIManager: Client button image component not found!");
            }
        }
        else
        {
            Debug.LogError("NetworkUIManager: Client button reference is null!");
        }

        if (networkManager == null)
        {
            Debug.LogError("NetworkUIManager: NetworkManager reference is null!");
        }
    }

    public void ToggleHost()
    {
        if (!hostRunning)
        {
            StartHost();
        }
        else
        {
            StopHost();
        }
    }

    public void ToggleClient()
    {
        if (!clientRunning)
        {
            StartClient();
        }
        else
        {
            StopClient();
        }
    }

    private void StartHost()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();
            hostRunning = true;

            if (hostButtonText != null)
            {
                hostButtonText.text = "Stop Host";
            }
            if (hostButtonImage != null)
            {
                hostButtonImage.color = connectedColor;
            }
        }
    }

    private void StopHost()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.StopConnection(true);
            networkManager.ClientManager.StopConnection();
            hostRunning = false;

            if (hostButtonText != null)
            {
                hostButtonText.text = "Start Host";
            }
            if (hostButtonImage != null)
            {
                hostButtonImage.color = disconnectedColor;
            }
        }
    }

    private void StartClient()
    {
        if (networkManager != null)
        {
            networkManager.ClientManager.StartConnection();
            clientRunning = true;

            if (clientButtonText != null)
            {
                clientButtonText.text = "Stop Client";
            }
            if (clientButtonImage != null)
            {
                clientButtonImage.color = connectedColor;
            }
        }
    }

    private void StopClient()
    {
        if (networkManager != null)
        {
            networkManager.ClientManager.StopConnection();
            clientRunning = false;

            if (clientButtonText != null)
            {
                clientButtonText.text = "Start Client";
            }
            if (clientButtonImage != null)
            {
                clientButtonImage.color = disconnectedColor;
            }
        }
    }
}
