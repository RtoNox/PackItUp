using UnityEngine;
using UnityEngine.SceneManagement;


public class SettingManager : MonoBehaviour
{
    public void SettingToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void CloseGame()
    {
        Application.Quit();
    }
        
}
