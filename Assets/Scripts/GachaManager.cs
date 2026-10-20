using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public enum GachaMenuState
{
    GachaScreen,
    ShopScreen
}

public class GachaManager : MonoBehaviour
{
    public GameObject gachaScreenUI;
    public GameObject shopScreenUI;
    public GachaMenuState currentState { get; private set; }

    private void Start()
    {
        ChangeState(GachaMenuState.GachaScreen);
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentState == GachaMenuState.ShopScreen)
            {
                ChangeState(GachaMenuState.GachaScreen);
                // EnableMenuButton();
            }
        }
    }

    public void ChangeState(GachaMenuState newState)
    {
        StartCoroutine(TransitionToState(newState));

        currentState = newState;
    }

    public void GachaToShop()
    {
        ChangeState(GachaMenuState.ShopScreen);
    }

    public void ShopToGacha()
    {
        ChangeState(GachaMenuState.GachaScreen);
    }

    public void GachaToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator TransitionToState(GachaMenuState newState)
    {
        if(newState != GachaMenuState.GachaScreen)
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
            case GachaMenuState.GachaScreen:
                gachaScreenUI.SetActive(true);
                break;
            case GachaMenuState.ShopScreen:
                shopScreenUI.SetActive(true);
                // EnableEndButton();
                break;
        }
    }

    private void HideAllMenu()
    {
        gachaScreenUI.SetActive(false);
        shopScreenUI.SetActive(false);
    }
}
