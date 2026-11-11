using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
	[Header("Map Dimensions")]
	public int mapWidth = 7;
	public int mapHeight = 12;
	public int numberOfPaths = 5;

	[Header("Canvas Spacing (in pixels)")]
	public float xSpacing = 130f;
	public float ySpacing = 140f;
	public float jitter = 25f;

	[Header("1. Rules: Guaranteed Path Count (Range)")]
	public List<PathCountRule> pathCountRules = new List<PathCountRule>()
	{
        // Default rule: At least 1 and max 2 Shops on every path
        new PathCountRule { nodeType = NodeType.Shop, minPerPath = 1, maxPerPath = 2, minFloor = 2, maxFloor = 10 }
	};

	[Header("2. Rules: Minimum Distance (Cooldown)")]
	public List<CooldownRule> cooldownRules = new List<CooldownRule>()
	{
        // Default rules: Rest and Shop must have at least 2 other nodes between them
        new CooldownRule { nodeType = NodeType.Rest, minDistanceBetween = 2 },
		new CooldownRule { nodeType = NodeType.Shop, minDistanceBetween = 2 }
	};

	[Header("3. Spawn Weights for Remaining Nodes")]
	public List<NodeSpawnWeight> randomWeights = new List<NodeSpawnWeight>()
	{
		new NodeSpawnWeight { nodeType = NodeType.Encounter, weight = 50 },
		new NodeSpawnWeight { nodeType = NodeType.Mystery, weight = 25 },
		new NodeSpawnWeight { nodeType = NodeType.Rest, weight = 15 },
		new NodeSpawnWeight { nodeType = NodeType.Treasure, weight = 10 }
	};

	private MapNodeData[,] grid;
	private List<List<MapNodeData>> allPaths = new List<List<MapNodeData>>();

	[HideInInspector] public List<MapNodeData> allNodesList = new List<MapNodeData>();
	[HideInInspector] public MapNodeData bossNode;

	public List<MapNodeData> GenerateNewMap()
	{
		grid = new MapNodeData[mapWidth, mapHeight];
		allPaths.Clear();
		allNodesList.Clear();

		// 1. Generate paths from bottom to top
		for (int p = 0; p < numberOfPaths; p++)
		{
			List<MapNodeData> currentPath = new List<MapNodeData>();
			int currX = Random.Range(0, mapWidth);

			for (int y = 0; y < mapHeight; y++)
			{
				if (grid[currX, y] == null)
					grid[currX, y] = CreateNodeAt(currX, y);

				MapNodeData currentNode = grid[currX, y];
				currentPath.Add(currentNode);

				if (y < mapHeight - 1)
				{
					int nextX = GetValidNextX(currX, y);
					if (grid[nextX, y + 1] == null)
						grid[nextX, y + 1] = CreateNodeAt(nextX, y + 1);

					MapNodeData nextNode = grid[nextX, y + 1];
					if (!currentNode.nextNodes.Contains(nextNode))
						currentNode.nextNodes.Add(nextNode);

					currX = nextX;
				}
			}
			allPaths.Add(currentPath);
		}

		// Create Boss node at the top center
		Vector2 bossPos = new Vector2(0, mapHeight * ySpacing + 80f);
		bossNode = new MapNodeData(mapWidth / 2, mapHeight, bossPos);
		bossNode.type = NodeType.Boss;
		allNodesList.Add(bossNode);

		for (int x = 0; x < mapWidth; x++)
		{
			if (grid[x, mapHeight - 1] != null)
				grid[x, mapHeight - 1].nextNodes.Add(bossNode);
		}

		// 2. Apply rules and seed room types
		ApplyRulesAndSeedRooms();

		return allNodesList;
	}

	private MapNodeData CreateNodeAt(int x, int y)
	{
		float posX = (x - (mapWidth - 1) / 2f) * xSpacing + Random.Range(-jitter, jitter);
		float posY = y * ySpacing + Random.Range(-jitter, jitter);
		MapNodeData node = new MapNodeData(x, y, new Vector2(posX, posY));
		allNodesList.Add(node);
		return node;
	}

	private int GetValidNextX(int currX, int currY)
	{
		List<int> validX = new List<int>();
		for (int offset = -1; offset <= 1; offset++)
		{
			int candidateX = currX + offset;
			if (candidateX < 0 || candidateX >= mapWidth) continue;

			// Prevent paths from crossing in an X shape
			if (offset == -1 && currX > 0 && grid[currX - 1, currY] != null)
			{
				if (grid[currX - 1, currY].nextNodes.Exists(n => n.x == currX && n.y == currY + 1))
					continue;
			}
			if (offset == 1 && currX < mapWidth - 1 && grid[currX + 1, currY] != null)
			{
				if (grid[currX + 1, currY].nextNodes.Exists(n => n.x == currX && n.y == currY + 1))
					continue;
			}
			validX.Add(candidateX);
		}
		return validX[Random.Range(0, validX.Count)];
	}

	private void ApplyRulesAndSeedRooms()
	{
		// STEP A: Randomly fill the map based on weights and Cooldown rules
		for (int y = 0; y < mapHeight; y++)
		{
			for (int x = 0; x < mapWidth; x++)
			{
				MapNodeData node = grid[x, y];
				if (node == null) continue;

				// First floor is always a basic Encounter
				if (y == 0)
				{
					node.type = NodeType.Encounter;
					continue;
				}

				node.type = PickRandomValidType(node);
			}
		}

		// STEP B: Enforce PathCountRules (e.g., at least 1 Shop on every path)
		foreach (var rule in pathCountRules)
		{
			foreach (var path in allPaths)
			{
				// Count how many times this node type already appears on this path
				int currentCount = path.FindAll(n => n.type == rule.nodeType).Count;

				// If count is below minPerPath, inject missing nodes
				int safetyCounter = 0;
				while (currentCount < rule.minPerPath && safetyCounter < 20)
				{
					safetyCounter++;
					// Find suitable nodes on the path within the allowed floor range
					List<MapNodeData> candidates = path.FindAll(n =>
						n.y >= rule.minFloor &&
						n.y <= rule.maxFloor &&
						n.type == NodeType.Encounter && // Only overwrite basic encounters, not other special rooms
						MapRuleValidator.CanPlaceNodeType(n, rule.nodeType, cooldownRules, allNodesList)
					);

					// Fallback: if strict cooldown left no candidates, allow any Encounter within floor range
					if (candidates.Count == 0)
					{
						candidates = path.FindAll(n => n.y >= rule.minFloor && n.y <= rule.maxFloor && n.type == NodeType.Encounter);
					}

					if (candidates.Count > 0)
					{
						MapNodeData chosen = candidates[Random.Range(0, candidates.Count)];
						chosen.type = rule.nodeType;
						currentCount++;
					}
				}
			}
		}
	}

	private NodeType PickRandomValidType(MapNodeData node)
	{
		// Filter only node types that do not violate Cooldown rules at this node
		List<NodeSpawnWeight> validWeights = new List<NodeSpawnWeight>();
		int totalWeight = 0;

		foreach (var w in randomWeights)
		{
			if (w.weight > 0 && MapRuleValidator.CanPlaceNodeType(node, w.nodeType, cooldownRules, allNodesList))
			{
				validWeights.Add(w);
				totalWeight += w.weight;
			}
		}

		if (validWeights.Count == 0 || totalWeight == 0)
			return NodeType.Encounter; // Default fallback

		int roll = Random.Range(0, totalWeight);
		int cursor = 0;
		foreach (var w in validWeights)
		{
			cursor += w.weight;
			if (roll < cursor)
				return w.nodeType;
		}

		return NodeType.Encounter;
	}
}