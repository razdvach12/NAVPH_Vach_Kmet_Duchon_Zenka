using System.Collections.Generic;
using UnityEngine;

public enum EnemyTargeting
{
    Random,     // Náhodné miesto, ktoré zasiahne aspoň jedno živé políčko
    BestHit     // Mierený útok - miesto, kde spôsobí najviac poškodenia
}

[System.Serializable]
public class EnemyAttack
{
    public GridPreset pattern;
    public EnemyTargeting targeting = EnemyTargeting.Random;
}

[CreateAssetMenu(fileName = "NewEnemy", menuName = "Combat/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName;

    [Tooltip("Tier nepriateľa - miestnosti (rooms) sa skladajú z nepriateľov daného tieru")]
    [Range(1, 3)]
    public int tier = 1;

    [Header("Grid životov nepriateľa")]
    public GridPreset bodyPreset;

    [Header("Útoky (každé kolo sa náhodne vyberie jeden)")]
    public List<EnemyAttack> attacks = new List<EnemyAttack>();

    [Header("Schopnosti na konci kola (napr. boss)")]
    [Tooltip("Celý grid nepriateľa sa otočí o 90° v smere hodinových ručičiek (iba štvorcový grid)")]
    public bool rotateGridEachRound;
    [Tooltip("Živlové políčka sa zmenia: Lava -> Ice -> Nature -> Rock -> Lava")]
    public bool cycleElementsEachRound;

    [Header("Odmena")]
    public int goldReward = 10;

    public EnemyAttack GetRandomAttack()
    {
        if (attacks == null || attacks.Count == 0) return null;
        return attacks[Random.Range(0, attacks.Count)];
    }
}
