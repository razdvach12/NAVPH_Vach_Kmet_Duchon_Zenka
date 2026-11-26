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

// Efekt relikvie v súboji (None = bez špeciálneho efektu, prípadne riadené čisto cez Relic Modifiers nižšie)
public enum RelicEffect
{
    None,
    ShieldOfCourage, // Na začiatku ťahu ochráni jedno náhodné živé políčko hráča (ak má aspoň 4 živé)
    FastFeather,     // +max stamina v súboji (predvolene +2, nastaviteľné cez relicMaxStaminaBonus)
    LuckyBear,       // Bonus k goldom za vyhrané kolo (predvolene náhodne 30% až 60%)
    AlchemistAmulet, // Zľava na consumables v obchode (predvolene -30%)
    CursedCoin,      // Zľava v celom obchode (predvolene -50%, stackuje sa s ostatnými zľavami)
    HeartOfMagna     // Premení živlové políčka nepriateľa (predvolene Lava) na normálne (Basic)
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

    [Header("Relic Modifiers (modulárne nastavenia)")]
    [Tooltip("Bonus k maximálnej stamine v súboji (napr. 2 pre Fast Feather)")]
    public int relicMaxStaminaBonus = 0;

    [Tooltip("Zľava na celý obchod (0.5 = -50 %, stackuje sa s ostatnými zľavami)")]
    [Range(0f, 1f)] public float relicShopDiscount = 0f;

    [Tooltip("Extra zľava na consumables (0.3 = -30 %, stackuje sa s relicShopDiscount)")]
    [Range(0f, 1f)] public float relicConsumableDiscount = 0f;

    [Tooltip("Minimálny percentuálny bonus ku goldom za kolo (0.3 = +30 %)")]
    [Min(0f)] public float relicGoldBonusMin = 0f;

    [Tooltip("Maximálny percentuálny bonus ku goldom za kolo (0.6 = +60 %)")]
    [Min(0f)] public float relicGoldBonusMax = 0f;

    [Tooltip("Živel nepriateľa, ktorý sa na začiatku boja a pri zmene živlov premení na normálne (Basic) políčko")]
    public BlockElement relicNeutralizedEnemyElement = BlockElement.None;

    [Header("Prepojenie na PlayerStats")]
    public GridPreset weaponGridPreset; 

    public int GetMaxStaminaBonus()
    {
        if (relicMaxStaminaBonus != 0) return relicMaxStaminaBonus;
        return relicEffect == RelicEffect.FastFeather ? 2 : 0;
    }

    public float GetShopDiscountFor(ItemType targetType)
    {
        float discount = relicShopDiscount > 0f
            ? relicShopDiscount
            : (relicEffect == RelicEffect.CursedCoin ? 0.5f : 0f);

        if (targetType == ItemType.Consumable)
        {
            discount += relicConsumableDiscount > 0f
                ? relicConsumableDiscount
                : (relicEffect == RelicEffect.AlchemistAmulet ? 0.3f : 0f);
        }

        return discount;
    }

    public void GetGoldBonusRange(out float minPercent, out float maxPercent)
    {
        if (relicGoldBonusMin > 0f || relicGoldBonusMax > 0f)
        {
            minPercent = Mathf.Min(relicGoldBonusMin, relicGoldBonusMax);
            maxPercent = Mathf.Max(relicGoldBonusMin, relicGoldBonusMax);
            return;
        }

        if (relicEffect == RelicEffect.LuckyBear)
        {
            minPercent = 0.3f;
            maxPercent = 0.6f;
            return;
        }

        minPercent = 0f;
        maxPercent = 0f;
    }

    public BlockElement GetNeutralizedEnemyElement()
    {
        if (relicNeutralizedEnemyElement != BlockElement.None) return relicNeutralizedEnemyElement;
        return relicEffect == RelicEffect.HeartOfMagna ? BlockElement.Lava : BlockElement.None;
    }
}