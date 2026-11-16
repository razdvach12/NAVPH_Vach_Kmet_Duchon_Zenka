using System.Collections.Generic;
using UnityEngine;

// Stav mapy počas jednej hry - statický, aby prežil prechody medzi scénami.
// Mapa sa vygeneruje pri odchode z výberu postavy a ostane rovnaká, kým sa hra neukončí / nezačne nová.
public static class MapRunState
{
	public static MapPreset Preset { get; private set; }
	public static List<MapNodeData> Nodes { get; private set; }
	public static MapNodeData BossNode { get; private set; }

	// Uzol, na ktorom hráč práve stojí (null = ešte si nevybral prvý uzol)
	public static MapNodeData CurrentNode;

	// Intro scroll (od bossa dole) sa pustí iba raz za hru
	public static bool IntroPlayed;

	public static bool HasActiveMap => Nodes != null && Nodes.Count > 0;

	public static void StartNewRun(MapPreset preset)
	{
		Clear();

		if (preset == null)
		{
			Debug.LogError("MapRunState: nie je priradený MapPreset - mapa sa nevygenerovala.");
			return;
		}

		GeneratedMap map = MapGenerator.Generate(preset);
		Preset = preset;
		Nodes = map.nodes;
		BossNode = map.bossNode;
	}

	public static void Clear()
	{
		Preset = null;
		Nodes = null;
		BossNode = null;
		CurrentNode = null;
		IntroPlayed = false;
	}

	// Vyčistí stav aj keď je v editore vypnutý Domain Reload (Enter Play Mode Options)
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetOnPlay() => Clear();
}
