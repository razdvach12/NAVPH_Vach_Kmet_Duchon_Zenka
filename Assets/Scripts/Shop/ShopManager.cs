using UnityEngine;
using System.Collections.Generic;

public class ShopManager : MonoBehaviour 
{
    [Header("Databáza všetkých predmetov")]
    public List<ItemData> allWeaponItems;
    public List<ItemData> allConsumableItems;

    [Header("UI Sloty v obchode")]
    public List<ShopSlotUI> weaponSlots;
    public List<ShopSlotUI> consumableSlots;

    void Start() 
    {
        GenerateShopStock();
    }

    public void GenerateShopStock() 
    {
        FillSlotsRandomly(allWeaponItems, weaponSlots);
        FillSlotsRandomly(allConsumableItems, consumableSlots);
    }

    void FillSlotsRandomly(List<ItemData> sourceDatabase, List<ShopSlotUI> targetSlots) 
    {
        if (sourceDatabase == null || sourceDatabase.Count == 0) return;

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
}