using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapRenderer : MonoBehaviour
{
	private class SpawnedLine
	{
		public MapNodeData from;
		public MapNodeData to;
		public Image image;
		public RectTransform rect;
	}

	[Header("UI Containers (from Hierarchy)")]
	public RectTransform linesContainer;
	public RectTransform nodesContainer;
	public RectTransform decorationsContainer;

	[Header("Prefabs (from Assets)")]
	public GameObject nodePrefab;
	public GameObject linePrefab;

	[Header("Node Icons")]
	public Sprite encounterIcon;
	public Sprite mediumEncounterIcon;
	public Sprite shopIcon;
	public Sprite chestIcon;
	public Sprite mysteryIcon;
	public Sprite healIcon;
	public Sprite bossIcon;
	public Sprite blankIcon;

	[Header("Node Scale Settings")]
	[Tooltip("Base scale multiplier to compensate for transparent padding (100 / 64 = 1.5625)")]
	public float nodeBaseScale = 1.5625f;

	[Header("UI Shader Glow Settings")]
	[Tooltip("Drag your NodeGlowMat (using UI/SpriteSoftGlow shader) here")]
	public Material glowMaterialTemplate;
	[Range(0f, 15f)]
	[Tooltip("Spread distance of the soft glow around the icon shape")]
	public float glowSize = 6f;
	[Range(0f, 5f)]
	[Tooltip("Brightness/strength of the glow")]
	public float glowIntensity = 2.2f;
	[Tooltip("Glow around the node the player is currently standing on")]
	public Color currentNodeGlowColor = new Color(1f, 0.85f, 0.1f, 1f); // Gold
	[Tooltip("Glow around the nodes the player can currently click")]
	public Color clickableNodeGlowColor = new Color(0.25f, 1f, 0.45f, 1f); // Green
	[Tooltip("Tint color for revealed nodes that are not currently clickable")]
	public Color unclickableTint = new Color(0.8f, 0.8f, 0.8f, 1f);

	[Header("Line Settings")]
	public float lineThickness = 5f;
	public Color lineColor = new Color(0.35f, 0.28f, 0.18f, 0.45f);
	[Tooltip("Color and thickness of lines leading to available nodes for the next step")]
	public Color validNextLineColor = new Color(0.2f, 0.85f, 0.3f, 1f);
	public float validLineThickness = 8f;
	[Tooltip("Color of lines the player has already traversed")]
	public Color visitedLineColor = new Color(1f, 0.85f, 0.2f, 0.9f);

	[Header("Map Decoration Settings")]
	[Tooltip("Drag the sliced sprites from mapa_deco_sprite here")]
	public List<Sprite> decorationSprites = new List<Sprite>();
	[Tooltip("Max number of decorations to fill empty spaces across the map")]
	public int decorationCount = 22;
	[Tooltip("Distance inward from left & right torn edges so sprites don't stick out")]
	public float decoEdgeInset = 90f;
	[Tooltip("Keep clear from the top (e.g. so trees don't overlap Deco_TopMountain)")]
	public float decoTopMargin = 450f;
	[Tooltip("Keep clear from the torn bottom edge")]
	public float decoBottomMargin = 140f;
	[Tooltip("Minimum distance between two decorations")]
	public float decoMinSpacing = 180f;
	[Tooltip("Minimum distance from any map node")]
	public float decoNodeClearance = 135f;
	[Tooltip("Minimum distance from any path line connecting nodes")]
	public float decoLineClearance = 90f;
	public Vector2 decoSizeRange = new Vector2(130f, 200f);
	public Color decoTint = new Color(1f, 1f, 1f, 0.85f);
	public bool allowRandomFlip = true;
	[Tooltip("Drag PaperMaskReadMat here so decorations clip cleanly to all 3 paper parts")]
	public Material decorationMaskMaterial;

	[Header("Fog of War")]
	[Tooltip("Drag the FogOfWar object (with MapFogOfWar script) here")]
	public MapFogOfWar fogOfWar;
	[Tooltip("How far above the Boss node the fog starts during the intro preview")]
	public float fogIntroStartPadding = 520f;

	private List<SpawnedLine> spawnedLines = new List<SpawnedLine>();

	private Material currentGlowMat;
	private Material clickableGlowMat;

	private List<MapNodeData> lastAllNodes;
	private HashSet<MapNodeData> lastVisibleNodes;
	private HashSet<MapNodeData> lastClickableNodes;
	private MapNodeData lastCurrentNode;
	private Coroutine refreshCoroutine;

	private static readonly int GlowColorProp = Shader.PropertyToID("_GlowColor");
	private static readonly int GlowSizeProp = Shader.PropertyToID("_GlowSize");
	private static readonly int GlowIntensityProp = Shader.PropertyToID("_GlowIntensity");

	public void RenderMap(List<MapNodeData> allNodes, Action<MapNodeData> onNodeClickedCallback)
	{
		ClearExistingMap();
		EnsureGlowMaterials();

		// 1. Draw lines (at the very back in linesContainer)
		foreach (var node in allNodes)
		{
			foreach (var next in node.nextNodes)
			{
				CreateUILine(node, next);
			}
		}

		// 2. Spawn the node buttons with icons
		foreach (var node in allNodes)
		{
			GameObject btnObj = Instantiate(nodePrefab, nodesContainer);
			btnObj.name = $"Node_{node.x}_{node.y}_{node.type}";

			RectTransform rt = btnObj.GetComponent<RectTransform>();
			rt.anchoredPosition = node.uiPosition;

			node.spawnedButton = btnObj.GetComponent<Button>();
			node.spawnedImage = btnObj.GetComponent<Image>();

			MapNodeData capturedNode = node;
			node.spawnedButton.onClick.AddListener(() => onNodeClickedCallback?.Invoke(capturedNode));

			UpdateNodeVisual(node, isVisible: true, isClickable: false, isCurrentPlayerNode: false);
		}

		// 3. Spawn random decorations across all open spaces (edges + middle), avoiding nodes & paths
		SpawnDecorations(allNodes);
	}

	private void SpawnDecorations(List<MapNodeData> allNodes)
	{
		if (decorationsContainer == null || decorationSprites == null || decorationSprites.Count == 0)
			return;

		Canvas.ForceUpdateCanvases();

		Rect bounds = decorationsContainer.rect;
		float minX = bounds.xMin + decoEdgeInset;
		float maxX = bounds.xMax - decoEdgeInset;
		float minY = bounds.yMin + decoBottomMargin;
		float maxY = bounds.yMax - decoTopMargin;
		if (minX >= maxX || minY >= maxY) return;

		// Convert all node world positions into decorationsContainer local space
		Dictionary<MapNodeData, Vector2> nodeLocalPos = new Dictionary<MapNodeData, Vector2>(allNodes.Count);
		foreach (var node in allNodes)
		{
			if (node.spawnedButton != null)
			{
				Vector3 localPos = decorationsContainer.InverseTransformPoint(node.spawnedButton.transform.position);
				nodeLocalPos[node] = new Vector2(localPos.x, localPos.y);
			}
		}

		// Precompute all line segments in decorationsContainer local space
		List<(Vector2 a, Vector2 b)> lineSegments = new List<(Vector2, Vector2)>();
		foreach (var node in allNodes)
		{
			if (!nodeLocalPos.TryGetValue(node, out Vector2 fromPos)) continue;
			foreach (var next in node.nextNodes)
			{
				if (nodeLocalPos.TryGetValue(next, out Vector2 toPos))
				{
					lineSegments.Add((fromPos, toPos));
				}
			}
		}

		List<Vector2> placedPositions = new List<Vector2>();
		int maxAttempts = decorationCount * 40;
		int attempts = 0;

		while (placedPositions.Count < decorationCount && attempts < maxAttempts)
		{
			attempts++;

			float x = UnityEngine.Random.Range(minX, maxX);
			float y = UnityEngine.Random.Range(minY, maxY);
			Vector2 candidate = new Vector2(x, y);

			// 1. Check spacing against other decorations
			bool tooClose = false;
			foreach (var placed in placedPositions)
			{
				if (Vector2.Distance(candidate, placed) < decoMinSpacing)
				{
					tooClose = true;
					break;
				}
			}
			if (tooClose) continue;

			// 2. Check clearance against map nodes
			foreach (var nodePos in nodeLocalPos.Values)
			{
				if (Vector2.Distance(candidate, nodePos) < decoNodeClearance)
				{
					tooClose = true;
					break;
				}
			}
			if (tooClose) continue;

			// 3. Check clearance against connecting path lines
			if (decoLineClearance > 0f)
			{
				foreach (var seg in lineSegments)
				{
					if (DistancePointToSegment(candidate, seg.a, seg.b) < decoLineClearance)
					{
						tooClose = true;
						break;
					}
				}
				if (tooClose) continue;
			}

			placedPositions.Add(candidate);

			GameObject decoObj = new GameObject($"Deco_{placedPositions.Count}", typeof(RectTransform), typeof(Image));
			decoObj.transform.SetParent(decorationsContainer, false);

			RectTransform rt = decoObj.GetComponent<RectTransform>();
			rt.anchorMin = new Vector2(0.5f, 0.5f);
			rt.anchorMax = new Vector2(0.5f, 0.5f);
			rt.localPosition = new Vector3(candidate.x, candidate.y, 0f);

			float size = UnityEngine.Random.Range(decoSizeRange.x, decoSizeRange.y);
			rt.sizeDelta = new Vector2(size, size);

			if (allowRandomFlip && UnityEngine.Random.value > 0.5f)
			{
				rt.localScale = new Vector3(-1f, 1f, 1f);
			}

			Image img = decoObj.GetComponent<Image>();
			img.sprite = decorationSprites[UnityEngine.Random.Range(0, decorationSprites.Count)];
			img.preserveAspect = true;
			img.color = decoTint;
			img.raycastTarget = false;

			if (decorationMaskMaterial != null)
				img.material = decorationMaskMaterial;
		}
	}

	private float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float lenSq = ab.sqrMagnitude;
		if (lenSq < 0.0001f) return Vector2.Distance(p, a);

		float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
		Vector2 projection = a + t * ab;
		return Vector2.Distance(p, projection);
	}

	public void UpdateAllNodeVisuals(
		List<MapNodeData> allNodes,
		HashSet<MapNodeData> visibleNodes,
		HashSet<MapNodeData> clickableNodes,
		MapNodeData currentPlayerNode,
		bool updateFog = true,
		bool instantFog = false)
	{
		lastAllNodes = allNodes;
		lastVisibleNodes = visibleNodes;
		lastClickableNodes = clickableNodes;
		lastCurrentNode = currentPlayerNode;

		EnsureGlowMaterials();
		ApplyGlowMaterialProperties();

		// 1. Update nodes
		foreach (var node in allNodes)
		{
			bool isVisible = visibleNodes.Contains(node);
			bool isClickable = clickableNodes.Contains(node);
			bool isCurrent = (node == currentPlayerNode);

			UpdateNodeVisual(node, isVisible, isClickable, isCurrent);
		}

		// 2. Update lines
		foreach (var line in spawnedLines)
		{
			if (line.image == null) continue;

			bool isValidNextStep = (currentPlayerNode != null && line.from == currentPlayerNode && clickableNodes.Contains(line.to));
			bool isAlreadyTraversed = (line.from.isVisited && line.to.isVisited);

			if (isValidNextStep)
			{
				line.image.color = validNextLineColor;
				line.rect.sizeDelta = new Vector2(line.rect.sizeDelta.x, validLineThickness);
			}
			else if (isAlreadyTraversed)
			{
				line.image.color = visitedLineColor;
				line.rect.sizeDelta = new Vector2(line.rect.sizeDelta.x, lineThickness + 1.5f);
			}
			else
			{
				line.image.color = lineColor;
				line.rect.sizeDelta = new Vector2(line.rect.sizeDelta.x, lineThickness);
			}
		}

		// 3. Update Fog of War
		if (updateFog && fogOfWar != null && visibleNodes.Count > 0)
		{
			float maxVisibleY = float.MinValue;
			foreach (var node in visibleNodes)
				if (node.uiPosition.y > maxVisibleY) maxVisibleY = node.uiPosition.y;

			bool allRevealed = (visibleNodes.Count >= allNodes.Count);

			if (instantFog)
				fogOfWar.SetPositionImmediate(maxVisibleY, allRevealed);
			else
				fogOfWar.MoveToNodeY(maxVisibleY, allRevealed);
		}

		// Force UI materials & masked clones to sync cleanly after layout/mask initialization
		if (gameObject.activeInHierarchy)
		{
			if (refreshCoroutine != null) StopCoroutine(refreshCoroutine);
			refreshCoroutine = StartCoroutine(RefreshMaterialsNextFrame());
		}
	}

	private IEnumerator RefreshMaterialsNextFrame()
	{
		yield return null;

		ApplyGlowMaterialProperties();
		SyncRenderedMaterials();

		Canvas.ForceUpdateCanvases();
		refreshCoroutine = null;
	}

	// Live preview in Play Mode when adjusting sliders/colors in Inspector
	private void OnValidate()
	{
		if (!Application.isPlaying) return;

		ApplyGlowMaterialProperties();

		if (lastAllNodes != null && lastVisibleNodes != null && lastClickableNodes != null)
		{
			UpdateAllNodeVisuals(lastAllNodes, lastVisibleNodes, lastClickableNodes, lastCurrentNode, updateFog: false);
		}
	}

	private void EnsureGlowMaterials()
	{
		if (glowMaterialTemplate == null) return;

		if (currentGlowMat == null)
			currentGlowMat = new Material(glowMaterialTemplate);

		if (clickableGlowMat == null)
			clickableGlowMat = new Material(glowMaterialTemplate);

		ApplyGlowMaterialProperties();
	}

	private void ApplyGlowMaterialProperties()
	{
		ApplyGlowToMaterial(currentGlowMat, currentNodeGlowColor);
		ApplyGlowToMaterial(clickableGlowMat, clickableNodeGlowColor);
	}

	private void ApplyGlowToMaterial(Material mat, Color glowColor)
	{
		if (mat == null || !mat.HasProperty(GlowSizeProp)) return;

		mat.SetColor(GlowColorProp, glowColor);
		mat.SetFloat(GlowSizeProp, glowSize);
		mat.SetFloat(GlowIntensityProp, glowIntensity);
	}

	private void SyncRenderedMaterials()
	{
		if (lastAllNodes == null) return;

		foreach (var node in lastAllNodes)
		{
			if (node.spawnedImage == null) continue;

			if (node == lastCurrentNode)
			{
				ApplyGlowToMaterial(node.spawnedImage.materialForRendering, currentNodeGlowColor);
			}
			else if (lastClickableNodes != null && lastClickableNodes.Contains(node))
			{
				ApplyGlowToMaterial(node.spawnedImage.materialForRendering, clickableNodeGlowColor);
			}

			node.spawnedImage.RecalculateMasking();
			node.spawnedImage.SetMaterialDirty();
		}
	}

	private void UpdateNodeVisual(MapNodeData node, bool isVisible, bool isClickable, bool isCurrentPlayerNode)
	{
		if (node.spawnedButton == null || node.spawnedImage == null) return;

		node.spawnedButton.interactable = isClickable;

		ColorBlock cb = node.spawnedButton.colors;
		cb.disabledColor = Color.white;
		node.spawnedButton.colors = cb;

		// Hidden in Fog of War
		if (!isVisible)
		{
			node.spawnedImage.sprite = blankIcon;
			node.spawnedImage.color = new Color(0.75f, 0.75f, 0.75f, 0.6f);
			node.spawnedImage.material = null; // Default UI material (no glow)
			node.spawnedButton.transform.localScale = Vector3.one * (0.85f * nodeBaseScale);
			node.spawnedImage.RecalculateMasking();
			node.spawnedImage.SetMaterialDirty();
			return;
		}

		// Revealed node
		node.spawnedImage.sprite = GetSpriteForType(node.type);

		if (isCurrentPlayerNode)
		{
			node.spawnedImage.color = Color.white;
			node.spawnedImage.material = currentGlowMat;
			node.spawnedButton.transform.localScale = Vector3.one * (1.25f * nodeBaseScale);
			ApplyGlowToMaterial(node.spawnedImage.materialForRendering, currentNodeGlowColor);
		}
		else if (isClickable)
		{
			node.spawnedImage.color = Color.white;
			node.spawnedImage.material = clickableGlowMat;
			node.spawnedButton.transform.localScale = Vector3.one * (1.15f * nodeBaseScale);
			ApplyGlowToMaterial(node.spawnedImage.materialForRendering, clickableNodeGlowColor);
		}
		else
		{
			node.spawnedImage.color = node.isVisited ? new Color(0.55f, 0.55f, 0.55f, 1f) : unclickableTint;
			node.spawnedImage.material = null; // Default UI material (no glow)
			float stateScale = (node.type == NodeType.Boss) ? 1.6f : 0.9f;
			node.spawnedButton.transform.localScale = Vector3.one * (stateScale * nodeBaseScale);
		}

		node.spawnedImage.RecalculateMasking();
		node.spawnedImage.SetMaterialDirty();
	}

	private Sprite GetSpriteForType(NodeType type)
	{
		switch (type)
		{
			case NodeType.Encounter: return encounterIcon;
			case NodeType.MediumEncounter: return mediumEncounterIcon != null ? mediumEncounterIcon : encounterIcon;
			case NodeType.Shop: return shopIcon;
			case NodeType.Treasure: return chestIcon;
			case NodeType.Mystery: return mysteryIcon;
			case NodeType.Rest: return healIcon;
			case NodeType.Boss: return bossIcon;
			default: return blankIcon;
		}
	}

	private void CreateUILine(MapNodeData fromNode, MapNodeData toNode)
	{
		GameObject lineObj = Instantiate(linePrefab, linesContainer);
		RectTransform rt = lineObj.GetComponent<RectTransform>();

		Vector2 startPos = fromNode.uiPosition;
		Vector2 endPos = toNode.uiPosition;

		Vector2 dir = endPos - startPos;
		float distance = dir.magnitude;
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

		rt.anchoredPosition = startPos + dir * 0.5f;
		rt.sizeDelta = new Vector2(distance, lineThickness);
		rt.localRotation = Quaternion.Euler(0, 0, angle);

		Image img = lineObj.GetComponent<Image>();
		if (img != null) img.color = lineColor;

		spawnedLines.Add(new SpawnedLine
		{
			from = fromNode,
			to = toNode,
			image = img,
			rect = rt
		});
	}

	private void ClearExistingMap()
	{
		if (refreshCoroutine != null)
		{
			StopCoroutine(refreshCoroutine);
			refreshCoroutine = null;
		}

		spawnedLines.Clear();
		lastAllNodes = null;
		lastVisibleNodes = null;
		lastClickableNodes = null;
		lastCurrentNode = null;
		foreach (Transform child in linesContainer) Destroy(child.gameObject);
		foreach (Transform child in nodesContainer) Destroy(child.gameObject);
		if (decorationsContainer != null)
		{
			foreach (Transform child in decorationsContainer) Destroy(child.gameObject);
		}
	}

	private void OnDestroy()
	{
		if (currentGlowMat != null) Destroy(currentGlowMat);
		if (clickableGlowMat != null) Destroy(clickableGlowMat);
	}
}