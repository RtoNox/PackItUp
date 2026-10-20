using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public static class HostLeavingMessage
{
    public const string MessageName = "HostLeavingRoom";

    private static NetworkManager registeredManager;

    public static void Register()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null || nm.CustomMessagingManager == null)
            return;

        if (registeredManager == nm)
            return;

        Unregister();

        nm.CustomMessagingManager.RegisterNamedMessageHandler(
            MessageName,
            OnHostLeaving
        );

        registeredManager = nm;
    }

    public static void Unregister()
    {
        if (registeredManager != null &&
            registeredManager.CustomMessagingManager != null)
        {
            registeredManager.CustomMessagingManager
                .UnregisterNamedMessageHandler(MessageName);
        }

        registeredManager = null;
    }

    private static void OnHostLeaving(
        ulong senderClientId,
        FastBufferReader reader)
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null || nm.IsServer)
            return;

        if (senderClientId != NetworkManager.ServerClientId)
            return;

        Debug.Log("Host is leaving. Returning to MainMenu.");

        SharedSession.CurrentSession = null;

        if (nm.IsListening)
            nm.Shutdown();

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (SceneManager.GetActiveScene().name != "MainMenu")
            SceneManager.LoadScene("MainMenu");
    }
}