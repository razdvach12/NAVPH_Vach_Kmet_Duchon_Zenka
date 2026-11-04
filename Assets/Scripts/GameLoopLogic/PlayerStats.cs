using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("Aktuálny stav hráča")]
    public int money = 0;
    public int maxStamina = 5; // Dá sa zvyšovať počas hry (napr. odmena za bossa)
    public int encountersDefeated = 0;
    public List<string> relics = new List<string>();
    public List<GridPreset> unlockedWeapons = new List<GridPreset>();
    public GridPreset playerHealthPreset;

    [Header("Inventár consumables")]
    public int maxConsumables = 4; // Toľko, koľko je slotov consumables v encountri
    public List<ConsumableData> consumables = new List<ConsumableData>();

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
        maxStamina = character.startingStamina;
        playerHealthPreset = character.healthGridPreset;
        
        // Vyčistíme a naplníme počiatočné zbrane
        unlockedWeapons.Clear();
        if (character.startingWeapons != null)
        {
            unlockedWeapons.AddRange(character.startingWeapons);
        }

        // Nový run začína s prázdnym inventárom
        consumables.Clear();

        Debug.Log($"Postava {character.characterName} úspešne uložená! Goldy: {money}, Zbrane: {unlockedWeapons.Count}");
    }

    // Vráti false, ak je inventár plný (napr. obchod potom nákup nepovolí)
    public bool TryAddConsumable(ConsumableData consumable)
    {
        if (consumable == null || consumables.Count >= maxConsumables) return false;

        consumables.Add(consumable);
        return true;
    }

    // Zavolá sa po použití consumable v súboji
    public void RemoveConsumable(ConsumableData consumable)
    {
        consumables.Remove(consumable);
    }
}