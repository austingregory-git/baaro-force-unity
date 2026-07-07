using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        //Debug.Log("Play button pressed");
        SceneManager.LoadScene("CharacterSelectionScene");
    }

    public void Settings()
    {
        Debug.Log("Settings button pressed");
    }

    public void Statistics()
    {
        Debug.Log("Statistics button pressed");
    }

    public void Quit()
    {
        Debug.Log("Quit button pressed");
        Application.Quit();
    }
}
