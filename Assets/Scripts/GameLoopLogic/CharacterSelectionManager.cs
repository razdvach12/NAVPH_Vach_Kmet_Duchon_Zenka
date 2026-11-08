using UnityEngine;

public class CharacterSelectionManager : MonoBehaviour
{
    [Header("Aktuálne zvolená postava v menu")]
    public CharacterData selectedCharacter;

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

        if (LoadingScene.Instance != null)
        {
            LoadingScene.Instance.SwitchToScene(5); 
        }
    }
}