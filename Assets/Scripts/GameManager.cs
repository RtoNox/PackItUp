using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public enum MenuState
{
    GameScreen,
    EndScreen,
    GameMenuScreen
}

public class GameManager : MonoBehaviour
{
    public GameObject gameScreenUI;
    public GameObject endScreenUI;
    public GameObject gameMenuScreenUI;

    public Button endToMainMenuButt;
    public Button menuToMainMenuButt;
    public Button returnToGameButt;

    [SerializeField] private Transform leaderboardContainer;
    [SerializeField] private Leaderboard entryPrefab;

    [SerializeField] private TMP_Text time;
    [SerializeField] private PointCalculator pointCalculator;

    public static GameManager instance;
    public MenuState currentState { get; private set; }

    public float roundTime = 180f;

    [Header("Mouse Settings")]
    [SerializeField] private bool lockMouseDuringGameplay = true;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Time.timeScale = 1;
        ChangeToGameScreen();
    }

    private void Update()
    {
        if (currentState == MenuState.GameScreen)
        {
            roundTime -= Time.deltaTime;

            if (time != null)
                time.text = Mathf.Max(0, roundTime).ToString("0");

            if (roundTime <= 0)
            {
                List<PlayerResult> results =
                    pointCalculator.CalculateRankings();

                ShowResults(results);
                ChangeToEndScreen();
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == MenuState.GameScreen)
            {
                ChangeState(MenuState.GameMenuScreen);
                EnableMenuButton();
            }
            else if (currentState == MenuState.GameMenuScreen)
            {
                ChangeToGameScreen();
                DisableAllButton();
            }
        }
    }

    public void ShowResults(List<PlayerResult> results)
    {
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

    public void ChangeState(MenuState newState)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionToState(newState));
        currentState = newState;
    }

    public void ChangeToGameScreen()
    {
        ChangeState(MenuState.GameScreen);
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

    public void ChangeToMainMenu()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator TransitionToState(MenuState newState)
    {
        if (newState != MenuState.GameScreen)
        {
            yield return null;
        }

        currentState = newState;
        HandleStateChange();
    }

    private void HandleStateChange()
    {
        HideAllMenu();

        switch (currentState)
        {
            case MenuState.GameScreen:
                Time.timeScale = 1;
                gameScreenUI.SetActive(true);

                if (lockMouseDuringGameplay)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                break;

            case MenuState.EndScreen:
                Time.timeScale = 0;
                endScreenUI.SetActive(true);
                EnableEndButton();

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;

            case MenuState.GameMenuScreen:
                Time.timeScale = 1;
                gameMenuScreenUI.SetActive(true);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
        }
    }

    private void HideAllMenu()
    {
        gameScreenUI.SetActive(false);
        endScreenUI.SetActive(false);
        gameMenuScreenUI.SetActive(false);
    }

    private void DisableAllButton()
    {
        returnToGameButt.interactable = false;
        endToMainMenuButt.interactable = false;
        menuToMainMenuButt.interactable = false;
    }

    private void EnableEndButton()
    {
        returnToGameButt.interactable = false;
        endToMainMenuButt.interactable = true;
        menuToMainMenuButt.interactable = false;
    }

    private void EnableMenuButton()
    {
        returnToGameButt.interactable = true;
        endToMainMenuButt.interactable = false;
        menuToMainMenuButt.interactable = true;
    }
}