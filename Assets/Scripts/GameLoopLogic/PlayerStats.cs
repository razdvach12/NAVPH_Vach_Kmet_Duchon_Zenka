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

    [Tooltip("Postava (classa), za ktorú sa hrá tento run. Nastaví sa v InitializeCharacter.")]
    public CharacterData selectedCharacter;

    [Header("Inventár")]
    public List<ItemData> relics = new List<ItemData>(); 
    public List<GridPreset> unlockedWeapons = new List<GridPreset>();

    public GridPreset playerHealthPreset;

    [Tooltip("Aktuálne HP políčok HP gridu (index = y * width + x), prenáša sa medzi encountermi. Prázdne = plné HP")]
    public int[] playerCurrentHp;

    [Header("Inventár consumables")]
    public int maxConsumables = 4; // Toľko, koľko je slotov consumables v encountri
    public List<ConsumableData> consumables = new List<ConsumableData>();

    [Header("Štatistiky runu (na výpis pri výhre/smrti)")]
    public int encountersVisited = 0;
    public int shopsVisited = 0;
    public int meadowsUsed = 0;
    public int damageDone = 0;
    public int damageTaken = 0;

    // Unikátne itemy, ktoré hráč počas runu získal (HashSet = každý asset sa ráta len raz)
    private readonly HashSet<ConsumableData> uniquePotions = new HashSet<ConsumableData>();
    private readonly HashSet<ItemData> uniqueRelics = new HashSet<ItemData>();

    public int UniquePotionsCount => uniquePotions.Count;
    public int UniqueRelicsCount => uniqueRelics.Count;

    public static PlayerStats EnsureInstance()
    {
        if (Instance == null)
        {
            Instance = FindAnyObjectByType<PlayerStats>();
            if (Instance == null)
            {
                GameObject go = new GameObject("PlayerStats");
                Instance = go.AddComponent<PlayerStats>();
            }
        }
        return Instance;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private bool isRuntimeHealthPresetInstance;

    public GridPreset EnsureRuntimeHealthPreset(GridPreset fallbackPreset = null)
    {
        if (playerHealthPreset == null && fallbackPreset != null)
        {
            playerHealthPreset = fallbackPreset;
            isRuntimeHealthPresetInstance = false;
        }

        if (playerHealthPreset != null && !isRuntimeHealthPresetInstance)
        {
            playerHealthPreset = Instantiate(playerHealthPreset);
            isRuntimeHealthPresetInstance = true;
        }

        return playerHealthPreset;
    }

    public void InitializeCharacter(CharacterData character)
    {
        if (character == null) return;

        selectedCharacter = character;

        money = character.startingGold;
        maxStamina = character.startingStamina;
        playerHealthPreset = character.healthGridPreset != null
            ? Instantiate(character.healthGridPreset)
            : null;
        isRuntimeHealthPresetInstance = playerHealthPreset != null;
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
        relics.Clear();

        ResetRunStats();

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
            uniqueRelics.Add(item);
            OnRelicsChanged?.Invoke();
        }
    }

    // Nahradí zbraň na danom indexe (napr. zväčšenou kópiou po upgrade - originálny asset sa nemení)
    public void ReplaceWeapon(int index, GridPreset newWeapon)
    {
        if (newWeapon == null || index < 0 || index >= unlockedWeapons.Count) return;
        unlockedWeapons[index] = newWeapon;
        OnWeaponsChanged?.Invoke();
    }

    // Vráti relikviu s daným efektom, ak ju hráč má (inak null)
    public ItemData GetRelic(RelicEffect effect)
    {
        if (effect == RelicEffect.None) return null;
        return relics.Find(r => r != null && r.relicEffect == effect);
    }

    public bool HasRelic(RelicEffect effect) => GetRelic(effect) != null;

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
        uniquePotions.Add(consumable);
        OnConsumablesChanged?.Invoke();
        return true;
    }

    // --- ŠTATISTIKY RUNU ---
    // Volať z miest, kde sa daná vec stane (encounter, shop, meadow, súboj)

    public void RecordEncounterVisited() => encountersVisited++;
    public void RecordShopVisited() => shopsVisited++;
    public void RecordMeadowUsed() => meadowsUsed++;

    public void RecordDamageDone(int amount)
    {
        if (amount > 0) damageDone += amount;
    }

    public void RecordDamageTaken(int amount)
    {
        if (amount > 0) damageTaken += amount;
    }

    public void ResetRunStats()
    {
        encountersVisited = 0;
        shopsVisited = 0;
        meadowsUsed = 0;
        damageDone = 0;
        damageTaken = 0;
        uniquePotions.Clear();
        uniqueRelics.Clear();
    }

    // Zavolá sa po použití consumable v súboji
    public void RemoveConsumable(ConsumableData consumable)
    {
        if (consumables.Remove(consumable)) OnConsumablesChanged?.Invoke();
    }

    // Doplní všetky odomknuté Max HP políčka hráča na maximálnu hrúbku (3)
    public void HealToMax()
    {
        if (playerHealthPreset != null)
        {
            int width = playerHealthPreset.width;
            int height = playerHealthPreset.height;
            playerCurrentHp = new int[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CellSetup cell = playerHealthPreset.GetCell(x, y);
                    if (cell.element != BlockElement.None)
                    {
                        // Hrúbka v presete = maximum políčka (1 až 3)
                        playerCurrentHp[y * width + x] = Mathf.Clamp(cell.thickness, 1, 3);
                    }
                }
            }
        }
        else
        {
            playerCurrentHp = null;
        }
    }
}