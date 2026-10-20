
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;

public enum MenuState
{
    GameScreen,
    EndScreen,
    GameMenuScreen
}

public class GameManager : MonoBehaviour
{
    [Header("Menu UI")]
    public GameObject gameScreenUI;
    public GameObject endScreenUI;
    public GameObject gameMenuScreenUI;

    [Header("Buttons")]
    public Button endToMainMenuButt;
    public Button menuToMainMenuButt;
    public Button returnToGameButt;

    [Header("Leaderboard")]
    [SerializeField] private Transform leaderboardContainer;
    [SerializeField] private Leaderboard entryPrefab;

    [Header("Game")]
    [SerializeField] private TMP_Text time;
    [SerializeField] private PointCalculator pointCalculator;
    public float roundTime = 180f;

    [Header("Mouse Settings")]
    [SerializeField] private bool lockMouseDuringGameplay = true;

    public static GameManager instance;
    public MenuState currentState { get; private set; }

    private bool endingSession;
    private bool roundEnded;
    private bool menuLoading;

    private NetworkManager networkManager;

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        networkManager = NetworkManager.Singleton;

        // Listen for network shutdown on every player's instance.
        // This allows clients to return to MainMenu when the host
        // shuts down the multiplayer session.
        if (networkManager != null)
        {
            networkManager.OnClientStopped += HandleClientStopped;
            networkManager.OnServerStopped += HandleServerStopped;
        }

