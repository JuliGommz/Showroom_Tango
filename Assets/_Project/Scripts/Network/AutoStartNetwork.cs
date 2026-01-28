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

        // Skip if already connected (scene was reloaded or NetworkManager persisted)
        if (nm.IsServerStarted || nm.IsClientStarted)
        {
            Debug.Log("[AutoStartNetwork] Network already active - skipping auto-start");
            return;
        }

        if (startAsHost)
        {
            StartCoroutine(StartAsHostWithFallback(nm));
        }
        else
        {
            nm.ClientManager.StartConnection();
            Debug.Log("[AutoStartNetwork] Started as CLIENT only");
        }
    }

    private IEnumerator StartAsHostWithFallback(NetworkManager nm)
    {
        // Attempt to start server
        nm.ServerManager.StartConnection();

        // Wait a frame for server to attempt binding
        yield return new WaitForSeconds(0.5f);

        if (nm.IsServerStarted)
        {
            // Server started successfully - also start client (host mode)
            nm.ClientManager.StartConnection();
            Debug.Log("[AutoStartNetwork] Started as HOST (server + client)");
        }
        else
        {
            // Server failed (port in use) - fall back to client-only
            Debug.LogWarning("[AutoStartNetwork] Server failed to start (port in use?) - falling back to CLIENT only");
            nm.ClientManager.StartConnection();
        }
    }
}
