using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkButtons : MonoBehaviour
{
    public bool showNetworkMenu = false;

    void OnGUI()
    {
        
        if (!showNetworkMenu) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer) 
        {
            if (GUILayout.Button("Host")) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Server")) NetworkManager.Singleton.StartServer();
            if (GUILayout.Button("Client")) NetworkManager.Singleton.StartClient();
        } 

        GUILayout.EndArea();
    }

    
    public void ShowMenu()
    {
        showNetworkMenu = true;
    }

    public void HideMenu()
    {
        showNetworkMenu = false;
    }
}