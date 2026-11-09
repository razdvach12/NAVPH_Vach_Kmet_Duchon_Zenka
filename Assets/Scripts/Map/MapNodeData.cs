using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum NodeType
{
	Encounter,
	Shop,
	Treasure,
	Mystery,
	Rest,
	Boss
}

public class MapNodeData
{
	public int x;
	public int y;
	public NodeType type;
	public Vector2 uiPosition;

	public List<MapNodeData> nextNodes = new List<MapNodeData>();

	public Button spawnedButton;
	public Image spawnedImage;
	public bool isVisited = false;

	public MapNodeData(int x, int y, Vector2 uiPos)
	{
		this.x = x;
		this.y = y;
		this.uiPosition = uiPos;
		this.type = NodeType.Encounter;
	}
}