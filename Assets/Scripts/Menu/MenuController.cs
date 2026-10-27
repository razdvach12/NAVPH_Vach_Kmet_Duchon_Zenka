using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
 
public class MenuController : MonoBehaviour
{
    // Táto metóda sa spustí automaticky hneď po načítaní scény
    private void Start()
    {
        MusicManager.Instance.PlayMusic("Menu");
        Debug.Log("Hra sa zapla: Spúšťam 'Menu' hudbu cez Start metódu.");
    }

    public void Play()
    {
        SceneManager.LoadScene("MenuScene");
        MusicManager.Instance.PlayMusic("Menu");
        Debug.Log("Play button clicked. Loading MenuScene and playing 'Menu' music.");
    }

    public void ChangeScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void Quit()
    {
        Application.Quit();
    }
}