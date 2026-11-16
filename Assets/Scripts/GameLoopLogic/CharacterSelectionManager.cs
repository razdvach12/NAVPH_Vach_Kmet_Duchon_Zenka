using UnityEngine;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Aktuálne zvolená postava v menu")]
    public CharacterData selectedCharacter;

    [Header("Mapa")]
    [Tooltip("Z tohto presetu sa po stlačení Start vygeneruje mapa pre celú hru")]
    public MapPreset mapPreset;

    private void Start()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic("CharacterSelBackground");
        }
    }

    // Univerzálna metóda pre hociktorú postavu
    public void SelectCharacter(CharacterData character)
    {
        selectedCharacter = character;
        Debug.Log($"Zvolený hrdina: {character.characterName}");
    }


    public void OnClickStartButton()
    {
        if (selectedCharacter == null)
        {
            Debug.LogWarning("Nie je vybraná žiadna postava!");
            return;
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.InitializeCharacter(selectedCharacter);
        }

        if (LoadingScene.IsBusy) return; // Dvojklik počas loadingu by vygeneroval mapu znova

        // Nová hra = nová mapa, ostane rovnaká až do konca hry
        MapRunState.StartNewRun(mapPreset);

        LoadingScene.Load(SceneIds.Map);
    }

    public void OnClickBackToMenuButton()
    {
        MapRunState.Clear();
        LoadingScene.Load(SceneIds.Menu);
    }
}
