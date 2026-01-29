/*
====================================================================
* AutoStartNetwork - Automatic Network Connection Starter
====================================================================
* Project: Showroom_Tango
* Course: Game & Multimedia Design
* Developer: Julian
* Date: 2025-12-17
* Version: 1.0
* 
* ⚠️ WICHTIG: KOMMENTIERUNG NICHT LÖSCHEN! ⚠️
* Diese detaillierte Authorship-Dokumentation ist für die akademische
* Bewertung erforderlich und darf nicht entfernt werden!
* 
* AUTHORSHIP CLASSIFICATION:
* 
* [HUMAN-AUTHORED]
* - Auto-start concept (Host vs Client mode)
* - Fallback strategy (Host -> Client if port busy)
* - Skip logic for persistent NetworkManager
* 
* [AI-ASSISTED]
* - Coroutine delay pattern for server binding check
* - NetworkManager lifecycle checks
* 
* [AI-GENERATED]
* - Complete implementation structure
* 
* DEPENDENCIES:
* - FishNet.Managing (NetworkManager)
* 
* NOTES:
* - Automatically starts network on scene load
* - Falls back to client if server port already in use
* - Prevents double-start if NetworkManager persists across scenes
====================================================================
*/

using UnityEngine;
using FishNet.Managing;
using System.Collections;

public class AutoStartNetwork : MonoBehaviour
{
    [Header("Network Role")]
    [Tooltip("True = Start as Host (server+client). False = Start as Client only.")]
    [SerializeField] private bool startAsHost = true;

    void Start()
    {
        NetworkManager nm = FindFirstObjectByType<NetworkManager>();
        if (nm == null)
        {
            Debug.LogError("[AutoStartNetwork] NetworkManager not found!");
            return;
        }

        if (nm.IsServerStarted || nm.IsClientStarted)
        {
            return;
        }

        if (startAsHost)
        {
            StartCoroutine(StartAsHostWithFallback(nm));
        }
        else
        {
            nm.ClientManager.StartConnection();
        }
    }

    private IEnumerator StartAsHostWithFallback(NetworkManager nm)
    {
        nm.ServerManager.StartConnection();

        // Wait for server binding attempt to complete
        yield return new WaitForSeconds(0.5f);

        if (nm.IsServerStarted)
        {
            nm.ClientManager.StartConnection();
        }
        else
        {
            // Fallback to client if port in use
            Debug.LogWarning("[AutoStartNetwork] Server failed to start (port in use?) - falling back to CLIENT only");
            nm.ClientManager.StartConnection();
        }
    }
}
