//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;

//public class MapRenderer : MonoBehaviour
//{
//	[Header("UI Containers (from Hierarchy)")]
//	public RectTransform linesContainer;
//	public RectTransform nodesContainer;

//	[Header("Prefabs (from Assets)")]
//	public GameObject nodePrefab;
//	public GameObject linePrefab;

//	[Header("Line Settings")]
//	public float lineThickness = 5f;
//	public Color lineColor = new Color(1f, 1f, 1f, 0.3f);

//	// Spawns all UI lines and buttons on the Canvas
//	public void RenderMap(List<MapNodeData> allNodes, Action<MapNodeData> onNodeClickedCallback)
//	{
//		ClearExistingMap();

//		// 1. Draw connection lines first
//		foreach (var node in allNodes)
//		{
//			foreach (var next in node.nextNodes)
//			{
//				CreateUILine(node.uiPosition, next.uiPosition);
//			}
//		}

//		// 2. Spawn node buttons
//		foreach (var node in allNodes)
//		{
//			GameObject btnObj = Instantiate(nodePrefab, nodesContainer);
//			btnObj.name = $"Node_{node.x}_{node.y}_{node.type}";

//			RectTransform rt = btnObj.GetComponent<RectTransform>();
//			rt.anchoredPosition = node.uiPosition;

//			node.spawnedButton = btnObj.GetComponent<Button>();
//			node.spawnedImage = btnObj.GetComponent<Image>();

//			// Hook up the click event
//			MapNodeData capturedNode = node;
//			node.spawnedButton.onClick.AddListener(() => onNodeClickedCallback?.Invoke(capturedNode));

//			// Initially show all nodes clearly (for the intro camera pan)
//			UpdateNodeVisual(node, isVisible: true, isClickable: false, isCurrentPlayerNode: false);
//		}
//	}

//	// Updates colors, sizes, and button interactability based on Fog of War
//	public void UpdateAllNodeVisuals(
//		List<MapNodeData> allNodes,
//		HashSet<MapNodeData> visibleNodes,
//		HashSet<MapNodeData> clickableNodes,
//		MapNodeData currentPlayerNode)
//	{
//		foreach (var node in allNodes)
//		{
//			bool isVisible = visibleNodes.Contains(node);
//			bool isClickable = clickableNodes.Contains(node);
//			bool isCurrent = (node == currentPlayerNode);

//			UpdateNodeVisual(node, isVisible, isClickable, isCurrent);
//		}
//	}

//	private void UpdateNodeVisual(MapNodeData node, bool isVisible, bool isClickable, bool isCurrentPlayerNode)
//	{
//		if (node.spawnedButton == null || node.spawnedImage == null) return;

//		// Only allow clicking if it's a valid next step
//		node.spawnedButton.interactable = isClickable;

//		// If hidden in Fog of War, make it dark grey, semi-transparent, and smaller
//		if (!isVisible)
//		{
//			node.spawnedImage.color = new Color(0.2f, 0.2f, 0.2f, 0.35f);
//			node.spawnedButton.transform.localScale = Vector3.one * 0.7f;
//			return;
//		}

//		// Set color based on room type
//		node.spawnedImage.color = GetColorForType(node.type);

//		// Keep visible non-clickable nodes bright (prevent Unity's default grey disabled tint)
//		ColorBlock cb = node.spawnedButton.colors;
//		cb.disabledColor = Color.white;
//		node.spawnedButton.colors = cb;

//		// Adjust scale based on state
//		if (isCurrentPlayerNode)
//		{
//			node.spawnedImage.color = Color.white; // Highlight current player position
//			node.spawnedButton.transform.localScale = Vector3.one * 1.25f;
//		}
//		else if (isClickable)
//		{
//			node.spawnedButton.transform.localScale = Vector3.one * 1.15f;
//		}
//		else
//		{
//			node.spawnedButton.transform.localScale = (node.type == NodeType.Boss) ? Vector3.one * 1.8f : Vector3.one * 0.9f;
//		}
//	}

//	private Color GetColorForType(NodeType type)
//	{
//		switch (type)
//		{
//			case NodeType.Encounter: return new Color(0.85f, 0.25f, 0.25f); // Red
//			case NodeType.Shop: return new Color(1f, 0.85f, 0.1f);    // Yellow
//			case NodeType.Treasure: return new Color(0.2f, 0.6f, 1f);     // Blue
//			case NodeType.Mystery: return new Color(0.7f, 0.3f, 0.9f);   // Purple
//			case NodeType.Rest: return new Color(0.2f, 0.8f, 0.3f);   // Green
//			case NodeType.Boss: return new Color(0.15f, 0.15f, 0.15f);// Dark/Black
//			default: return Color.white;
//		}
//	}

//	private void CreateUILine(Vector2 startPos, Vector2 endPos)
//	{
//		GameObject lineObj = Instantiate(linePrefab, linesContainer);
//		RectTransform rt = lineObj.GetComponent<RectTransform>();

//		Vector2 dir = endPos - startPos;
//		float distance = dir.magnitude;
//		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

//		rt.anchoredPosition = startPos + dir * 0.5f;
//		rt.sizeDelta = new Vector2(distance, lineThickness);
//		rt.localRotation = Quaternion.Euler(0, 0, angle);

