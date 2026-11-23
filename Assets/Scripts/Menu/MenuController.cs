using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    // Táto metóda sa spustí automaticky hneď po načítaní scény
    private void Start()
    {
        MusicManager.Instance.PlayMusic("MenuBackground");
        Debug.Log("Hra sa zapla: Spúšťam 'Menu' hudbu cez Start metódu.");

        HookTutorialButton();
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

    // Tlačidlo TUTORIAL - načíta TutorialScene cez loading obrazovku (rovnako ako ostatné scény)
    public void OpenTutorial()
    {
        if (LoadingScene.IsBusy) return;
        LoadingScene.Load(SceneIds.Tutorial);
    }

    // Tlačidlo Tutorial v menu (TutorialButtton) pôvodne iba zapínalo panel TutorialMenu.
    // Vypneme jeho SetActive volania (zvuk kliknutia ostáva) a namiesto toho otvoríme TutorialScene.
    private void HookTutorialButton()
    {
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            if (!IsTutorialButton(button)) continue;

            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == "SetActive")
                {
                    button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
                }
            }

            button.onClick.RemoveListener(OpenTutorial);
            button.onClick.AddListener(OpenTutorial);
        }
    }

    // Tlačidlo samotné alebo jeho rodič sa volá TutorialButton / TutorialButtton
    private static bool IsTutorialButton(Button button)
    {
        Transform t = button.transform;
        for (int depth = 0; t != null && depth < 3; depth++, t = t.parent)
        {
            if (t.name.StartsWith("TutorialButt")) return true;
        }
        return false;
    }

    public void Quit()
    {
        Application.Quit();
    }
}