        ChangeToGameScreen();
    }

    private void Update()
    {
        // Update the timer only while playing.
        if (currentState == MenuState.GameScreen && !roundEnded)
        {
            roundTime -= Time.deltaTime;
            roundTime = Mathf.Max(0f, roundTime);

            if (time != null)
                time.text = roundTime.ToString("0");

            if (roundTime <= 0f)
            {
                roundEnded = true;

                if (pointCalculator != null)
                {
                    List<PlayerResult> results =
                        pointCalculator.CalculateRankings();

                    ShowResults(results);
                }
                else
                {
                    Debug.LogError("PointCalculator is not assigned.");
                }

                ChangeToEndScreen();
                return;
            }
        }

        // Escape opens or closes the in-game menu.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == MenuState.GameScreen)
            {
                ChangeToGameMenuScreen();
            }
            else if (currentState == MenuState.GameMenuScreen)
            {
                ChangeToGameScreen();
            }
        }
    }

    // =========================================================
    // LEADERBOARD
    // =========================================================

    public void ShowResults(List<PlayerResult> results)
    {
        if (leaderboardContainer == null || entryPrefab == null)
        {
            Debug.LogError("Leaderboard references are missing.");
            return;
        }

        foreach (Transform child in leaderboardContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (PlayerResult result in results)
        {
            Leaderboard entry =
                Instantiate(entryPrefab, leaderboardContainer);

            entry.SetData(result);
        }
    }

    // =========================================================
    // MENU STATES
    // =========================================================

    public void ChangeState(MenuState newState)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionToState(newState));
    }

    public void ChangeToGameScreen()
    {
        ChangeState(MenuState.GameScreen);
        DisableAllButton();
    }

    public void ChangeToEndScreen()
    {
        ChangeState(MenuState.EndScreen);
    }

    public void ChangeToGameMenuScreen()
    {
        ChangeState(MenuState.GameMenuScreen);
        EnableMenuButton();
    }

    private IEnumerator TransitionToState(MenuState newState)
    {
        if (newState != MenuState.GameScreen)
            yield return null;

        currentState = newState;
        HandleStateChange();
    }

    private void HandleStateChange()
    {
        HideAllMenu();

        switch (currentState)
        {
            case MenuState.GameScreen:
                Time.timeScale = 1f;

                if (gameScreenUI != null)
                    gameScreenUI.SetActive(true);

                SetMouseLocked(true);
                break;

            case MenuState.EndScreen:
                Time.timeScale = 0f;

                if (endScreenUI != null)
                    endScreenUI.SetActive(true);

                EnableEndButton();
                SetMouseLocked(false);
                break;

            case MenuState.GameMenuScreen:
                Time.timeScale = 1f;

                if (gameMenuScreenUI != null)
                    gameMenuScreenUI.SetActive(true);

                EnableMenuButton();
                SetMouseLocked(false);
                break;
        }
    }

    private void HideAllMenu()
    {
        if (gameScreenUI != null)
            gameScreenUI.SetActive(false);

        if (endScreenUI != null)
            endScreenUI.SetActive(false);

        if (gameMenuScreenUI != null)
            gameMenuScreenUI.SetActive(false);
    }

    // =========================================================
    // MOUSE
    // =========================================================

    private void SetMouseLocked(bool locked)
    {
        if (locked && lockMouseDuringGameplay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // =========================================================
    // BUTTONS
    // =========================================================

    private void DisableAllButton()
    {
        if (returnToGameButt != null)
            returnToGameButt.interactable = false;

        if (endToMainMenuButt != null)
            endToMainMenuButt.interactable = false;

        if (menuToMainMenuButt != null)
            menuToMainMenuButt.interactable = false;
    }

    private void EnableEndButton()
    {
        if (returnToGameButt != null)
            returnToGameButt.interactable = false;

        if (endToMainMenuButt != null)
            endToMainMenuButt.interactable = true;

        if (menuToMainMenuButt != null)
            menuToMainMenuButt.interactable = false;
    }

    private void EnableMenuButton()
    {
        if (returnToGameButt != null)
            returnToGameButt.interactable = true;

        if (endToMainMenuButt != null)
            endToMainMenuButt.interactable = false;

        if (menuToMainMenuButt != null)
            menuToMainMenuButt.interactable = true;
    }

    // =========================================================
    // LEAVE GAME / END MULTIPLAYER SESSION
    // =========================================================

    public void ChangeToMainMenu()
    {
        if (endingSession || menuLoading)
            return;

        Time.timeScale = 1f;
        SetMouseLocked(false);

        networkManager = NetworkManager.Singleton;

        // Offline game or already disconnected.
        if (networkManager == null || !networkManager.IsListening)
        {
            LoadMainMenuLocally();
            return;
        }

        // Prevent repeated clicks while the shutdown is happening.
        endingSession = true;

        if (networkManager.IsServer)
        {
            // Host pressed Leave.
            // Shut down the session for all connected players.
            EndSessionForEveryone();
        }
        else
        {
            // Client pressed Leave.
            // Ask the host to end the session for everyone.
            RequestEndSessionRpc();
        }
    }

    // Sent by a client to the host/server.
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestEndSessionRpc()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm == null || !nm.IsServer || !nm.IsListening)
            return;

        EndSessionForEveryone();
    }

    // Runs on the host/server.
    private void EndSessionForEveryone()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
        {
            LoadMainMenuLocally();
            return;
        }

        if (!networkManager.IsServer)
            return;

        endingSession = true;

        Time.timeScale = 1f;
        SetMouseLocked(false);

        // Shut down the host. NGO disconnects the connected clients.
        // Each player's local stop callback will load MainMenu.
        networkManager.Shutdown();

        // Fallback in case the stop callback does not load the scene.
        StartCoroutine(LoadMenuAfterShutdown());
    }

    // Called locally when this player's client connection stops.
    // This also fires for the host's local client.
    private void HandleClientStopped(bool wasHost)
    {
        if (menuLoading)
            return;

        // A stopped connection should return to the menu.
        LoadMainMenuLocally();
    }

    // Called locally when this player's server stops.
    private void HandleServerStopped(bool wasHost)
    {
        if (menuLoading)
            return;

        LoadMainMenuLocally();
    }

    private IEnumerator LoadMenuAfterShutdown()
    {
        // Give NGO a short time to finish shutdown processing.
        yield return null;
        yield return null;

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            LoadMainMenuLocally();
        }
    }

    private void LoadMainMenuLocally()
    {
        if (menuLoading)
            return;

        if (SceneManager.GetActiveScene().name == "MainMenu")
            return;

        menuLoading = true;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Use Unity's local scene loading after the network has stopped.
        SceneManager.LoadScene("MainMenu");
    }

    // =========================================================
    // CLEANUP
    // =========================================================

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientStopped -= HandleClientStopped;
            networkManager.OnServerStopped -= HandleServerStopped;
        }

        if (instance == this)
            instance = null;
    }
}
