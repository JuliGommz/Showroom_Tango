using UnityEngine;
using FishNet.Managing;

public class AutoStartNetwork : MonoBehaviour
{
    void Start()
    {
        NetworkManager nm = FindObjectOfType<NetworkManager>();
        if (nm != null)
        {
            nm.ServerManager.StartConnection();
            nm.ClientManager.StartConnection();
            Debug.Log("[AutoStart] Host started");
        }
    }
}
