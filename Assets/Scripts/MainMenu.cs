using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public enum MainMenuState
{
    MainMenuScreen,
    ShopScreen,
    AbilityScreen
}

public class MainMenu : MonoBehaviour
{
    public GameObject mainMenuScreenUI;
    public GameObject shopScreenUI;
    public GameObject abilityScreenUI;
    public MainMenuState currentState { get; private set; }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == MainMenuState.ShopScreen || currentState == MainMenuState.AbilityScreen)
            {
                ChangeState(MainMenuState.MainMenuScreen);
            }
        }
    }

    public void ChangeState(MainMenuState newState)
    {
        StartCoroutine(TransitionToState(newState));

        currentState = newState;
    }

    public void MainMenuToShop()
    {
        ChangeState(MainMenuState.ShopScreen);
    }

    public void MainMenuToAbility()
    {
        ChangeState(MainMenuState.AbilityScreen);
    }

    public void StuffToMainMenu()
    {
        ChangeState(MainMenuState.MainMenuScreen);
    }

    private IEnumerator TransitionToState(MainMenuState newState)
    {
        if(newState != MainMenuState.MainMenuScreen)
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
            case MainMenuState.MainMenuScreen:
                mainMenuScreenUI.SetActive(true);
                break;
            case MainMenuState.ShopScreen:
                shopScreenUI.SetActive(true);
                break;
            case MainMenuState.AbilityScreen:
                abilityScreenUI.SetActive(true);
                break;
        }
    }

    private void HideAllMenu()
    {
        mainMenuScreenUI.SetActive(false);
        shopScreenUI.SetActive(false);
        abilityScreenUI.SetActive(false);
    }

    public void GoToLobby()
    {
        LoadScene("Lobby");
    }

    public void GoToGacha()
    {
        LoadScene("Gacha");
    }

    public void GoToProfile()
    {
        LoadScene("Profile");
    }

    public void GoToEmotes()
    {
        LoadScene("Emotes");
    }

    public void GoToBP()
    {
        LoadScene("BP");
    }

    public void GoToBaju()
    {
        LoadScene("Baju");
    }

    public void GoToGiveMeMoney()
    {
        LoadScene("GIVEMEMONEYPLSBROINEEDTHIS");
    }

    // Dipanggil dari tombol Ability / Skil di Main Menu
    public void GoToAbility()
    {
        AbilityDataHolder.PreviousScene = "MainMenu";
        Debug.Log("[Navigation] Membuka Ability dari MainMenu");
        LoadScene("Ability");
    }

    // DIPANGGIL KHUSUS DARI TOMBOL SKIL DI ROOMSCENE
    public void OpenAbilityFromRoomScene()
    {
        AbilityDataHolder.PreviousScene = "RoomScene";
        Debug.Log("[Navigation] Membuka Ability dari RoomScene");
        LoadScene("Ability");
    }

    public void GoToSettings()
    {
        LoadScene("Settings");
    }

    // =========================================================
    // LOAD SCENE
    // =========================================================

    private void LoadScene(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError(
                "Cannot load scene: " + sceneName +
                ". Make sure it is added to Build Profiles."
            );
        }
    }
}