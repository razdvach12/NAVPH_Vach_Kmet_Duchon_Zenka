using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour 
{
    public static ShopManager Instance; // Pridané pre ľahké prepojenie so slotmi

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

    // --- NOVÉ POLIA PRE TEXTY VYPREDANIA ---
    [Header("Texty pri vypredaní (UI)")]
    public GameObject emptyWeaponsText;
    public GameObject emptyConsumablesText;
    public GameObject emptyRelicsText;

    void Awake()
    {
        Instance = this;
    }

    void Start() 
    {
        GenerateShopStock();
    }

    public void GenerateShopStock() 
    {
        FillSlotsRandomly(allWeaponItems, weaponSlots);
        FillSlotsRandomly(allConsumableItems, consumableSlots);

        List<ItemData> relicsAndUpgradesCombined = new List<ItemData>();
        if (allRelicItems != null) relicsAndUpgradesCombined.AddRange(allRelicItems);
        if (allUpgradeItems != null) relicsAndUpgradesCombined.AddRange(allUpgradeItems);

        FillSlotsRandomly(relicsAndUpgradesCombined, relicSlots);

        // Hneď po vygenerovaní skontrolujeme, či náhodou nie je niečo okamžite vypredané
        CheckIfSectionsAreEmpty();
    }

    void FillSlotsRandomly(List<ItemData> sourceDatabase, List<ShopSlotUI> targetSlots) 
    {
        if (sourceDatabase == null || sourceDatabase.Count == 0) 
        {
            // Ak je databáza prázdna, vyčistíme všetky sloty
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

    // --- TÁTO FUNKCIA SKONTROLUJE, ČI SA MAJÚ UKÁZAŤ TEXTY ---
    public void CheckIfSectionsAreEmpty()
    {
        if (emptyWeaponsText != null) 
            emptyWeaponsText.SetActive(IsSectionEmpty(weaponSlots));
            
        if (emptyConsumablesText != null) 
            emptyConsumablesText.SetActive(IsSectionEmpty(consumableSlots));
            
        if (emptyRelicsText != null) 
            emptyRelicsText.SetActive(IsSectionEmpty(relicSlots));
    }

    // Zistí, či sú všetky sloty v danej sekcii prázdne (assignedItem == null)
    private bool IsSectionEmpty(List<ShopSlotUI> slots)
    {
        if (slots == null || slots.Count == 0) return true;
        
        foreach (ShopSlotUI slot in slots)
        {
            if (slot.assignedItem != null) 
            {
                return false; // Ak aspoň jeden slot má predmet, sekcia NIE JE prázdna
            }
        }
        return true; 
    }
}