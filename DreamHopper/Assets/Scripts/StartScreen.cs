using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    public GameObject startScreen;
    public GameObject optionsScreen;

    public void StartButton()
    {
        SceneManager.LoadScene("Level1");
    }

    public void OptionsButton()
    {
        startScreen.SetActive(false);
        optionsScreen.SetActive(true);
    }
    public void BackButton()
    {
        optionsScreen.SetActive(false);
        startScreen.SetActive(true);
    }

    public void ExitButton()
    {
        Application.Quit();
    }
}
