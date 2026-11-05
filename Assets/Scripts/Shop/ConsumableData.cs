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
    public GridPreset[] consumableShapes; // Tvary zásahu - len pre DamageEnemy a HealPlayer, pri použití sa vyberie náhodný

    // Náhodne vyberie jeden z tvarov (null, ak žiadny nie je nastavený)
    public GridPreset GetRandomShape()
    {
        if (consumableShapes == null || consumableShapes.Length == 0) return null;
        return consumableShapes[Random.Range(0, consumableShapes.Length)];
    }
}
