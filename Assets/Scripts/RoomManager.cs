using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using TMPro;

#if UNITY_EDITOR
using ParrelSync;
#endif

public class RoomManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField roomIDInput;
    [SerializeField] private TMP_Text roomIDText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private GameObject createButton;
    [SerializeField] private GameObject joinButton;
    [SerializeField] private GameObject startButton;

    [Header("Game Settings")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string gameSceneName = "GameScene";

    private ISession currentSession;

    private async void Start()
    {
        // Start button should not be visible initially
        if (startButton != null)
            startButton.SetActive(false);

        await InitializeUnityServices();
    }


    // =========================================================
    // UNITY SERVICES
    // =========================================================

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
                    "ParrelSync Clone Profile: Clone_"
                    + cloneArgument
                    + "_Profile"
                );
            }
            else
            {
                options.SetProfile("Original_Profile");

                Debug.Log(
                    "Original Unity Editor Profile"
                );
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

            Debug.Log(
                "Player ID: "
                + AuthenticationService.Instance.PlayerId
            );

            if (statusText != null)
                statusText.text = "Ready";
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Unity Services initialization failed!"
            );

            Debug.LogException(e);

            if (statusText != null)
                statusText.text =
                    "Failed to initialize";
        }
    }


    // =========================================================
    // CREATE ROOM
    // =========================================================

    public async void CreateRoom()
    {
        Debug.Log("CREATE ROOM");

        if (statusText != null)
            statusText.text = "Creating room...";

        try
        {
            var options = new SessionOptions
            {
                MaxPlayers = maxPlayers
            }.WithRelayNetwork();


            currentSession =
                await MultiplayerService.Instance
                    .CreateSessionAsync(options);


            if (currentSession == null)
            {
                Debug.LogError(
                    "Session is NULL!"
                );

                return;
            }


            string roomID = currentSession.Code;


            Debug.Log(
                "ROOM CREATED: " + roomID
            );


            // Show room ID
            if (roomIDText != null)
            {
                roomIDText.text =
                    "Room ID: " + roomID;
            }


            // Update player count
            UpdatePlayerCount();


            // Hide Create and Join buttons
            if (createButton != null)
                createButton.SetActive(false);

            if (joinButton != null)
                joinButton.SetActive(false);


            // ONLY HOST GETS START BUTTON
            if (startButton != null)
                startButton.SetActive(true);


            // Listen for players joining
            currentSession.Changed +=
                OnSessionChanged;


            if (statusText != null)
                statusText.text =
                    "Room created";
        }
        catch (Exception e)
        {
            Debug.LogError(
                "CREATE ROOM FAILED"
            );

            Debug.LogException(e);

            if (statusText != null)
                statusText.text =
                    "Failed to create room";
        }
    }


    // =========================================================
    // JOIN ROOM
    // =========================================================

    public async void JoinRoom()
    {
        if (roomIDInput == null)
        {
            Debug.LogError(
                "Room ID Input is not assigned!"
            );

            return;
        }


        string roomID =
            roomIDInput.text.Trim().ToUpper();


        if (string.IsNullOrEmpty(roomID))
        {
            if (statusText != null)
                statusText.text =
                    "Enter a Room ID";

            return;
        }


        Debug.Log(
            "JOINING ROOM: " + roomID
        );


        if (statusText != null)
            statusText.text =
                "Joining room...";


        try
        {
            currentSession =
                await MultiplayerService.Instance
                    .JoinSessionByCodeAsync(roomID);


            Debug.Log(
                "JOINED ROOM: " + roomID
            );


            // Show room ID
            if (roomIDText != null)
            {
                roomIDText.text =
                    "Room ID: " + roomID;
            }


            // Update player count
            UpdatePlayerCount();


            // Hide Create and Join buttons
            if (createButton != null)
                createButton.SetActive(false);

            if (joinButton != null)
                joinButton.SetActive(false);


            // IMPORTANT:
            // Client does NOT get Start button
            if (startButton != null)
                startButton.SetActive(false);


            // Listen for player changes
            currentSession.Changed +=
                OnSessionChanged;


            if (statusText != null)
                statusText.text =
                    "Joined room";
        }
        catch (Exception e)
        {
            Debug.LogError(
                "JOIN ROOM FAILED"
            );

            Debug.LogException(e);


            if (statusText != null)
                statusText.text =
                    "Room not found";
        }
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
        if (currentSession == null)
            return;


        if (playerCountText != null)
        {
            playerCountText.text =
                "Players: "
                + currentSession.Players.Count
                + " / "
                + maxPlayers;
        }
    }


    // =========================================================
    // START GAME
    // =========================================================

    public void StartGame()
    {
        if (currentSession == null)
        {
            Debug.LogError(
                "No room exists!"
            );

            return;
        }


        // Make absolutely sure only host can start
        if (!currentSession.IsHost)
        {
            Debug.LogWarning(
                "Only the host can start the game!"
            );

            return;
        }


        Debug.Log(
            "HOST STARTING GAME"
        );


        if (statusText != null)
            statusText.text =
                "Starting game...";


        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "NetworkManager is missing!"
            );

            return;
        }


        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName,
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }
}