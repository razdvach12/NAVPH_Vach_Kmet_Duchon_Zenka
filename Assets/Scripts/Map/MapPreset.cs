using System.Collections.Generic;
using UnityEngine;

// Preset mapy - nastavíš v ňom tvar mapy, počty miestností a pravidlá.
// Z presetu sa pri štarte hry (výber postavy -> Start) procedurálne vygeneruje mapa.
// Vytvorenie: pravý klik v Project okne -> Create -> Map -> Map Preset
[CreateAssetMenu(fileName = "NewMapPreset", menuName = "Map/Map Preset")]
public class MapPreset : ScriptableObject
{
	[Header("Tvar mapy")]
	[Tooltip("Počet stĺpcov, v ktorých môžu byť uzly")]
	[Min(1)] public int mapWidth = 7;
	[Tooltip("Počet poschodí BEZ bossa (poschodie 0 = spodné, boss je nad posledným poschodím)")]
	[Min(2)] public int mapHeight = 12;
	[Tooltip("Koľko ciest sa vygeneruje zdola nahor (cesty sa môžu zlievať)")]
	[Min(1)] public int numberOfPaths = 5;

	[Header("Rozostupy na Canvase (v pixeloch)")]
	public float xSpacing = 130f;
	public float ySpacing = 140f;
	public float jitter = 25f;

	[Header("1. Pevné poschodia (napr. poschodie 0 = vždy Encounter)")]
	public List<FixedFloorRule> fixedFloors = new List<FixedFloorRule>()
	{
		new FixedFloorRule { floor = 0, nodeType = NodeType.Encounter }
	};

	[Header("2. Počty miestností na celej mape (Encounter vyplní zvyšok)")]
	public List<RoomCountRule> roomCounts = new List<RoomCountRule>()
	{
		new RoomCountRule { nodeType = NodeType.Shop,     min = 3, max = 4, minFloor = 2, maxFloor = 10 },
		new RoomCountRule { nodeType = NodeType.Treasure, min = 2, max = 3, minFloor = 4, maxFloor = 9 },
		new RoomCountRule { nodeType = NodeType.Rest,     min = 3, max = 5, minFloor = 3, maxFloor = 11 },
		new RoomCountRule { nodeType = NodeType.Mystery,  min = 5, max = 8, minFloor = 1, maxFloor = 10 }
	};

	[Header("3. Garantované miestnosti na KAŽDEJ ceste")]
	public List<PathCountRule> pathCountRules = new List<PathCountRule>()
	{
		new PathCountRule { nodeType = NodeType.Shop, minPerPath = 1, maxPerPath = 2, minFloor = 2, maxFloor = 10 }
	};

	[Header("4. Minimálna vzdialenosť medzi rovnakými miestnosťami")]
	public List<CooldownRule> cooldownRules = new List<CooldownRule>()
	{
		new CooldownRule { nodeType = NodeType.Rest, minDistanceBetween = 2 },
		new CooldownRule { nodeType = NodeType.Shop, minDistanceBetween = 2 }
	};

	[Header("Obtiažnosť Encounterov podľa poschodia")]
	[Tooltip("Prvé pravidlo, do ktorého poschodie spadá, určí obtiažnosť")]
	public List<EncounterDifficultyRule> encounterDifficulty = new List<EncounterDifficultyRule>()
	{
		new EncounterDifficultyRule { minFloor = 0, maxFloor = 99, difficulty = RoomDifficulty.Easy }
	};
	[Tooltip("Použije sa, ak poschodie nespadá do žiadneho pravidla vyššie")]
	public RoomDifficulty defaultEncounterDifficulty = RoomDifficulty.Easy;

	[Header("Boss (posledná miestnosť)")]
	[Tooltip("Konkrétna boss room. Ak je prázdne, vyberie sa náhodná room s obtiažnosťou Boss z TurnManagera.")]
	public RoomData bossRoom;

	public RoomDifficulty GetEncounterDifficulty(int floor)
	{
		foreach (var rule in encounterDifficulty)
		{
			if (rule != null && floor >= rule.minFloor && floor <= rule.maxFloor)
				return rule.difficulty;
		}
		return defaultEncounterDifficulty;
	}

	private void OnValidate()
	{
		if (roomCounts == null) return;
		foreach (var rule in roomCounts)
		{
			if (rule == null) continue;
			rule.min = Mathf.Max(0, rule.min);
			rule.max = Mathf.Max(rule.min, rule.max);

			if (rule.nodeType == NodeType.Encounter || rule.nodeType == NodeType.Boss)
				Debug.LogWarning($"{name}: počet pre {rule.nodeType} sa v 'Počty miestností' ignoruje - Encounter vyplní zvyšok a Boss je vždy jeden.", this);
		}
	}
}
