using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine.SceneManagement;
using TMPro;

#if UNITY_EDITOR
using ParrelSync;
#endif

public class RoomJoin : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField roomIDInput;
    [SerializeField] private TMP_Text roomIDText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private GameObject joinButton;

    [Header("Game Settings")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string roomSceneName = "RoomScene";

    private ISession currentSession;

    private bool servicesReady;
    private bool isJoining;

    private async void Start()
    {
        if (statusText != null)
            statusText.text = "Initializing services...";

        if (joinButton != null)
            joinButton.SetActive(true);

        await InitializeUnityServices();
    }

    public void JoinToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // =========================================================
    // UNITY SERVICES
    // =========================================================

    private async Task InitializeUnityServices()
    {
        try
        {
            InitializationOptions options =
                new InitializationOptions();

#if UNITY_EDITOR
            if (ClonesManager.IsClone())
            {
                string cloneArgument = ClonesManager.GetArgument();

                options.SetProfile(
                    "Clone_" + cloneArgument + "_Profile"
                );

                Debug.Log(
                    "ParrelSync Clone Profile: Clone_"
                    + cloneArgument + "_Profile"
                );
            }
            else
            {
                options.SetProfile("Original_Profile");

                Debug.Log("Original Unity Editor Profile");
            }
#endif

            if (UnityServices.State !=
                ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }

            servicesReady = true;

            Debug.Log(
                "Player ID: "
                + AuthenticationService.Instance.PlayerId
            );

            if (statusText != null)
                statusText.text = "Ready to join a room";
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Unity Services initialization failed!"
            );

            Debug.LogException(e);

            if (statusText != null)
                statusText.text = "Initialization failed";
        }
    }

    // =========================================================
    // JOIN ROOM
    // =========================================================

    public async void JoinRoom()
    {
        if (isJoining)
            return;

        if (!servicesReady)
        {
            if (statusText != null)
                statusText.text = "Services are not ready";

            return;
        }

        if (roomIDInput == null)
        {
            Debug.LogError(
                "Room ID Input is not assigned!"
            );

            return;
        }

        string roomCode =
            roomIDInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(roomCode))
        {
            if (statusText != null)
                statusText.text = "Enter a room code";

            return;
        }

        // NetworkManager must exist before joining.
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "NetworkManager is missing. "
                + "It must exist before joining the session."
            );

            if (statusText != null)
                statusText.text = "NetworkManager is missing";

            return;
        }

        isJoining = true;

        if (joinButton != null)
            joinButton.SetActive(false);

        if (statusText != null)
            statusText.text = "Joining room...";

        try
        {
            // Join the host's existing session.
            currentSession =
                await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(roomCode);

            if (currentSession == null)
            {
                throw new Exception(
                    "JoinSessionByCodeAsync returned null."
                );
            }

            Debug.Log(
                "Joined session: " + currentSession.Code
            );

            if (roomIDText != null)
            {
                roomIDText.text =
                    "Room ID: " + currentSession.Code;
            }

            currentSession.Changed += OnSessionChanged;

            UpdatePlayerCount();

            if (statusText != null)
                statusText.text =
                    "Session joined. Connecting to host...";

            // Wait for the session's Netcode connection.
            bool connected = await WaitForNetworkConnection(20);

            if (!connected)
            {
                throw new Exception(
                    "Session joined, but the Netcode client "
                    + "did not connect within 20 seconds. "
                    + "Check the Multiplayer Services network integration."
                );
            }

            Debug.Log("Connected to host through Netcode.");

            if (statusText != null)
                statusText.text = "Connected! Opening room...";

            // Load RoomScene locally for this client.
            // Do not use NetworkManager.SceneManager here,
            // because that would request a network-wide scene change.
            SceneManager.LoadScene(roomSceneName);
        }
        catch (Exception e)
        {
            Debug.LogError("JOIN ROOM FAILED");
            Debug.LogException(e);

            if (statusText != null)
                statusText.text =
                    "Could not join room. Check the code and connection.";

            if (joinButton != null)
                joinButton.SetActive(true);
        }
        finally
        {
            isJoining = false;
        }
    }

    // =========================================================
    // WAIT FOR NETWORK CONNECTION
    // =========================================================

    private async Task<bool> WaitForNetworkConnection(
        int timeoutSeconds)
    {
        float startTime = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - startTime <
               timeoutSeconds)
        {
            NetworkManager networkManager =
                NetworkManager.Singleton;

            if (networkManager != null &&
                networkManager.IsClient &&
                networkManager.IsConnectedClient)
            {
                return true;
            }

            await Task.Delay(100);
        }

        return false;
    }

    // =========================================================
    // SESSION CHANGED
    // =========================================================

    private void OnSessionChanged()
    {
        UpdatePlayerCount();
    }

    // =========================================================
    // PLAYER COUNT
    // =========================================================

    private void UpdatePlayerCount()
    {
        if (currentSession == null ||
            playerCountText == null)
        {
            return;
        }

        playerCountText.text =
            "Players: "
            + currentSession.Players.Count
            + " / "
            + maxPlayers;
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (currentSession != null)
        {
            currentSession.Changed -= OnSessionChanged;
        }
    }
}