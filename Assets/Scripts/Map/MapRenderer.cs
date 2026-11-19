
using System;
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

	[Header("Soft Radial Glow Settings")]
	[Tooltip("Glow size in pixels (larger than the icon to surround it nicely)")]
	public float glowSize = 140f;
	[Tooltip("Glow beneath the node the player is currently standing on")]
	public Color currentNodeGlowColor = new Color(1f, 0.85f, 0.1f, 0.85f); // Gold
	[Tooltip("Glow beneath the nodes the player can currently click")]
	public Color clickableNodeGlowColor = new Color(0.25f, 1f, 0.45f, 0.8f); // Green
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

	private List<SpawnedLine> spawnedLines = new List<SpawnedLine>();
	private Dictionary<MapNodeData, Image> nodeGlows = new Dictionary<MapNodeData, Image>();
	private Sprite cachedGlowSprite;

	public void RenderMap(List<MapNodeData> allNodes, Action<MapNodeData> onNodeClickedCallback)
	{
		ClearExistingMap();

		if (cachedGlowSprite == null)
			cachedGlowSprite = CreateSoftGlowSprite(64);

		// 1. Draw lines (at the very back in linesContainer)
		foreach (var node in allNodes)
		{
			foreach (var next in node.nextNodes)
			{
				CreateUILine(node, next);
			}
		}

		// 2. Spawn Glow objects FIRST in nodesContainer (so they render right UNDER all icons)
		foreach (var node in allNodes)
		{
			GameObject glowObj = new GameObject($"Glow_{node.x}_{node.y}", typeof(RectTransform), typeof(Image));
			glowObj.transform.SetParent(nodesContainer, false);

			RectTransform glowRt = glowObj.GetComponent<RectTransform>();
			glowRt.anchoredPosition = node.uiPosition;
			glowRt.sizeDelta = new Vector2(glowSize, glowSize);

			Image glowImg = glowObj.GetComponent<Image>();
			glowImg.sprite = cachedGlowSprite;
			glowImg.raycastTarget = false; // Do not block node clicks
			glowImg.enabled = false;

			nodeGlows[node] = glowImg;
		}

		// 3. Spawn the actual node buttons with icons (ON TOP of the glow)
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
	}

	public void UpdateAllNodeVisuals(
		List<MapNodeData> allNodes,
		HashSet<MapNodeData> visibleNodes,
		HashSet<MapNodeData> clickableNodes,
		MapNodeData currentPlayerNode)
	{
		// 1. Update nodes and their glow
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
	}

	private void UpdateNodeVisual(MapNodeData node, bool isVisible, bool isClickable, bool isCurrentPlayerNode)
	{
		if (node.spawnedButton == null || node.spawnedImage == null) return;

		node.spawnedButton.interactable = isClickable;

		ColorBlock cb = node.spawnedButton.colors;
		cb.disabledColor = Color.white;
		node.spawnedButton.colors = cb;

		nodeGlows.TryGetValue(node, out Image glowImg);

		// Hidden in Fog of War
		if (!isVisible)
		{
			node.spawnedImage.sprite = blankIcon;
			node.spawnedImage.color = new Color(0.75f, 0.75f, 0.75f, 0.6f);
			node.spawnedButton.transform.localScale = Vector3.one * 0.85f;
			if (glowImg != null) glowImg.enabled = false;
			return;
		}

		// Revealed node
		node.spawnedImage.sprite = GetSpriteForType(node.type);

		if (isCurrentPlayerNode)
		{
			node.spawnedImage.color = Color.white;
			node.spawnedButton.transform.localScale = Vector3.one * 1.25f;
			if (glowImg != null)
			{
				glowImg.enabled = true;
				glowImg.color = currentNodeGlowColor;
				glowImg.rectTransform.sizeDelta = new Vector2(glowSize * 1.15f, glowSize * 1.15f);
			}
		}
		else if (isClickable)
		{
			node.spawnedImage.color = Color.white;
			node.spawnedButton.transform.localScale = Vector3.one * 1.15f;
			if (glowImg != null)
			{
				glowImg.enabled = true;
				glowImg.color = clickableNodeGlowColor;
				glowImg.rectTransform.sizeDelta = new Vector2(glowSize, glowSize);
			}
		}
		else
		{
			node.spawnedImage.color = node.isVisited ? new Color(0.55f, 0.55f, 0.55f, 0.8f) : unclickableTint;
			node.spawnedButton.transform.localScale = (node.type == NodeType.Boss) ? Vector3.one * 1.6f : Vector3.one * 0.9f;
			if (glowImg != null) glowImg.enabled = false;
		}
	}

	// Generates a smooth circular texture with a soft fade to transparent
	private Sprite CreateSoftGlowSprite(int size)
	{
		Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
		tex.filterMode = FilterMode.Bilinear;
		tex.wrapMode = TextureWrapMode.Clamp;

		float center = (size - 1) * 0.5f;
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
				// Quadratic falloff (Mathf.Pow) creates a smooth soft glow without harsh edges
				float alpha = Mathf.Pow(Mathf.Clamp01(1f - dist), 2f);
				tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
			}
		}
		tex.Apply();

		return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
		spawnedLines.Clear();
		nodeGlows.Clear();
		foreach (Transform child in linesContainer) Destroy(child.gameObject);
		foreach (Transform child in nodesContainer) Destroy(child.gameObject);
	}
}