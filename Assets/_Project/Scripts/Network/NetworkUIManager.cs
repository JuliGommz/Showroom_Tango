/*
 * Filename: NetworkUIManager.cs
 * Author: Julian
 * Date: 17.12.2025
 * Description: Simple UI for starting Host or Client.
 */

using FishNet.Managing;
using UnityEngine;

public class NetworkUIManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;

    public void StartHost()
    {
        if (networkManager != null)
        {
            networkManager.ServerManager.StartConnection();
            networkManager.ClientManager.StartConnection();
            Debug.Log("Host started!");
        }
    }

    public void StartClient()
    {
        if (networkManager != null)
        {
            networkManager.ClientManager.StartConnection();
            Debug.Log("Client started!");
        }
    }
}