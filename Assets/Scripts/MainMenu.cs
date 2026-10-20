using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

#if UNITY_EDITOR
using ParrelSync;
#endif

public enum MainMenuState
{
    MainMenuScreen,
    ShopScreen,
    AbilityScreen,
    ProfileScreen
}

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuScreenUI;
    public GameObject shopScreenUI;
    public GameObject abilityScreenUI;
    public GameObject profileScreenUI;

    [Header("Profile")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text savedNameText;

    public MainMenuState currentState { get; private set; }

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        LoadPlayerName();
        ChangeState(MainMenuState.MainMenuScreen);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState != MainMenuState.MainMenuScreen)
            {
                ChangeState(MainMenuState.MainMenuScreen);
            }
        }
    }

    // =========================================================
    // MENU STATE
    // =========================================================

    public void ChangeState(MainMenuState newState)
    {
        currentState = newState;
        HandleStateChange();

        Debug.Log("Changed menu state to: " + currentState);
    }

    public void MainMenuToShop()
    {
        ChangeState(MainMenuState.ShopScreen);
    }

    public void MainMenuToAbility()
    {
        ChangeState(MainMenuState.AbilityScreen);
    }

    public void MainMenuToProfile()
    {
        LoadPlayerName();
        ChangeState(MainMenuState.ProfileScreen);
    }

    public void StuffToMainMenu()
    {
        ChangeState(MainMenuState.MainMenuScreen);
    }

    private void HandleStateChange()
    {
        HideAllMenu();

        switch (currentState)
        {
            case MainMenuState.MainMenuScreen:
                if (mainMenuScreenUI != null)
                    mainMenuScreenUI.SetActive(true);
                break;

            case MainMenuState.ShopScreen:
                if (shopScreenUI != null)
                    shopScreenUI.SetActive(true);
                break;

            case MainMenuState.AbilityScreen:
                if (abilityScreenUI != null)
                    abilityScreenUI.SetActive(true);
                break;

            case MainMenuState.ProfileScreen:
                if (profileScreenUI != null)
                {
                    profileScreenUI.SetActive(true);

                    Debug.Log(
                        "Profile activeSelf: " +
                        profileScreenUI.activeSelf
                    );

                    Debug.Log(
                        "Profile activeInHierarchy: " +
                        profileScreenUI.activeInHierarchy
                    );
                }
                else
                {
                    Debug.LogError(
                        "ProfileScreenUI is not assigned in the Inspector!"
                    );
                }
                break;
        }
    }

    private void HideAllMenu()
    {
        if (mainMenuScreenUI != null)
            mainMenuScreenUI.SetActive(false);

        if (shopScreenUI != null)
            shopScreenUI.SetActive(false);

        if (abilityScreenUI != null)
            abilityScreenUI.SetActive(false);

        if (profileScreenUI != null)
            profileScreenUI.SetActive(false);
    }

    // =========================================================
    // PROFILE
    // =========================================================

    public void GoToProfile()
    {
        Debug.Log("Profile button clicked!");
        MainMenuToProfile();
    }

    public void SavePlayerName()
    {
        string playerName;

        if (playerNameInput != null &&
            !string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            playerName = playerNameInput.text.Trim();
        }
        else
        {
            playerName = "Player_" + Random.Range(100, 999);
        }

        string playerNameKey = GetPlayerNameKey();

        PlayerPrefs.SetString(playerNameKey, playerName);
        PlayerPrefs.Save();

        if (playerNameInput != null)
            playerNameInput.text = playerName;

        if (savedNameText != null)
            savedNameText.text = "Player Name: " + playerName;

        Debug.Log(
            "Saved Player Name: " + playerName +
            " | Key: " + playerNameKey
        );
    }

    private void LoadPlayerName()
    {
        string playerNameKey = GetPlayerNameKey();

        if (!PlayerPrefs.HasKey(playerNameKey))
        {
            PlayerPrefs.SetString(
                playerNameKey,
                "Player_" + Random.Range(100, 999)
            );

            PlayerPrefs.Save();
        }

        string playerName = PlayerPrefs.GetString(playerNameKey);

        if (playerNameInput != null)
            playerNameInput.text = playerName;

        if (savedNameText != null)
            savedNameText.text = "Player Name: " + playerName;
    }

    private string GetPlayerNameKey()
    {
#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            string cloneArgument = ClonesManager.GetArgument();
            return "PlayerName_Clone_" + cloneArgument;
        }

        return "PlayerName_Original";
#else
        return "PlayerName";
#endif
    }

    // =========================================================
    // SCENE NAVIGATION
    // =========================================================

    public void GoToLobby()
    {
        LoadScene("Lobby");
    }

    public void GoToGacha()
    {
        LoadScene("Gacha");
    }

    // Profile opens the panel instead of loading another scene.
    // GoToProfile() handles this.
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

    // Called by the Ability button in Main Menu.
    public void GoToAbility()
    {
        AbilityDataHolder.PreviousScene = "MainMenu";
        Debug.Log("[Navigation] Membuka Ability dari MainMenu");
        LoadScene("Ability");
    }

    // Called specifically by the Ability button in RoomScene.
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