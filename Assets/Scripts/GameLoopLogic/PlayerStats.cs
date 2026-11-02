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
    public event Action OnRelicsChanged; // <-- NOVÝ SIGNÁL PRE RELIKVIE

    [Header("Aktuálny stav hráča")]
    public int money = 0;
    public int encountersDefeated = 0;
    
    [Header("Inventár")]
    // ZMENA: Namiesto List<string> teraz ukladáme List<ItemData>, aby sme mali prístup k ikonke!
    public List<ItemData> relics = new List<ItemData>(); 
    public List<GridPreset> unlockedWeapons = new List<GridPreset>();
    public List<ItemData> consumables = new List<ItemData>(); 
    
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

    public void InitializeCharacter(CharacterData character)
    {
        if (character == null) return;

        money = character.startingGold;
        playerHealthPreset = character.healthGridPreset;
        
        unlockedWeapons.Clear();
        if (character.startingWeapons != null)
        {
            unlockedWeapons.AddRange(character.startingWeapons);
        }

        Debug.Log($"Postava {character.characterName} inicializovaná! Goldy: {money}");
        
        // Zaktualizuj všetko UI pri načítaní postavy
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
            Debug.Log($"Nákup úspešný! Zostáva ti {money} goldov.");
            OnMoneyChanged?.Invoke(); 
            return true;
        }
        Debug.Log("Nemáš dostatok goldov!");
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
                Debug.Log($"Zbraň {item.itemName} pridaná do arzenálu!");
                OnWeaponsChanged?.Invoke(); 
            }
            else
            {
                Debug.LogWarning($"Zbrani {item.itemName} chýba GridPreset!");
            }
        }
        else if (item.itemType == ItemType.Consumable)
        {
            consumables.Add(item);
            Debug.Log($"Potion {item.itemName} pridaný!");
            OnConsumablesChanged?.Invoke(); 
        }
        else if (item.itemType == ItemType.Relic)
        {
            // TOTO JE UPRAVENÉ PRE RELIKVIE
            relics.Add(item);
            Debug.Log($"Relikvia {item.itemName} pridaná!");
            OnRelicsChanged?.Invoke(); // Pošle signál, aby sa UI relikvií prekreslilo
        }
        else if (item.itemType == ItemType.Upgrade)
        {
            Debug.Log($"Aplikovaný upgrade: {item.itemName}");
        }
    }
}