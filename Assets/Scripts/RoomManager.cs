
using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

#if UNITY_EDITOR
using ParrelSync;
#endif

public enum RoomState
{
    RoomScreen,
    AbilityScreen
}

public class RoomManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text roomIDText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private GameObject startButton;

    [Header("Game Settings")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string gameSceneName = "GameScene";

    public GameObject roomScreenUI;
    public GameObject abilityScreenUI;
    public RoomState currentState { get; private set; }

    private ISession currentSession;

    private bool servicesReady;
    private bool creatingRoom;
    private bool roomCreated;

    private async void Start()
    {
        if (startButton != null)
            startButton.SetActive(false);

        if (statusText != null)
            statusText.text = "Initializing services...";

        await InitializeUnityServices();

        if (servicesReady)
            await CreateRoom();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == RoomState.AbilityScreen)
            {
                ChangeState(RoomState.RoomScreen);
            }
        }
    }

    public void ChangeState(RoomState newState)
    {
        StartCoroutine(TransitionToState(newState));

        currentState = newState;
    }

    public void RoomToAbility()
    {
        ChangeState(RoomState.AbilityScreen);
    }

    public void AbilityToRoom()
    {
        ChangeState(RoomState.RoomScreen);
    }

    private IEnumerator TransitionToState(RoomState newState)
    {
        if(newState != RoomState.RoomScreen)
        {
            yield return new WaitForSecondsRealtime(0);
        }

        currentState = newState;
        HandleStateChange();
    }

    private void HandleStateChange()
    {
        HideAllMenu();

        switch (currentState)
        {
            case RoomState.RoomScreen:
                roomScreenUI.SetActive(true);
                break;
            case RoomState.AbilityScreen:
                abilityScreenUI.SetActive(true);
                break;
        }
    }

    private void HideAllMenu()
    {
        roomScreenUI.SetActive(false);
        abilityScreenUI.SetActive(false);
    }

    public void RoomToMainMenu()
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
                    "ParrelSync profile: Clone_"
                    + cloneArgument + "_Profile"
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

            Debug.Log(
                "Player ID: "
                + AuthenticationService.Instance.PlayerId
            );

            if (statusText != null)
                statusText.text = "Services ready";
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
    // AUTOMATIC ROOM CREATION
    // =========================================================

    private async Task CreateRoom()
    {
        if (!servicesReady || creatingRoom || roomCreated)
            return;

        creatingRoom = true;

        if (statusText != null)
            statusText.text = "Creating room...";

        try
        {
            SessionOptions options = new SessionOptions
            {
                MaxPlayers = maxPlayers
            }.WithRelayNetwork();

            currentSession =
                await MultiplayerService.Instance
                    .CreateSessionAsync(options);

            if (currentSession == null)
            {
                throw new Exception(
                    "Session creation returned null."
                );
            }

            roomCreated = true;

            currentSession.Changed += OnSessionChanged;

            Debug.Log(
                "ROOM CREATED: " + currentSession.Code
            );

            if (roomIDText != null)
            {
                roomIDText.text =
                    "Room ID: " + currentSession.Code;
            }

            UpdatePlayerCount();

            if (startButton != null)
            {
                startButton.SetActive(
                    currentSession.IsHost
                );
            }

            if (statusText != null)
            {
                statusText.text =
                    "Room created. Waiting for players...";
            }

            LogNetworkStatus();
        }
        catch (Exception e)
        {
            Debug.LogError("ROOM CREATION FAILED");
            Debug.LogException(e);

            if (statusText != null)
                statusText.text = "Failed to create room";
        }
        finally
        {
            creatingRoom = false;
        }
    }

    // =========================================================
    // NETWORK STATUS
    // =========================================================

    private void LogNetworkStatus()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning(
                "NetworkManager.Singleton is null. "
                + "Check your NetworkManager setup."
            );

            return;
        }

        Debug.Log(
            "NGO listening: "
            + NetworkManager.Singleton.IsListening
        );
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
    // START GAME — HOST ONLY
    // =========================================================

    public void StartGame()
    {
        if (currentSession == null ||
            !currentSession.IsHost)
        {
            Debug.LogWarning(
                "Only the host can start the game."
            );

            return;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager is missing!");

            if (statusText != null)
                statusText.text = "NetworkManager is missing";

            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.LogError(
                "Netcode is not listening. "
                + "Check the session networking integration."
            );

            if (statusText != null)
                statusText.text = "Network is not ready";

            return;
        }

        if (statusText != null)
            statusText.text = "Starting game...";

        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName,
            LoadSceneMode.Single
        );
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (currentSession != null)
            currentSession.Changed -= OnSessionChanged;
    }
}