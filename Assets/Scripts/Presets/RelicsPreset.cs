using UnityEngine;

[CreateAssetMenu(fileName = "NewRelic", menuName = "Inventory/Relic")]
public class Relic : ScriptableObject
{
    public string relicName;
    
    [Header("Passive Stats")]
    public float bonusHealth;
    public float bonusDamage;
    public float dodgeChance; 
}