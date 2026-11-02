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
    public int encountersDefeated = 0;
    
    // --- OBRÁZOK POSTAVY ---
    public Sprite playerSprite; 

    [Header("Inventár")]
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
        
        // --- ULOŽENIE OBRÁZKA POSTAVY ---
        // Ak sa v tvojom CharacterData volá premenná inak, zmeň slovo "characterSprite"
        playerSprite = character.characterSprite; 
        
        unlockedWeapons.Clear();
        if (character.startingWeapons != null)
        {
            unlockedWeapons.AddRange(character.startingWeapons);
        }

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
            consumables.Add(item);
            OnConsumablesChanged?.Invoke(); 
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
}