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

    private void Start()
    {
        Time.timeScale = 1;
        ChangeToGameScreen();
    }

    public void ShowResults(List<PlayerResult> results)
    {
        // TextMeshProUGUI[] rankTexts =
        // {
        //     rank1Text,
        //     rank2Text,
        //     rank3Text,
        //     rank4Text
        // };

        foreach (Transform child in leaderboardContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (PlayerResult result in results)
        {
            Leaderboard entry =
                Instantiate(
                    entryPrefab,
                    leaderboardContainer);

            entry.SetData(result);
        }

        // for (int i = 0; i < results.Count && i < rankTexts.Length; i++)
        // {
        //     rankTexts[i].text = $"{results[i].rank}. Player {results[i].clientId}, {results[i].points} Points";
        // }
    }


    private void Update()
    {
        roundTime -= Time.deltaTime;

        time.text = roundTime.ToString("0");

        if(roundTime <= 0)
        {
            List<PlayerResult> results = pointCalculator.CalculateRankings();
            ShowResults(results);
            ChangeToEndScreen();
        }

        /*if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == MenuState.GameScreen)
            {
                ChangeState(MenuState.GameMenuScreen);
                EnableMenuButton();
            }
            else if (currentState == MenuState.GameMenuScreen)
            {
                ChangeState(MenuState.GameScreen);
                DisableAllButton();
            }
        }*/
    }
    
    public void ChangeState(MenuState newState)
    {
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
    }

    public void ChangeToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator TransitionToState(MenuState newState)
    {
        if(newState != MenuState.GameScreen)
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
            case MenuState.GameScreen:
                Time.timeScale = 1;
                gameScreenUI.SetActive(true);
                break;
            case MenuState.EndScreen:
                Time.timeScale = 0;
                endScreenUI.SetActive(true);
                break;
            case MenuState.GameMenuScreen:
                Time.timeScale = 1;
                gameMenuScreenUI.SetActive(true);
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
