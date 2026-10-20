using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using TMPro;

#if UNITY_EDITOR
using ParrelSync;
#endif

public class RoomJoin : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject joinButton;

    private ISession currentSession;
    private bool servicesReady;
    private bool isJoining;

    private async void Start()
    {
        if (statusText != null)
            statusText.text = "Initializing services...";

        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            InitializationOptions options = new InitializationOptions();

#if UNITY_EDITOR
            if (ClonesManager.IsClone())
            {
                string cloneArgument = ClonesManager.GetArgument();

                options.SetProfile(
                    "Clone_" + cloneArgument + "_Profile"
                );

                Debug.Log(
                    "ParrelSync profile: Clone_" +
                    cloneArgument + "_Profile"
                );
            }
            else
            {
                options.SetProfile("Original_Profile");
                Debug.Log("Original Unity Editor profile");
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

            Debug.Log("Unity Services initialized.");
            Debug.Log(
                "Player ID: " +
                AuthenticationService.Instance.PlayerId
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

    public async void JoinRoom()
    {
        if (!servicesReady)
        {
            if (statusText != null)
                statusText.text = "Services are not ready";

            return;
        }

        if (isJoining)
            return;

        if (roomCodeInput == null)
        {
            Debug.LogError(
                "Room code input is not assigned!"
            );

            return;
        }

        // Read the room code before using it in the try block.
        string roomCode = roomCodeInput.text.Trim();

        if (string.IsNullOrEmpty(roomCode))
        {
            if (statusText != null)
                statusText.text = "Please enter a room code";

            return;
        }

        isJoining = true;

        if (joinButton != null)
            joinButton.SetActive(false);

        if (statusText != null)
            statusText.text = "Joining room...";

        try
        {
            // Join the existing session using the entered code.
            currentSession =
                await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(roomCode);

            if (currentSession == null)
            {
                throw new Exception(
                    "Joining the session returned null."
                );
            }

            // Save the session before entering RoomScene.
            SharedSession.CurrentSession = currentSession;

            Debug.Log(
                "Successfully joined room: " +
                currentSession.Code
            );

            if (statusText != null)
                statusText.text = "Joined room. Loading room...";

            // Load RoomScene after the session is saved.
            SceneManager.LoadScene("RoomScene");
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Failed to join room with code: " + roomCode
            );

            Debug.LogException(e);

            if (statusText != null)
                statusText.text =
                    "Failed to join room. Check the code.";

            if (joinButton != null)
                joinButton.SetActive(true);
        }
        finally
        {
            isJoining = false;
        }
    }
}