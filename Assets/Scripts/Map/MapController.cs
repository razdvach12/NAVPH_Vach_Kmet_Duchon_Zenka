using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
	[Header("References")]
	[Tooltip("Použije sa iba ak mapa ešte nebola vygenerovaná (napr. MapScene spustená priamo v editore). Normálne sa mapa generuje pri odchode z výberu postavy.")]
	public MapPreset fallbackPreset;
	public MapRenderer mapRenderer;
	public RectTransform mapContent; // The scrollable panel containing the map

	[Header("Visibility & Scroll Settings")]
	[Tooltip("How many steps ahead the player can see (2 or 3)")]
	public int visionRange = 3;
	[Tooltip("Duration of the intro scroll from top (Boss) to bottom (Start)")]
	public float introPanDuration = 3.0f;
	[Tooltip("Vertical offset to keep the current node nicely centered on screen")]
	public float verticalScreenOffset = -250f;
	[Tooltip("Ako dlho sa čaká po kliku na uzol (posun mapy), kým sa začne načítavať scéna")]
	public float enterNodeDelay = 0.5f;

	private bool canPlayerClick = false;

	private List<MapNodeData> AllNodes => MapRunState.Nodes;

	void Start()
	{
		// Ak hudba mapy už hrá (napr. po výhre v encounteri), pokračuje bez prerušenia
		if (MusicManager.Instance != null) MusicManager.Instance.PlayMusic("MapBackground");

		if (!MapRunState.HasActiveMap)
		{
			Debug.LogWarning("MapController: mapa ešte nebola vygenerovaná (scéna spustená priamo?) - generujem z Fallback Preset.");
			MapRunState.StartNewRun(fallbackPreset);
			if (!MapRunState.HasActiveMap) return;
		}

		mapRenderer.RenderMap(AllNodes, OnNodeClicked);

		// Hráč sa vrátil po porazení bossa - hra je dohraná
		if (MapRunState.CurrentNode != null && MapRunState.CurrentNode.type == NodeType.Boss)
		{
			StartCoroutine(FinishRunRoutine());
			return;
		}

		if (!MapRunState.IntroPlayed)
		{
			StartCoroutine(IntroScrollRoutine());
		}
		else
		{
			// Návrat zo scény - mapa ostáva rovnaká, iba sa nastaví na pozíciu hráča
			mapContent.anchoredPosition = GetScrollPositionFor(MapRunState.CurrentNode);
			UpdateFogOfWar();
			canPlayerClick = true;
		}
	}

	private Vector2 GetScrollPositionFor(MapNodeData node)
	{
		if (node == null) return new Vector2(0, verticalScreenOffset);
		return new Vector2(0, -node.uiPosition.y + verticalScreenOffset);
	}

	private IEnumerator IntroScrollRoutine()
	{
		canPlayerClick = false;

		// Start viewing the Boss at the top (move MapContent down)
		Vector2 startScrollPos = new Vector2(0, -MapRunState.BossNode.uiPosition.y + 150f);
		// End viewing the 0th floor at the bottom
		Vector2 endScrollPos = GetScrollPositionFor(null);

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
		MapRunState.IntroPlayed = true;
		UpdateFogOfWar();
		canPlayerClick = true;
	}

	private void OnNodeClicked(MapNodeData clickedNode)
	{
		if (!canPlayerClick) return;

		MapNodeData current = MapRunState.CurrentNode;

		// First move: must click a node on the 0th floor
		if (current == null)
		{
			if (clickedNode.y == 0)
				MovePlayerToNode(clickedNode);
		}
		// Subsequent moves: must click a connected node on the next floor
		else
		{
			if (current.nextNodes.Contains(clickedNode))
				MovePlayerToNode(clickedNode);
		}
	}

	private void MovePlayerToNode(MapNodeData targetNode)
	{
		canPlayerClick = false; // Ochrana proti dvojkliku počas posunu / loadingu

		MapRunState.CurrentNode = targetNode;
		targetNode.isVisited = true;

		Debug.Log($"Player entered node: {targetNode.type} at Floor {targetNode.y}");

		// Recalculate visibility (2-3 steps ahead)
		UpdateFogOfWar();

		// Smoothly scroll the map so the player stays in view, then load the room
		StopAllCoroutines();
		StartCoroutine(EnterNodeRoutine(targetNode));
	}

	private IEnumerator EnterNodeRoutine(MapNodeData node)
	{
		yield return SmoothScrollMap(GetScrollPositionFor(node));

		float remaining = enterNodeDelay - 0.4f;
		if (remaining > 0f) yield return new WaitForSeconds(remaining);

		LoadSceneForNode(node);
	}

	// Typ uzla -> scéna
	private void LoadSceneForNode(MapNodeData node)
	{
		LoadSceneForType(node.type, node);
	}

	// Typy miestností, na ktoré môže "?" náhodne preskočiť (bez Shopu a Bossa)
	private static readonly NodeType[] MysteryOutcomes =
	{
		NodeType.Encounter,
		NodeType.MediumEncounter,
		NodeType.Treasure,
		NodeType.Rest
	};

	private void LoadSceneForType(NodeType type, MapNodeData node)
	{
		MapPreset preset = MapRunState.Preset;

		switch (type)
		{
			case NodeType.Encounter:
				EncounterSelection.NextRoom = null;
				EncounterSelection.NextDifficulty = preset != null ? preset.GetEncounterDifficulty(node.y) : RoomDifficulty.Easy;
				LoadingScene.Load(SceneIds.Encounter);
				break;

			case NodeType.MediumEncounter:
				EncounterSelection.NextRoom = null;
				EncounterSelection.NextDifficulty = RoomDifficulty.Medium;
				LoadingScene.Load(SceneIds.Encounter);
				break;

			case NodeType.Boss:
				if (preset != null && preset.bossRoom != null)
					EncounterSelection.NextRoom = preset.bossRoom;
				else
					EncounterSelection.NextDifficulty = RoomDifficulty.Boss;
				LoadingScene.Load(SceneIds.Encounter);
				break;

			case NodeType.Shop:
				LoadingScene.Load(SceneIds.Shop);
				break;

			case NodeType.Treasure:
				LoadingScene.Load(SceneIds.Chest);
				break;

			case NodeType.Rest:
				LoadingScene.Load(SceneIds.Meadow);
				break;

			// "?" - náhodne jedna z miestností v MysteryOutcomes (rovnaká šanca pre každú)
			case NodeType.Mystery:
				NodeType outcome = MysteryOutcomes[Random.Range(0, MysteryOutcomes.Length)];
				Debug.Log($"Mystery -> {outcome}");
				LoadSceneForType(outcome, node);
				break;
		}
	}

	// Boss porazený - zatiaľ nie je záverečná scéna v Build Settings, tak sa hra ukončí návratom do menu
	private IEnumerator FinishRunRoutine()
	{
		canPlayerClick = false;
		mapContent.anchoredPosition = GetScrollPositionFor(MapRunState.CurrentNode);
		UpdateFogOfWar();

		// Počkáme, kým LoadingScene dokončí aktuálne načítanie (inak by nové volanie ignoroval)
		yield return new WaitWhile(() => LoadingScene.IsBusy);
		yield return new WaitForSeconds(1.5f);

		Debug.Log("Boss porazený - koniec hry, návrat do menu.");
		MapRunState.Clear();
		LoadingScene.Load(SceneIds.Menu);
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
		mapContent.anchoredPosition = targetAnchoredPos;
	}

	private void UpdateFogOfWar()
	{
		var visibleNodes = new HashSet<MapNodeData>();
		var clickableNodes = new HashSet<MapNodeData>();
		MapNodeData current = MapRunState.CurrentNode;

		// Určíme, po ktoré poschodie (vrátane) hráč momentálne dovidí
		// Ak ešte nevybral prvý uzol, vidí od 0 po (visionRange - 1), napr. prvé 3 poschodia (0, 1, 2)
		// Ak stojí na poschodí Y, vidí všetko až po poschodie (Y + visionRange)
		int maxVisibleFloor = (current == null) ? (visionRange - 1) : (current.y + visionRange);

		foreach (var node in AllNodes)
		{
			// Všetko do výšky maxVisibleFloor sa natrvalo odhalí
			if (node.y <= maxVisibleFloor || node.isVisited)
			{
				node.isRevealed = true;
			}

			// Čo bolo raz odhalené, ostáva navždy viditeľné
			if (node.isRevealed)
			{
				visibleNodes.Add(node);
			}
		}

		// Klikateľné uzly: buď 0. poschodie na štarte, alebo priami susedia z aktuálneho uzla
		if (current == null)
		{
			foreach (var node in AllNodes)
			{
				if (node.y == 0) clickableNodes.Add(node);
			}
		}
		else
		{
			foreach (var next in current.nextNodes)
			{
				clickableNodes.Add(next);
			}
		}

		mapRenderer.UpdateAllNodeVisuals(AllNodes, visibleNodes, clickableNodes, current);
	}
}
