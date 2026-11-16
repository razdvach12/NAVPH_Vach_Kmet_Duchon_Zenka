using System.Collections.Generic;
using UnityEngine;

// Výsledok generovania - zoznam uzlov a boss na vrchu
public class GeneratedMap
{
	public List<MapNodeData> nodes = new List<MapNodeData>();
	public MapNodeData bossNode;
}

// Procedurálne vygeneruje mapu podľa MapPresetu.
// Je statický (nie MonoBehaviour), aby sa dal zavolať z hociktorej scény - napr. pri odchode z výberu postavy.
public static class MapGenerator
{
	public static GeneratedMap Generate(MapPreset preset)
	{
		int width = Mathf.Max(1, preset.mapWidth);
		int height = Mathf.Max(2, preset.mapHeight);

		var grid = new MapNodeData[width, height];
		var allPaths = new List<List<MapNodeData>>();
		var map = new GeneratedMap();

		// 1. Cesty zdola nahor
		for (int p = 0; p < preset.numberOfPaths; p++)
		{
			var currentPath = new List<MapNodeData>();
			int currX = Random.Range(0, width);

			for (int y = 0; y < height; y++)
			{
				if (grid[currX, y] == null)
					grid[currX, y] = CreateNodeAt(preset, map, currX, y);

				MapNodeData currentNode = grid[currX, y];
				currentPath.Add(currentNode);

				if (y < height - 1)
				{
					int nextX = GetValidNextX(grid, width, currX, y);
					if (grid[nextX, y + 1] == null)
						grid[nextX, y + 1] = CreateNodeAt(preset, map, nextX, y + 1);

					MapNodeData nextNode = grid[nextX, y + 1];
					if (!currentNode.nextNodes.Contains(nextNode))
						currentNode.nextNodes.Add(nextNode);

					currX = nextX;
				}
			}
			allPaths.Add(currentPath);
		}

		// 2. Boss hore v strede - posledná miestnosť
		Vector2 bossPos = new Vector2(0, height * preset.ySpacing + 80f);
		map.bossNode = new MapNodeData(width / 2, height, bossPos) { type = NodeType.Boss };
		map.nodes.Add(map.bossNode);

		for (int x = 0; x < width; x++)
		{
			if (grid[x, height - 1] != null)
				grid[x, height - 1].nextNodes.Add(map.bossNode);
		}

		// 3. Typy miestností podľa pravidiel z presetu
		AssignRoomTypes(preset, map.nodes, allPaths);

		return map;
	}

	private static MapNodeData CreateNodeAt(MapPreset preset, GeneratedMap map, int x, int y)
	{
		float posX = (x - (preset.mapWidth - 1) / 2f) * preset.xSpacing + Random.Range(-preset.jitter, preset.jitter);
		float posY = y * preset.ySpacing + Random.Range(-preset.jitter, preset.jitter);
		var node = new MapNodeData(x, y, new Vector2(posX, posY));
		map.nodes.Add(node);
		return node;
	}

	private static int GetValidNextX(MapNodeData[,] grid, int width, int currX, int currY)
	{
		var validX = new List<int>();
		for (int offset = -1; offset <= 1; offset++)
		{
			int candidateX = currX + offset;
			if (candidateX < 0 || candidateX >= width) continue;

			// Cesty sa nesmú krížiť do X
			if (offset == -1 && currX > 0 && grid[currX - 1, currY] != null)
			{
				if (grid[currX - 1, currY].nextNodes.Exists(n => n.x == currX && n.y == currY + 1))
					continue;
			}
			if (offset == 1 && currX < width - 1 && grid[currX + 1, currY] != null)
			{
				if (grid[currX + 1, currY].nextNodes.Exists(n => n.x == currX && n.y == currY + 1))
					continue;
			}
			validX.Add(candidateX);
		}
		return validX[Random.Range(0, validX.Count)];
	}

