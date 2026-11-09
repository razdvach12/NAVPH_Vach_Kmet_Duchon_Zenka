using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    // --- UDALOSTI (SIGNÁLY) PRE UI ---
    public event Action OnMoneyChanged;
    public event Action OnWeaponsChanged;
    public event Action OnConsumablesChanged; 
    public event Action OnRelicsChanged; 

    [Header("Aktuálny stav hráča")]
    public int money = 0;
    public int maxStamina = 5; // Dá sa zvyšovať počas hry (napr. odmena za bossa)
    public int encountersDefeated = 0;
    
    // --- OBRÁZOK POSTAVY ---
    public Sprite playerSprite; 

    [Header("Inventár")]
    public List<ItemData> relics = new List<ItemData>(); 
    public List<GridPreset> unlockedWeapons = new List<GridPreset>();

    public GridPreset playerHealthPreset;

    [Tooltip("Aktuálne HP políčok HP gridu (index = y * width + x), prenáša sa medzi encountermi. Prázdne = plné HP")]
    public int[] playerCurrentHp;

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

    public void InitializeCharacter(CharacterData character)
    {
        if (character == null) return;

        money = character.startingGold;
        maxStamina = character.startingStamina;
        playerHealthPreset = character.healthGridPreset;
        playerCurrentHp = null; // Nový run začína s plnými životmi
        
        // --- ULOŽENIE OBRÁZKA POSTAVY ---
        // Ak sa v tvojom CharacterData volá premenná inak, zmeň slovo "characterSprite"
        playerSprite = character.characterSprite; 
        
        unlockedWeapons.Clear();
        if (character.startingWeapons != null)
        {
            unlockedWeapons.AddRange(character.startingWeapons);
        }

        // Nový run začína s prázdnym inventárom
        consumables.Clear();

        Debug.Log($"Postava {character.characterName} úspešne uložená! Goldy: {money}, Zbrane: {unlockedWeapons.Count}");

        OnMoneyChanged?.Invoke();
        OnWeaponsChanged?.Invoke();
        OnConsumablesChanged?.Invoke();
        OnRelicsChanged?.Invoke();
    }

    public bool TrySpendMoney(int amount)
    {
        if (money >= amount)
        {
            money -= amount;
            OnMoneyChanged?.Invoke(); 
            return true;
        }
        return false;
    }

    public void AddMoney(int amount)
    {
        money += amount;
        OnMoneyChanged?.Invoke(); 
    }

    public void AddPurchasedItem(ItemData item)
    {
        if (item == null) return;

        if (item.itemType == ItemType.Weapon)
        {
            if (item.weaponGridPreset != null)
            {
                unlockedWeapons.Add(item.weaponGridPreset);
                OnWeaponsChanged?.Invoke(); 
            }
        }
        else if (item.itemType == ItemType.Consumable)
        {
            // Consumable musí byť ConsumableData asset (TryAddConsumable už zavolá OnConsumablesChanged)
            if (item is ConsumableData consumable) TryAddConsumable(consumable);
        }
        else if (item.itemType == ItemType.Relic)
        {
            relics.Add(item);
            OnRelicsChanged?.Invoke(); 
        }
    }

    public void RemoveWeapon(int index)
    {
        if (index >= 0 && index < unlockedWeapons.Count)
        {
            unlockedWeapons.RemoveAt(index); 
            OnWeaponsChanged?.Invoke();
        }
    }

    // Vráti false, ak je inventár plný (napr. obchod potom nákup nepovolí)
    public bool TryAddConsumable(ConsumableData consumable)
    {
        if (consumable == null || consumables.Count >= maxConsumables) return false;

        consumables.Add(consumable);
        OnConsumablesChanged?.Invoke();
        return true;
    }

    // Zavolá sa po použití consumable v súboji
    public void RemoveConsumable(ConsumableData consumable)
    {
        if (consumables.Remove(consumable)) OnConsumablesChanged?.Invoke();
    }
}