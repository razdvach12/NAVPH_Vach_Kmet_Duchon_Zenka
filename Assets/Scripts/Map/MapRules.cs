using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathCountRule
{
	public NodeType nodeType = NodeType.Shop;
	[Tooltip("Minimal number of this type of rooms on ALL paths")]
	public int minPerPath = 1;
	[Tooltip("Maximal number of this type of rooms on ONE path")]
	public int maxPerPath = 2;
	[Tooltip("From what floor number this type of room can spawn")]
	public int minFloor = 2;
	[Tooltip("To what floor number this type of room can spawn")]
	public int maxFloor = 10;
}

[System.Serializable]
public class CooldownRule
{
	public NodeType nodeType = NodeType.Rest;
	[Tooltip("How many rooms needs to be between two rooms of the same type (1 = they cant be one after other, 2 = there need to be at least 2 rooms of different type between them)")]
	public int minDistanceBetween = 2;
}

// Koľko miestností daného typu má byť na CELEJ mape (náhodne medzi min a max)
[System.Serializable]
public class RoomCountRule
{
	public NodeType nodeType = NodeType.Shop;
	[Tooltip("Minimálny počet na celej mape")]
	public int min = 2;
	[Tooltip("Maximálny počet na celej mape (min = max -> presný počet)")]
	public int max = 3;
	[Tooltip("Od ktorého poschodia sa môže objaviť (0 = spodné)")]
	public int minFloor = 1;
	[Tooltip("Po ktoré poschodie sa môže objaviť")]
	public int maxFloor = 11;
}

// Celé poschodie bude vždy tohto typu (napr. 0 = Encounter, posledné = Rest)
[System.Serializable]
public class FixedFloorRule
{
	[Tooltip("Číslo poschodia (0 = spodné)")]
	public int floor = 0;
	public NodeType nodeType = NodeType.Encounter;
}

// Akú obtiažnosť majú Encountery na daných poschodiach
[System.Serializable]
public class EncounterDifficultyRule
{
	public int minFloor = 0;
	public int maxFloor = 99;
	public RoomDifficulty difficulty = RoomDifficulty.Easy;
}

public static class MapRuleValidator
{
	public static bool CanPlaceNodeType(
		MapNodeData targetNode,
		NodeType typeToPlace,
		List<CooldownRule> cooldownRules,
		List<MapNodeData> allNodes)
	{

		CooldownRule rule = cooldownRules.Find(r => r.nodeType == typeToPlace);
		if (rule == null || rule.minDistanceBetween <= 0)
			return true;

		if (HasTypeInStepsForward(targetNode, typeToPlace, rule.minDistanceBetween))
			return false;

		if (HasTypeInStepsBackward(targetNode, typeToPlace, rule.minDistanceBetween, allNodes))
			return false;

		return true;
	}

	private static bool HasTypeInStepsForward(MapNodeData current, NodeType type, int stepsLeft)
	{
		if (stepsLeft <= 0) return false;
		foreach (var next in current.nextNodes)
		{
			if (next.type == type) return true;
			if (HasTypeInStepsForward(next, type, stepsLeft - 1)) return true;
		}
		return false;
	}

	private static bool HasTypeInStepsBackward(MapNodeData current, NodeType type, int stepsLeft, List<MapNodeData> allNodes)
	{
		if (stepsLeft <= 0) return false;
		foreach (var prev in allNodes)
		{
			if (prev.nextNodes.Contains(current))
			{
				if (prev.type == type) return true;
				if (HasTypeInStepsBackward(prev, type, stepsLeft - 1, allNodes)) return true;
			}
		}
		return false;
	}
}
