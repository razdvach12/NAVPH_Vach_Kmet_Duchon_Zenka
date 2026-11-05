using UnityEngine;

public enum ConsumableEffect
{
    None,
    DamageEnemy,
    HealPlayer,
    Flee,
    FreeNextAttack
}


[CreateAssetMenu(fileName = "NewConsumable", menuName = "Shop/ConsumableData")]
public class ConsumableData : ItemData
{
    [Header("Consumable")]
    public ConsumableEffect consumableEffect;
    public GridPreset consumableShape; // Tvar zásahu - len pre DamageEnemy a HealPlayer
}
