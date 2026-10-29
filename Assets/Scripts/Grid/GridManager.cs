using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Owner Settings")]
    public bool isPlayerGrid = false; // True = Hráčov grid / Náhľad zbrane, False = Veľký Enemy grid

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

    [Header("Destroyed Max HP Visual Prefab")]
    [Tooltip("Prefab, ktorý sa zobrazí, keď odomknuté Max HP políčko klesne na 0")]
    [SerializeField] private GameObject destroyedHpPrefab;

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

    // --- GENEROVANIE OBYČAJNÉHO GRIDU (PRE HRÁČA ALEBO NÁHĽAD ZBRANE) ---
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

        for (int x = 0; x < totalWidth; x++)
        {
            for (int y = 0; y < totalHeight; y++)
            {
                CreateSlotAt(x, y, centerOffset);
            }
        }

        if (enemies == null || enemies.Count == 0) return;

        int groupWidth = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;
            groupWidth += enemies[i].width;
            if (i < enemies.Count - 1) groupWidth += gapBetweenEnemies;
        }

        int currentStartX = Mathf.Max(0, (boardWidth - groupWidth) / 2);

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
                    }
                }
            }

            currentStartX += enemy.width + gapBetweenEnemies;
        }
    }

    // --- LOGIKA ZÁSAHU ZBRAŇOU (PREVIEW + DAMAGE) ---

    public List<(GridSlot slot, CellSetup weaponCell)> GetOverlappedSlots(GridSlot centerSlot, GridPreset weaponPreset)
    {
        var result = new List<(GridSlot, CellSetup)>();

        int weaponCenterX = weaponPreset.width / 2;
        int weaponCenterY = weaponPreset.height / 2;

        for (int wx = 0; wx < weaponPreset.width; wx++)
        {
            for (int wy = 0; wy < weaponPreset.height; wy++)
            {
                CellSetup weaponCell = weaponPreset.GetCell(wx, wy);
                if (weaponCell.element == BlockElement.None) continue;

                int targetX = centerSlot.x + (wx - weaponCenterX);
                int targetY = centerSlot.y + (wy - weaponCenterY);

                if (targetX >= 0 && targetX < totalWidth && targetY >= 0 && targetY < totalHeight)
                {
                    result.Add((slots[targetX, targetY], weaponCell));
                }
            }
        }
        return result;
    }

    public void ShowWeaponPreview(GridSlot centerSlot, GridPreset weaponPreset)
    {
        ClearPreview();
        if (isPlayerGrid) return;

        var overlapped = GetOverlappedSlots(centerSlot, weaponPreset);
        foreach (var (slot, _) in overlapped)
        {
            // Výrazne červená iba ak je na políčku ŽIVÝ blok (thickness > 0)
            Color previewColor = slot.HasAliveBlock
                ? new Color(1f, 0.25f, 0.25f, 0.9f)
                : new Color(1f, 0.6f, 0.2f, 0.45f);

            slot.SetHighlight(true, previewColor);
        }
    }

    public void ClearPreview()
    {
        if (slots == null) return;
        foreach (var s in slots)
        {
            if (s != null) s.SetHighlight(false, Color.white);
        }
    }

    public bool ApplyWeaponDamage(GridSlot centerSlot, GridPreset weaponPreset)
    {
        if (isPlayerGrid) return false;

        var overlapped = GetOverlappedSlots(centerSlot, weaponPreset);
        if (overlapped.Count == 0) return false;

        foreach (var (slot, weaponCell) in overlapped)
        {
            // Zásah dostanú iba políčka, ktoré ešte nie sú zničené (HasAliveBlock)
            if (slot.HasAliveBlock)
            {
                int elementBonus = GetElementBonus(weaponCell.element, slot.currentBlock.element);
                int finalDamage = Mathf.Max(0, weaponCell.thickness + elementBonus);

                if (finalDamage > 0)
                {
                    slot.currentBlock.TakeDamage(finalDamage);
                }
            }
        }

        ClearPreview();
        return true;
    }

    // --- LOGIKA PRE LIEČENIE POTIONOM A MAX HP ---

    public void ShowPotionPreview(GridSlot centerSlot, GridPreset potionPreset)
    {
        ClearPreview();
        if (!isPlayerGrid) return;

        var overlapped = GetOverlappedSlots(centerSlot, potionPreset);
        foreach (var (slot, _) in overlapped)
        {
            // Zelená ak bolo políčko v Max HP a je zničené (0) alebo poškodené
            Color previewColor = slot.CanBeHealed
                ? new Color(0.2f, 1f, 0.2f, 0.85f)
                : new Color(0.7f, 0.7f, 0.7f, 0.35f);

            slot.SetHighlight(true, previewColor);
        }
    }

    public bool ApplyPotionHeal(GridSlot centerSlot, GridPreset potionPreset)
    {
        if (!isPlayerGrid) return false;

        var overlapped = GetOverlappedSlots(centerSlot, potionPreset);
        bool healedAny = false;

        foreach (var (slot, potionCell) in overlapped)
        {
            // Vylieči len políčka, ktoré patria do Max HP (vrátane zničených so živlom)
            if (slot.CanBeHealed)
            {
                slot.currentBlock.Heal(potionCell.thickness);
                healedAny = true;
            }
        }

        ClearPreview();
        return healedAny;
    }

    // Pridanie nového políčka do Max HP (na predtým prázdny slot) alebo zvýšenie maxThickness
    public bool UnlockOrUpgradePlayerMaxHp(GridSlot targetSlot, BlockElement newElement, int addThickness = 1)
    {
        if (!isPlayerGrid || targetSlot == null) return false;

        if (!targetSlot.IsUnlockedMaxHp)
        {
            CellSetup newCell = new CellSetup
            {
                element = newElement,
                thickness = Mathf.Clamp(addThickness, 1, 3)
            };
            SpawnSlotValueOnSlot(targetSlot, newCell, -1);
            return true;
        }
        else if (targetSlot.currentBlock.maxThickness < 3)
        {
            targetSlot.currentBlock.UpgradeMaxThickness(addThickness);
            return true;
        }

        return false;
    }

    public static int GetElementBonus(BlockElement attacker, BlockElement defender)
    {
        if (attacker == BlockElement.Lava && defender == BlockElement.Ice) return 1;
        if (attacker == BlockElement.Ice && defender == BlockElement.Lava) return -1;

        if (attacker == BlockElement.Ice && defender == BlockElement.Nature) return 1;
        if (attacker == BlockElement.Nature && defender == BlockElement.Ice) return -1;

        if (attacker == BlockElement.Nature && defender == BlockElement.Rock) return 1;
        if (attacker == BlockElement.Rock && defender == BlockElement.Nature) return -1;

        if (attacker == BlockElement.Rock && defender == BlockElement.Lava) return 1;
        if (attacker == BlockElement.Lava && defender == BlockElement.Rock) return -1;

        return 0;
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
        slot.Init(x, y, this);

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
            // Odovzdáme aj destroyedHpPrefab, aby ho SlotValue vedel zobraziť pri klesnutí na 0
            slotValueScript.Setup(cellData.thickness, ownerIndex, destroyedHpPrefab);
            slot.currentBlock = slotValueScript;
        }
    }

    public void ClearGrid()
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