//		Image img = lineObj.GetComponent<Image>();
//		if (img != null) img.color = lineColor;
//	}

//	private void ClearExistingMap()
//	{
//		foreach (Transform child in linesContainer) Destroy(child.gameObject);
//		foreach (Transform child in nodesContainer) Destroy(child.gameObject);
//	}
//}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapRenderer : MonoBehaviour
{
	[Header("UI Containers (from Hierarchy)")]
	public RectTransform linesContainer;
	public RectTransform nodesContainer;

	[Header("Prefabs (from Assets)")]
	public GameObject nodePrefab;
	public GameObject linePrefab;

	[Header("Node Icons (Drag your 7 sprites here)")]
	public Sprite encounterIcon; // encounter_icon
	public Sprite shopIcon;      // Shop_icon
	public Sprite chestIcon;     // Chest_icon (Treasure)
	public Sprite mysteryIcon;   // Mystery_icon
	public Sprite healIcon;      // Heal_icon (Rest)
	public Sprite bossIcon;      // Boss_icon
	public Sprite blankIcon;     // blank_icon (Fog of War)

	[Header("Line Settings")]
	public float lineThickness = 5f;
	public Color lineColor = new Color(0.35f, 0.28f, 0.18f, 0.7f); // Dark brown to match parchment style

	public void RenderMap(List<MapNodeData> allNodes, Action<MapNodeData> onNodeClickedCallback)
	{
		ClearExistingMap();

		// 1. Draw connection lines
		foreach (var node in allNodes)
		{
			foreach (var next in node.nextNodes)
			{
				CreateUILine(node.uiPosition, next.uiPosition);
			}
		}

		// 2. Spawn node buttons
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

			// Initially show real icons for the intro camera scroll
			UpdateNodeVisual(node, isVisible: true, isClickable: false, isCurrentPlayerNode: false);
		}
	}

	public void UpdateAllNodeVisuals(
		List<MapNodeData> allNodes,
		HashSet<MapNodeData> visibleNodes,
		HashSet<MapNodeData> clickableNodes,
		MapNodeData currentPlayerNode)
	{
		foreach (var node in allNodes)
		{
			bool isVisible = visibleNodes.Contains(node);
			bool isClickable = clickableNodes.Contains(node);
			bool isCurrent = (node == currentPlayerNode);

			UpdateNodeVisual(node, isVisible, isClickable, isCurrent);
		}
	}

	private void UpdateNodeVisual(MapNodeData node, bool isVisible, bool isClickable, bool isCurrentPlayerNode)
	{
		if (node.spawnedButton == null || node.spawnedImage == null) return;

		node.spawnedButton.interactable = isClickable;

		// Keep button disabled tint at full white so Unity doesn't grey out non-clickable icons
		ColorBlock cb = node.spawnedButton.colors;
		cb.disabledColor = Color.white;
		node.spawnedButton.colors = cb;

		// If hidden in Fog of War -> show blank_icon! (Boss stays visible at the top if you want, or also blank)
		if (!isVisible)
		{
			node.spawnedImage.sprite = blankIcon;
			node.spawnedImage.color = Color.white;
			node.spawnedButton.transform.localScale = Vector3.one * 0.85f;
			return;
		}

		// Visible node -> show its actual icon
		node.spawnedImage.sprite = GetSpriteForType(node.type);
		node.spawnedImage.color = Color.white;

		// Scale feedback for player position and clickable nodes
		if (isCurrentPlayerNode)
		{
			node.spawnedButton.transform.localScale = Vector3.one * 1.25f;
		}
		else if (isClickable)
		{
			node.spawnedButton.transform.localScale = Vector3.one * 1.1f;
		}
		else
		{
			node.spawnedButton.transform.localScale = (node.type == NodeType.Boss) ? Vector3.one * 1.6f : Vector3.one * 0.9f;
		}
	}

	private Sprite GetSpriteForType(NodeType type)
	{
		switch (type)
		{
			case NodeType.Encounter: return encounterIcon;
			case NodeType.Shop: return shopIcon;
			case NodeType.Treasure: return chestIcon;
			case NodeType.Mystery: return mysteryIcon;
			case NodeType.Rest: return healIcon;
			case NodeType.Boss: return bossIcon;
			default: return blankIcon;
		}
	}

	private void CreateUILine(Vector2 startPos, Vector2 endPos)
	{
		GameObject lineObj = Instantiate(linePrefab, linesContainer);
		RectTransform rt = lineObj.GetComponent<RectTransform>();

		Vector2 dir = endPos - startPos;
		float distance = dir.magnitude;
		float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

		rt.anchoredPosition = startPos + dir * 0.5f;
		rt.sizeDelta = new Vector2(distance, lineThickness);
		rt.localRotation = Quaternion.Euler(0, 0, angle);

		Image img = lineObj.GetComponent<Image>();
		if (img != null) img.color = lineColor;
	}

	private void ClearExistingMap()
	{
		foreach (Transform child in linesContainer) Destroy(child.gameObject);
		foreach (Transform child in nodesContainer) Destroy(child.gameObject);
	}
}