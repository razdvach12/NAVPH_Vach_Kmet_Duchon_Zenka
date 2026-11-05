using System.Collections.Generic;
using UnityEngine;

public enum RoomDifficulty { Easy, Medium, Hard, Boss }

// Miesto v room, kam sa pri štarte encountera vyberie náhodný nepriateľ daného tieru
[System.Serializable]
public class RoomSlot
{
    [Range(1, 3)]
    public int tier = 1;

    [Tooltip("Ak je vyplnený, na miesto sa dá vždy tento nepriateľ (napr. boss) - tier sa ignoruje")]
    public EnemyData fixedEnemy;

    [Tooltip("Ľavý horný roh miesta na gride (x = stĺpec zľava, y = riadok zhora)")]
    public Vector2Int position;
}

// Konkrétny nepriateľ a jeho pozícia na gride (výsledok výberu pre room)
public class PlacedEnemy
{
    public EnemyData enemy;
    public Vector2Int position;
}

[CreateAssetMenu(fileName = "NewRoom", menuName = "Combat/Room Data")]
public class RoomData : ScriptableObject
{
    public RoomDifficulty difficulty = RoomDifficulty.Easy;
    public List<RoomSlot> slots = new List<RoomSlot>();

    // Nové prvky pridané cez "+" v Inspectore majú tier 0 - opravíme ho na platnú hodnotu
    private void OnValidate()
    {
        if (slots == null) return;
        foreach (RoomSlot slot in slots)
        {
            if (slot != null) slot.tier = Mathf.Clamp(slot.tier, 1, 3);
        }
    }

    // Veľkosť miesta pre nepriateľa daného tieru (null = berie sa skutočná veľkosť nepriateľa)
    public static Vector2Int? GetSlotSize(int tier)
    {
        if (tier == 1) return new Vector2Int(5, 5);
        return null;
    }

    // Na každé miesto vyberie náhodného nepriateľa daného tieru a vycentruje ho v mieste
    public List<PlacedEnemy> PickEnemies(List<EnemyData> enemyPool)
    {
        var result = new List<PlacedEnemy>();

        foreach (RoomSlot slot in slots)
        {
            if (slot.fixedEnemy != null)
            {
                result.Add(new PlacedEnemy { enemy = slot.fixedEnemy, position = slot.position });
                continue;
            }

            if (enemyPool == null || enemyPool.Count == 0)
            {
                Debug.LogWarning($"Room {name}: zoznam Enemy Pool na TurnManager je prázdny - nie je z koho vyberať.");
                result.Add(null);
                continue;
            }

            var candidates = enemyPool.FindAll(e => e != null && e.bodyPreset != null && e.tier == slot.tier);
            if (candidates.Count == 0)
            {
                Debug.LogWarning($"Room {name}: v zozname nepriateľov nie je nikto z tieru {slot.tier}.");
                result.Add(null);
                continue;
            }

            EnemyData enemy = candidates[Random.Range(0, candidates.Count)];
            Vector2Int position = slot.position;

            Vector2Int? slotSize = GetSlotSize(slot.tier);
            if (slotSize.HasValue)
            {
                position.x += (slotSize.Value.x - enemy.bodyPreset.width) / 2;
                position.y += (slotSize.Value.y - enemy.bodyPreset.height) / 2;
            }

            result.Add(new PlacedEnemy { enemy = enemy, position = position });
        }

        return result;
    }
}
