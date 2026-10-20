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

    private NetworkManager networkManager;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1f;

        networkManager = NetworkManager.Singleton;

        ChangeToGameScreen();
    }

    private void Update()
    {
        // Do not update the timer while the game menu is open or
        // after the round has ended.
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
        if (endingSession)
            return;

        Time.timeScale = 1f;
        SetMouseLocked(false);

        networkManager = NetworkManager.Singleton;

        // Offline game or already disconnected.
        if (networkManager == null || !networkManager.IsListening)
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        if (networkManager.IsServer)
        {
            // The host can end the session directly.
            EndSessionForEveryone();
        }
        else
        {
            // Ask the host to end the session for everyone.
            RequestEndSessionRpc();
        }
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void RequestEndSessionRpc()
    {
        if (NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        EndSessionForEveryone();
    }

    private void EndSessionForEveryone()
    {
        if (endingSession)
            return;

        endingSession = true;

        networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        if (!networkManager.IsServer)
            return;

        if (networkManager.SceneManager == null)
        {
            Debug.LogError(
                "Network scene management is unavailable. " +
                "Enable Scene Management in the NetworkManager."
            );

            endingSession = false;
            return;
        }

        // All connected clients are instructed to load MainMenu.
        networkManager.SceneManager.LoadScene(
            "MainMenu",
            LoadSceneMode.Single
        );

        // Network shutdown must happen after the scene transition.
        networkManager.SceneManager.OnLoadEventCompleted +=
            OnMainMenuLoadCompleted;
    }

    private void OnMainMenuLoadCompleted(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        if (sceneName != "MainMenu")
            return;

        if (networkManager != null &&
            networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnLoadEventCompleted -=
                OnMainMenuLoadCompleted;
        }

        // The session has ended; disconnect everyone.
        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        Debug.Log("Multiplayer session ended. Everyone returned to MainMenu.");
    }

    private void OnDestroy()
    {
        if (networkManager != null &&
            networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnLoadEventCompleted -=
                OnMainMenuLoadCompleted;
        }

        if (instance == this)
            instance = null;
    }
}