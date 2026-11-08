using UnityEngine;
using UnityEngine.UI; // Potrebné kvôli práci s komponentom Button
using System.Collections.Generic;

public class ShopManager : MonoBehaviour 
{
    public static ShopManager Instance;

    [Header("Databáza všetkých predmetov")]
    public List<ItemData> allWeaponItems;
    public List<ItemData> allConsumableItems;
    public List<ItemData> allRelicItems; 
    public List<ItemData> allUpgradeItems; 

    [Header("UI Sloty v obchode")]
    public List<ShopSlotUI> weaponSlots;
    public List<ShopSlotUI> consumableSlots;
    public List<ShopSlotUI> relicSlots; 
    public List<ShopSlotUI> upgradeSlots; 

    [Header("Texty pri vypredaní (UI)")]
    public GameObject emptyWeaponsText;
    public GameObject emptyConsumablesText;
    public GameObject emptyRelicsText;

    // --- NOVÉ: Tlačidlá jednotlivých sekcií pre prepínanie ---
    [Header("Tlačidlá sekcií v menu obchodu")]
    public Button weaponTabButton;
    public Button consumableTabButton;
    public Button relicTabButton;

    void Awake()
    {
        Instance = this;
    }

    void Start() 
    {
        GenerateShopStock();
    }

    // Volajte túto metódu vždy, keď sa niečo v obchode kúpi (napr. zo ShopSlotUI)
    void Update()
    {
        // Preventívne priebežne kontrolujeme stav sekcií (prípadne to môžete volať len priamo pri nákupe)
        CheckIfSectionsAreEmpty();
    }

    public void GenerateShopStock() 
    {
        FillSlotsRandomly(allWeaponItems, weaponSlots);
        FillSlotsRandomly(allConsumableItems, consumableSlots);

        List<ItemData> relicsAndUpgradesCombined = new List<ItemData>();
        if (allRelicItems != null) relicsAndUpgradesCombined.AddRange(allRelicItems);
        if (allUpgradeItems != null) relicsAndUpgradesCombined.AddRange(allUpgradeItems);

        FillSlotsRandomly(relicsAndUpgradesCombined, relicSlots);

        CheckIfSectionsAreEmpty();
    }

    void FillSlotsRandomly(List<ItemData> sourceDatabase, List<ShopSlotUI> targetSlots) 
    {
        if (sourceDatabase == null || sourceDatabase.Count == 0) 
        {
            foreach(var slot in targetSlots) if (slot != null) slot.SetItem(null);
            return;
        }

        List<ItemData> availableItems = new List<ItemData>(sourceDatabase);

        foreach (ShopSlotUI slot in targetSlots) 
        {
            if (slot == null) continue;

            if (availableItems.Count > 0) 
            {
                int randomIndex = Random.Range(0, availableItems.Count);
                ItemData chosenItem = availableItems[randomIndex];
                slot.SetItem(chosenItem);
                availableItems.RemoveAt(randomIndex);
            } 
            else 
            {
                slot.SetItem(null);
            }
        }
    }

    // --- KONTROLA VYPREDANIA A ZNEFUNKČNENIE TLAČIDIEL ---
    public void CheckIfSectionsAreEmpty()
    {
        // 1. Sekcia Zbraní
        bool weaponsEmpty = IsSectionEmpty(weaponSlots);
        if (emptyWeaponsText != null) 
            emptyWeaponsText.SetActive(weaponsEmpty);
        if (weaponTabButton != null) 
            weaponTabButton.interactable = !weaponsEmpty; // Ak je vypredané, interactor vypneme (false)

        // 2. Sekcia Spotrebného materiálu
        bool consumablesEmpty = IsSectionEmpty(consumableSlots);
        if (emptyConsumablesText != null) 
            emptyConsumablesText.SetActive(consumablesEmpty);
        if (consumableTabButton != null) 
            consumableTabButton.interactable = !consumablesEmpty;

        // 3. Sekcia Relikvií
        bool relicsEmpty = IsSectionEmpty(relicSlots);
        if (emptyRelicsText != null) 
            emptyRelicsText.SetActive(relicsEmpty);
        if (relicTabButton != null) 
            relicTabButton.interactable = !relicsEmpty;
    }

    private bool IsSectionEmpty(List<ShopSlotUI> slots)
    {
        if (slots == null || slots.Count == 0) return true;
        
        foreach (ShopSlotUI slot in slots)
        {
            // Ak slot existuje, má priradený predmet A ZÁROVEŇ ešte nie je kupujúci/predmet minutý (predpokladáme, že po nákupe sa item zmaže alebo nastaví na null)
            if (slot != null && slot.assignedItem != null) 
            {
                return false; 
            }
        }
        return true; 
    }
}