using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static void Play()
    {
        SceneManager.LoadScene("CharacterSelectionScene");
    }

    public static void Settings()
    {
        Debug.Log("Settings button pressed");
    }

    public static void Statistics()
    {
        Debug.Log("Statistics button pressed");
    }

    public static void Quit()
    {
        Debug.Log("Quit button pressed");
        Application.Quit();
    }
}
