using UnityEngine;

public enum ItemType 
{ 
    Weapon,
    Consumable,
    Relic,
    Upgrade
}

public enum ElementType 
{ 
    None, 
    Lava, 
    Ice, 
    Nature, 
    Rock 
}

// Efekt relikvie v súboji (None = relikvia zatiaľ nemá efekt)
public enum RelicEffect
{
    None,
    ShieldOfCourage, // Na začiatku ťahu ochráni jedno náhodné živé políčko hráča (ak má aspoň 4 živé)
    FastFeather      // +2 max stamina v súboji, kým ju hráč má
}

// Čo robí upgrade (ItemType.Upgrade)
public enum UpgradeEffect
{
    HealthGrid, // Pridá Max HP políčko (elementType None) alebo zmení živel políčka (elementType Lava/Ice/...)
    WeaponSize  // Pridá jedno políčko (1 damage) na zbraň, ktorú si hráč vyberie
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Shop/ItemData")]
public class ItemData : ScriptableObject 
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public int price;

    [Tooltip("0 = po kúpe v obchode zmizne. Viac ako 0 = ostane v obchode a zdražie o túto sumu (platí iba počas jednej návštevy obchodu)")]
    [Min(0)] public int priceIncreasePerPurchase = 0;
    
    public ItemType itemType;
    public ElementType elementType;

    [Range(0, 3)]
    public int staminaCost = 1;
    public int effectValue;

    [Tooltip("Iba pre upgrady - čo upgrade robí")]
    public UpgradeEffect upgradeEffect = UpgradeEffect.HealthGrid;

    [Tooltip("Iba pre relikvie - čo relikvia robí v súboji")]
    public RelicEffect relicEffect = RelicEffect.None;

    [Header("Prepojenie na PlayerStats")]
    public GridPreset weaponGridPreset; 
}