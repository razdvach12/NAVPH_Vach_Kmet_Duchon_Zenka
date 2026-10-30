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

[CreateAssetMenu(fileName = "NewItem", menuName = "Shop/ItemData")]
public class ItemData : ScriptableObject 
{
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public int price;
    
    public ItemType itemType;
    public ElementType elementType;

    [Range(1, 3)]
    public int staminaCost = 1;
    public int effectValue;
}