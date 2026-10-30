using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("Aktuálny stav hráča")]
    public int money = 0;
    public int encountersDefeated = 0;
    public List<string> relics = new List<string>();
    public List<GridPreset> unlockedWeapons = new List<GridPreset>();
    public GridPreset playerHealthPreset;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- TÚTO METÓDU ZAVOLÁŠ PRI KLIKNUTÍ NA START ---
    public void InitializeCharacter(CharacterData character)
    {
        if (character == null) return;

        money = character.startingGold;
        playerHealthPreset = character.healthGridPreset;
        
        // Vyčistíme a naplníme počiatočné zbrane
        unlockedWeapons.Clear();
        if (character.startingWeapons != null)
        {
            unlockedWeapons.AddRange(character.startingWeapons);
        }

        Debug.Log($"Postava {character.characterName} úspešne uložená! Goldy: {money}, Zbrane: {unlockedWeapons.Count}");
    }
}