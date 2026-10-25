using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Owner Settings")]
    public bool isPlayerGrid = false; // True = Hráčov grid vľavo, False = Veľký Enemy grid vpravo

    [Header("Base Grid Setup")]
    [SerializeField] private GameObject gridSlotPrefab;
    [SerializeField] private float spacing = 1.1f;

    [Header("Player Mode (Single Preset)")]
    [SerializeField] private GridPreset singlePreset;

    [Header("Enemy Mode (Fixed Board + Multiple Presets)")]
    [SerializeField] private int boardWidth = 20;
    [SerializeField] private int boardHeight = 8;
    [SerializeField] private List<GridPreset> enemyPresets = new List<GridPreset>();
    [SerializeField] private int gapBetweenEnemies = 1;

    [Header("SlotValue Prefab Variants")]
    [SerializeField] private GameObject basicHpPrefab;
    [SerializeField] private GameObject lavaHpPrefab;
    [SerializeField] private GameObject iceHpPrefab;
    [SerializeField] private GameObject rockHpPrefab;
    [SerializeField] private GameObject natureHpPrefab;

    private GridSlot[,] slots;
    private int totalWidth;
    private int totalHeight;

    private void Start()
    {
        GenerateGrid();
    }

    [ContextMenu("Generate Grid Now")]
    public void GenerateGrid()
    {
        if (isPlayerGrid && singlePreset != null)
        {
            GenerateSingleGrid(singlePreset);
        }
        else if (!isPlayerGrid)
        {
            GenerateCombinedEnemyGrid(enemyPresets);
        }
    }

    // --- GENEROVANIE OBYČAJNÉHO GRIDU (PRE HRÁČA) ---
    public void GenerateSingleGrid(GridPreset preset)
    {
        ClearGrid();
        totalWidth = preset.width;
        totalHeight = preset.height;
        slots = new GridSlot[totalWidth, totalHeight];

        Vector3 centerOffset = new Vector3((totalWidth - 1) * spacing * 0.5f, (totalHeight - 1) * spacing * 0.5f, 0);

        for (int x = 0; x < totalWidth; x++)
        {
            for (int y = 0; y < totalHeight; y++)
            {
                GridSlot slot = CreateSlotAt(x, y, centerOffset);
                CellSetup cellData = preset.GetCell(x, y);
                if (cellData.element != BlockElement.None)
                {
                    SpawnSlotValueOnSlot(slot, cellData, -1);
                }
            }
        }
    }

    // --- GENEROVANIE FIXNÉHO VEĽKÉHO GRIDU PRE VIACERÝCH NEPRIATEĽOV ---
    public void GenerateCombinedEnemyGrid(List<GridPreset> enemies)
    {
        ClearGrid();

        totalWidth = boardWidth;
        totalHeight = boardHeight;
        slots = new GridSlot[totalWidth, totalHeight];

        Vector3 centerOffset = new Vector3((totalWidth - 1) * spacing * 0.5f, (totalHeight - 1) * spacing * 0.5f, 0);

        // 1. Vytvoríme celú fixnú plochu (vždy viditeľná)
        for (int x = 0; x < totalWidth; x++)
        {
            for (int y = 0; y < totalHeight; y++)
            {
                CreateSlotAt(x, y, centerOffset);
            }
        }

        if (enemies == null || enemies.Count == 0) return;

        // 2. Spočítame šírku celej skupiny nepriateľov vrátane medzier
        int groupWidth = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;
            groupWidth += enemies[i].width;
            if (i < enemies.Count - 1) groupWidth += gapBetweenEnemies;
        }

        // 3. Vycentrujeme skupinu horizontálne do stredu fixnej plochy
        int currentStartX = Mathf.Max(0, (boardWidth - groupWidth) / 2);

        // 4. Uložíme jednotlivých nepriateľov na fixnú plochu
        for (int enemyIndex = 0; enemyIndex < enemies.Count; enemyIndex++)
        {
            GridPreset enemy = enemies[enemyIndex];
            if (enemy == null) continue;

            int startY = Mathf.Max(0, (boardHeight - enemy.height) / 2);

            for (int ex = 0; ex < enemy.width; ex++)
            {
                for (int ey = 0; ey < enemy.height; ey++)
                {
                    CellSetup cellData = enemy.GetCell(ex, ey);
                    if (cellData.element != BlockElement.None)
                    {
                        int targetX = currentStartX + ex;
                        int targetY = startY + ey;

                        if (targetX >= 0 && targetX < boardWidth && targetY >= 0 && targetY < boardHeight)
                        {
                            SpawnSlotValueOnSlot(slots[targetX, targetY], cellData, enemyIndex);
                        }
                        else
                        {
                            Debug.LogWarning($"Enemy {enemy.name} exceeds fixed board size ({boardWidth}x{boardHeight})!");
                        }
                    }
                }
            }

            currentStartX += enemy.width + gapBetweenEnemies;
        }
    }

    // --- POMOCNÉ METÓDY ---
    private GridSlot CreateSlotAt(int x, int y, Vector3 centerOffset)
    {
        Vector3 localPos = new Vector3(x * spacing, (totalHeight - 1 - y) * spacing, 0) - centerOffset;
        var spawnedTile = Instantiate(gridSlotPrefab, transform);
        spawnedTile.transform.localPosition = localPos;
        spawnedTile.name = $"Tile_{x}_{y}";

        GridSlot slot = spawnedTile.GetComponent<GridSlot>();
        if (slot == null) slot = spawnedTile.AddComponent<GridSlot>();
        slot.Init(x, y);

        slots[x, y] = slot;
        return slot;
    }

    private void SpawnSlotValueOnSlot(GridSlot slot, CellSetup cellData, int ownerIndex)
    {
        GameObject prefabToSpawn = GetVariantPrefab(cellData.element);
        if (prefabToSpawn == null) return;

        var hpBlock = Instantiate(prefabToSpawn, slot.transform);
        hpBlock.transform.localPosition = new Vector3(0, 0, -0.1f);
        hpBlock.name = $"HP_{cellData.element}_{slot.x}_{slot.y}";

        var slotValueScript = hpBlock.GetComponent<SlotValue>();
        if (slotValueScript != null)
        {
            slotValueScript.Setup(cellData.thickness, ownerIndex);
            slot.currentBlock = slotValueScript;
        }
    }

    private void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    private GameObject GetVariantPrefab(BlockElement element)
    {
        return element switch
        {
            BlockElement.Basic => basicHpPrefab,
            BlockElement.Lava => lavaHpPrefab,
            BlockElement.Ice => iceHpPrefab,
            BlockElement.Rock => rockHpPrefab,
            BlockElement.Nature => natureHpPrefab,
            _ => null
        };
    }
}