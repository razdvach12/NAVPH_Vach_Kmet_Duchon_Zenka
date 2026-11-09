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

[System.Serializable]
public class NodeSpawnWeight
{
	public NodeType nodeType;
	[Range(0, 100)] public int weight = 20;
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