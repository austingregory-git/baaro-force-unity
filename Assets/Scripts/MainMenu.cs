using UnityEngine;
using UnityEngine.SceneManagement;
using BaaroForce.Characters;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {
        Realm[] allRealms = (Realm[])System.Enum.GetValues(typeof(Realm));
        Realm randomRealm = allRealms[Random.Range(0, allRealms.Length)];
        PartyManager.Instance.SetRealm(randomRealm);
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