	private static void AssignRoomTypes(MapPreset preset, List<MapNodeData> allNodes, List<List<MapNodeData>> allPaths)
	{
		// Uzly, ktoré už nesmú zmeniť typ (pevné poschodia)
		var locked = new HashSet<MapNodeData>();

		// STEP A: všetko je Encounter, okrem bossa
		foreach (var node in allNodes)
		{
			if (node.type != NodeType.Boss) node.type = NodeType.Encounter;
		}

		// STEP B: pevné poschodia
		foreach (var rule in preset.fixedFloors)
		{
			if (rule == null || rule.nodeType == NodeType.Boss) continue;
			foreach (var node in allNodes)
			{
				if (node.type == NodeType.Boss || node.y != rule.floor) continue;
				node.type = rule.nodeType;
				locked.Add(node);
			}
		}

		// STEP C: garantované miestnosti na každej ceste
		foreach (var rule in preset.pathCountRules)
		{
			if (rule == null || !IsPlaceableType(rule.nodeType)) continue;
			int totalMax = GetTotalMax(preset, rule.nodeType);

			foreach (var path in allPaths)
			{
				while (CountOnPath(path, rule.nodeType) < rule.minPerPath && CountAll(allNodes, rule.nodeType) < totalMax)
				{
					var candidates = path.FindAll(n =>
						IsFreeCandidate(n, locked, rule.minFloor, rule.maxFloor) &&
						MapRuleValidator.CanPlaceNodeType(n, rule.nodeType, preset.cooldownRules, allNodes) &&
						!WouldExceedMaxOnAnyPath(allPaths, n, rule.nodeType, rule.maxPerPath));

					if (candidates.Count == 0)
					{
						Debug.LogWarning($"[{preset.name}] Na jednej z ciest sa nepodarilo umiestniť {rule.nodeType} (pravidlo 'na každej ceste') - pravidlá sú príliš prísne.");
						break;
					}

					candidates[Random.Range(0, candidates.Count)].type = rule.nodeType;
				}
			}
		}

		// STEP D: počty miestností na celej mape (zvyšok ostane Encounter)
		foreach (var rule in preset.roomCounts)
		{
			if (rule == null || !IsPlaceableType(rule.nodeType)) continue;

			int target = Random.Range(rule.min, rule.max + 1);
			int maxPerPath = GetMaxPerPath(preset, rule.nodeType);
			int current = CountAll(allNodes, rule.nodeType);

			while (current < target)
			{
				var candidates = allNodes.FindAll(n =>
					IsFreeCandidate(n, locked, rule.minFloor, rule.maxFloor) &&
					MapRuleValidator.CanPlaceNodeType(n, rule.nodeType, preset.cooldownRules, allNodes) &&
					!WouldExceedMaxOnAnyPath(allPaths, n, rule.nodeType, maxPerPath));

				if (candidates.Count == 0)
				{
					if (current < rule.min)
						Debug.LogWarning($"[{preset.name}] {rule.nodeType}: umiestnených iba {current} z minimálne {rule.min} - pravidlá (poschodia/rozostupy) sú príliš prísne.");
					break;
				}

				candidates[Random.Range(0, candidates.Count)].type = rule.nodeType;
				current++;
			}
		}
	}

	private static bool IsPlaceableType(NodeType type) => type != NodeType.Encounter && type != NodeType.Boss;

	// Voľný uzol = Encounter, ktorý nie je na pevnom poschodí a je v povolenom rozsahu poschodí
	private static bool IsFreeCandidate(MapNodeData n, HashSet<MapNodeData> locked, int minFloor, int maxFloor)
	{
		return n.type == NodeType.Encounter && !locked.Contains(n) && n.y >= minFloor && n.y <= maxFloor;
	}

	private static int CountAll(List<MapNodeData> nodes, NodeType type) => nodes.FindAll(n => n.type == type).Count;

	private static int CountOnPath(List<MapNodeData> path, NodeType type) => path.FindAll(n => n.type == type).Count;

	private static int GetTotalMax(MapPreset preset, NodeType type)
	{
		var rule = preset.roomCounts.Find(r => r != null && r.nodeType == type);
		return rule != null ? rule.max : int.MaxValue;
	}

	private static int GetMaxPerPath(MapPreset preset, NodeType type)
	{
		var rule = preset.pathCountRules.Find(r => r != null && r.nodeType == type);
		return rule != null ? rule.maxPerPath : int.MaxValue;
	}

	// Či by umiestnenie typu na uzol prekročilo maxPerPath na niektorej ceste, ktorá ním prechádza
	private static bool WouldExceedMaxOnAnyPath(List<List<MapNodeData>> allPaths, MapNodeData targetNode, NodeType type, int maxPerPath)
	{
		foreach (var path in allPaths)
		{
			if (path.Contains(targetNode) && CountOnPath(path, type) >= maxPerPath)
				return true;
		}
		return false;
	}
}
