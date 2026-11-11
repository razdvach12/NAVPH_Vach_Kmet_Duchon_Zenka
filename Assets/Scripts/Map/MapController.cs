using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
	[Header("References")]
	public MapGenerator mapGenerator;
	public MapRenderer mapRenderer;
	public RectTransform mapContent; // The scrollable panel containing the map

	[Header("Visibility & Scroll Settings")]
	[Tooltip("How many steps ahead the player can see (2 or 3)")]
	public int visionRange = 3;
	[Tooltip("Duration of the intro scroll from top (Boss) to bottom (Start)")]
	public float introPanDuration = 3.0f;
	[Tooltip("Vertical offset to keep the current node nicely centered on screen")]
	public float verticalScreenOffset = -250f;

	private List<MapNodeData> allNodes;
	private MapNodeData currentPlayerNode = null;
	private bool canPlayerClick = false;

	void Start()
	{
		StartNewMapLayer();
	}

	public void StartNewMapLayer()
	{
		currentPlayerNode = null;

		// 1. Generate data with rules
		allNodes = mapGenerator.GenerateNewMap();

		// 2. Render buttons and lines in UI (initially all visible for the intro scroll)
		mapRenderer.RenderMap(allNodes, OnNodeClicked);

		// 3. Start the intro camera/panel scroll from Boss down to the start
		StopAllCoroutines();
		StartCoroutine(IntroScrollRoutine());
	}

	private IEnumerator IntroScrollRoutine()
	{
		canPlayerClick = false;

		// Start viewing the Boss at the top (move MapContent down)
		Vector2 startScrollPos = new Vector2(0, -mapGenerator.bossNode.uiPosition.y + 150f);
		// End viewing the 0th floor at the bottom
		Vector2 endScrollPos = new Vector2(0, verticalScreenOffset);

		mapContent.anchoredPosition = startScrollPos;
		yield return new WaitForSeconds(1.0f); // Pause briefly on the Boss

		float elapsed = 0f;
		while (elapsed < introPanDuration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.SmoothStep(0f, 1f, elapsed / introPanDuration);
			mapContent.anchoredPosition = Vector2.Lerp(startScrollPos, endScrollPos, t);
			yield return null;
		}

		// Once at the bottom, enable Fog of War (2-3 blocks) and allow clicking
		UpdateFogOfWar();
		canPlayerClick = true;
	}

	private void OnNodeClicked(MapNodeData clickedNode)
	{
		if (!canPlayerClick) return;

		// First move: must click a node on the 0th floor
		if (currentPlayerNode == null)
		{
			if (clickedNode.y == 0)
				MovePlayerToNode(clickedNode);
		}
		// Subsequent moves: must click a connected node on the next floor
		else
		{
			if (currentPlayerNode.nextNodes.Contains(clickedNode))
				MovePlayerToNode(clickedNode);
		}
	}

	private void MovePlayerToNode(MapNodeData targetNode)
	{
		currentPlayerNode = targetNode;
		currentPlayerNode.isVisited = true;

		Debug.Log($"Player entered node: {targetNode.type} at Floor {targetNode.y}");

		// Smoothly scroll the map so the player stays in view
		StopAllCoroutines();
		Vector2 targetScrollPos = new Vector2(0, -targetNode.uiPosition.y + verticalScreenOffset);
		StartCoroutine(SmoothScrollMap(targetScrollPos));

		// Recalculate visibility (2-3 steps ahead)
		UpdateFogOfWar();
	}

	private IEnumerator SmoothScrollMap(Vector2 targetAnchoredPos)
	{
		Vector2 startPos = mapContent.anchoredPosition;
		float elapsed = 0f;
		while (elapsed < 0.4f)
		{
			elapsed += Time.deltaTime;
			mapContent.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, elapsed / 0.4f);
			yield return null;
		}
	}

	private void UpdateFogOfWar()
	{
		HashSet<MapNodeData> visibleNodes = new HashSet<MapNodeData>();
		HashSet<MapNodeData> clickableNodes = new HashSet<MapNodeData>();

		if (currentPlayerNode == null)
		{
			// Before picking the first node, show the bottom 'visionRange' floors
			foreach (var node in allNodes)
			{
				if (node.y < visionRange) visibleNodes.Add(node);
				if (node.y == 0) clickableNodes.Add(node);
			}
		}
		else
		{
			// Keep already visited nodes visible
			foreach (var node in allNodes)
			{
				if (node.isVisited) visibleNodes.Add(node);
			}

			// Reveal connected nodes up to 'visionRange' steps ahead
			RevealNodesRecursive(currentPlayerNode, visionRange, visibleNodes);

			// Only direct next nodes are clickable
			foreach (var next in currentPlayerNode.nextNodes)
			{
				clickableNodes.Add(next);
			}
		}

		mapRenderer.UpdateAllNodeVisuals(allNodes, visibleNodes, clickableNodes, currentPlayerNode);
	}

	private void RevealNodesRecursive(MapNodeData fromNode, int stepsLeft, HashSet<MapNodeData> visibleSet)
	{
		visibleSet.Add(fromNode);
		if (stepsLeft <= 0) return;

		foreach (var next in fromNode.nextNodes)
		{
			RevealNodesRecursive(next, stepsLeft - 1, visibleSet);
		}
	}
}