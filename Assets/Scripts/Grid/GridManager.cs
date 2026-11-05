using System.Collections;
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

    [Header("Enemy Mode (Fixed Board + Room)")]
    [SerializeField] private int boardWidth = 20;
    [SerializeField] private int boardHeight = 8;

    // Nepriatelia vybraní pre room, ktorých nastaví TurnManager pred generovaním gridu
    private List<PlacedEnemy> roomEnemies;
    // Nepriatelia aktuálnej room - index v zozname = ownerEnemyIndex na políčkach
    private readonly List<EnemyData> enemies = new List<EnemyData>();
    // Oblasť gridu, ktorú zaberá každý nepriateľ (pre otáčanie)
    private readonly List<RectInt> enemyAreas = new List<RectInt>();

    public IReadOnlyList<EnemyData> Enemies => enemies;

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
            GenerateRoomGrid(roomEnemies);
        }
    }

    // Umožní nastaviť grid hráča (napr. z PlayerStats) ešte pred generovaním v Start()
    public void SetSinglePreset(GridPreset preset)
    {
        singlePreset = preset;
    }

    // Nastaví nepriateľov z room ešte pred generovaním v Start()
    public void SetRoomEnemies(List<PlacedEnemy> placedEnemies)
    {
        roomEnemies = placedEnemies;
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

    // --- GENEROVANIE FIXNÉHO VEĽKÉHO GRIDU PRE NEPRIATEĽOV Z ROOM ---
    public void GenerateRoomGrid(List<PlacedEnemy> placedEnemies)
    {
        ClearGrid();
        enemies.Clear();
        enemyAreas.Clear();

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

        if (placedEnemies == null) return;

        for (int enemyIndex = 0; enemyIndex < placedEnemies.Count; enemyIndex++)
        {
            PlacedEnemy placed = placedEnemies[enemyIndex];

            // Pridáme aj prázdny záznam, aby index sedel s ownerEnemyIndex
            enemies.Add(placed?.enemy);
            if (placed == null || placed.enemy == null || placed.enemy.bodyPreset == null)
            {
                enemyAreas.Add(default);
                continue;
            }

            GridPreset body = placed.enemy.bodyPreset;
            Vector2Int start = placed.position;
            enemyAreas.Add(new RectInt(start.x, start.y, body.width, body.height));

            if (start.x < 0 || start.y < 0 || start.x + body.width > boardWidth || start.y + body.height > boardHeight)
            {
                Debug.LogWarning($"{placed.enemy.enemyName} na pozícii {start} presahuje grid {boardWidth}x{boardHeight}.");
            }

            for (int ex = 0; ex < body.width; ex++)
            {
                for (int ey = 0; ey < body.height; ey++)
                {
                    CellSetup cellData = body.GetCell(ex, ey);
                    if (cellData.element == BlockElement.None) continue;

                    int targetX = start.x + ex;
                    int targetY = start.y + ey;
                    if (targetX < 0 || targetX >= boardWidth || targetY < 0 || targetY >= boardHeight) continue;

                    if (slots[targetX, targetY].currentBlock != null)
                    {
                        Debug.LogWarning($"{placed.enemy.enemyName} sa prekrýva s iným nepriateľom na [{targetX},{targetY}].");
                        continue;
                    }

                    SpawnSlotValueOnSlot(slots[targetX, targetY], cellData, enemyIndex);
                }
            }
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

        DealDamage(overlapped);
        ClearPreview();
        return true;
    }

    private void DealDamage(List<(GridSlot slot, CellSetup attackCell)> overlapped)
    {
        foreach (var (slot, attackCell) in overlapped)
        {
            // Zásah dostanú iba políčka, ktoré ešte nie sú zničené (HasAliveBlock)
            if (slot.HasAliveBlock)
            {
                int finalDamage = GetDamage(attackCell, slot);
                if (finalDamage > 0)
                {
                    slot.currentBlock.TakeDamage(finalDamage);
                }
            }
        }
    }

    // Zbrane a útoky nemajú živly - poškodenie je hrúbka políčka útoku
    private static int GetDamage(CellSetup attackCell, GridSlot targetSlot)
    {
        return Mathf.Max(0, attackCell.thickness);
    }

    // --- LOGIKA ÚTOKU NEPRIATEĽA NA HRÁČA ---

    // Vyberie stredové políčko pre útok nepriateľa (null = útok nemá čo zasiahnuť)
    public GridSlot FindEnemyAttackTarget(GridPreset attackPreset, EnemyTargeting targeting)
    {
        if (slots == null || attackPreset == null) return null;

        var candidates = new List<GridSlot>();
        int bestScore = 0;

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            int score = GetAttackScore(slot, attackPreset);
            if (score <= 0) continue;

            if (targeting == EnemyTargeting.BestHit)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    candidates.Clear();
                }
                if (score == bestScore) candidates.Add(slot);
            }
            else
            {
                candidates.Add(slot);
            }
        }

        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    // Koľko životov by útok na danom mieste reálne zobral
    private int GetAttackScore(GridSlot centerSlot, GridPreset attackPreset)
    {
        int score = 0;
        foreach (var (slot, attackCell) in GetOverlappedSlots(centerSlot, attackPreset))
        {
            if (!slot.HasAliveBlock) continue;
            score += Mathf.Min(GetDamage(attackCell, slot), slot.currentBlock.thickness);
        }
        return score;
    }

    public void ShowEnemyAttackPreview(GridSlot centerSlot, GridPreset attackPreset)
    {
        ClearPreview();
        if (!isPlayerGrid) return;

        foreach (var (slot, _) in GetOverlappedSlots(centerSlot, attackPreset))
        {
            Color previewColor = slot.HasAliveBlock
                ? new Color(0.65f, 0.15f, 0.9f, 0.9f)
                : new Color(0.65f, 0.45f, 0.9f, 0.45f);

            slot.SetHighlight(true, previewColor);
        }
    }

    public bool ApplyEnemyAttack(GridSlot centerSlot, GridPreset attackPreset)
    {
        if (!isPlayerGrid) return false;

        var overlapped = GetOverlappedSlots(centerSlot, attackPreset);
        if (overlapped.Count == 0) return false;

        DealDamage(overlapped);
        ClearPreview();
        return true;
    }

    // --- KONTROLA ŽIVOTOV (VÝHRA / PREHRA) ---

    public bool HasAnyAliveBlock()
    {
        if (slots == null) return false;
        foreach (var s in slots)
        {
            if (s != null && s.HasAliveBlock) return true;
        }
        return false;
    }

    public bool IsEnemyAlive(int enemyIndex)
    {
        if (slots == null) return false;
        foreach (var s in slots)
        {
            if (s != null && s.HasAliveBlock && s.currentBlock.ownerEnemyIndex == enemyIndex) return true;
        }
        return false;
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

    // --- SCHOPNOSTI NEPRIATEĽOV NA KONCI KOLA (NAPR. BOSS) ---

    // Otočí všetky políčka nepriateľa o 90° v smere hodinových ručičiek v rámci jeho oblasti
    public void RotateEnemyClockwise(int enemyIndex)
    {
        if (slots == null || enemyIndex < 0 || enemyIndex >= enemyAreas.Count) return;

        RectInt area = enemyAreas[enemyIndex];
        if (area.width != area.height)
        {
            Debug.LogWarning($"Otočenie funguje iba pre štvorcového nepriateľa ({area.width}x{area.height}).");
            return;
        }
        if (area.xMin < 0 || area.yMin < 0 || area.xMax > totalWidth || area.yMax > totalHeight)
        {
            Debug.LogWarning("Nepriateľ presahuje grid, otočenie sa preskočí.");
            return;
        }

        var moved = new List<(SlotValue block, int x, int y)>();
        for (int x = area.xMin; x < area.xMax; x++)
        {
            for (int y = area.yMin; y < area.yMax; y++)
            {
                GridSlot slot = slots[x, y];
                if (slot.currentBlock == null || slot.currentBlock.ownerEnemyIndex != enemyIndex) continue;

                // Lokálne (lx, ly) -> (veľkosť - 1 - ly, lx); riadky idú zhora nadol
                int lx = x - area.xMin;
                int ly = y - area.yMin;
                moved.Add((slot.currentBlock, area.xMin + area.height - 1 - ly, area.yMin + lx));
                slot.currentBlock = null;
            }
        }

        foreach (var (block, x, y) in moved)
        {
            GridSlot target = slots[x, y];
            target.currentBlock = block;
            block.transform.SetParent(target.transform, false);
            block.transform.localPosition = new Vector3(0, 0, -0.1f);
        }
    }

    // Zmení živel všetkých živlových políčok nepriateľa (Basic ostáva), hrúbka sa zachová
    public void CycleEnemyElements(int enemyIndex)
    {
        if (slots == null) return;

        foreach (var slot in slots)
        {
            SlotValue block = slot.currentBlock;
            if (block == null || block.ownerEnemyIndex != enemyIndex) continue;

            BlockElement next = GetNextElement(block.element);
            if (next == block.element) continue;

            ReplaceBlockElement(slot, next);
        }
    }

    // Vymení políčko za prefab iného živlu, hrúbka a vlastník sa zachovajú
    private SlotValue ReplaceBlockElement(GridSlot slot, BlockElement newElement)
    {
        SlotValue block = slot.currentBlock;
        if (block == null || GetVariantPrefab(newElement) == null) return null;

        int maxThickness = block.maxThickness;
        int currentThickness = block.thickness;
        int owner = block.ownerEnemyIndex;

        Destroy(block.gameObject);
        slot.currentBlock = null;

        SpawnSlotValueOnSlot(slot, new CellSetup { element = newElement, thickness = maxThickness }, owner);
        if (slot.currentBlock != null) slot.currentBlock.SetCurrentThickness(currentThickness);
        return slot.currentBlock;
    }

    // --- SCHOPNOSTI ŽIVLOVÝCH POLÍČOK (NA KONCI KOLA) ---
    // Lava: 1 poškodenie náhodnému živému políčku súpera (platí bonus živlov), cieľ sa krátko ukáže
    // Rock: so šancou +1 hodnota (max 3)
    // Nature: so šancou sa rozrastie na 1 náhodného suseda (prázdne alebo Basic políčko), inak +1 sama sebe
    // Ice: uhýbanie sa rieši priamo pri zásahu (SlotValue.TakeDamage)
    public IEnumerator ApplyElementEffects(GridManager opponentGrid, float lavaPreviewTime)
    {
        if (slots == null) yield break;

        // Živé políčka si uložíme vopred, aby novo zarastené políčka v tomto kole nekonali znova
        var aliveSlots = new List<GridSlot>();
        foreach (var slot in slots)
        {
            if (slot != null && slot.HasAliveBlock) aliveSlots.Add(slot);
        }

        int lavaHits = 0;
        foreach (GridSlot slot in aliveSlots)
        {
            SlotValue block = slot.currentBlock;
            if (block == null || !block.IsAlive) continue;

            switch (block.element)
            {
                case BlockElement.Lava:
                    lavaHits++;
                    break;
                case BlockElement.Rock:
                    if (Random.value < SlotValue.RockGrowChance) block.Grow(1);
                    break;
                case BlockElement.Nature:
                    if (Random.value < SlotValue.NatureSpreadChance) SpreadNature(slot);
                    break;
            }
        }

        if (lavaHits > 0 && opponentGrid != null)
        {
            yield return opponentGrid.TakeLavaHits(lavaHits, lavaPreviewTime);
        }
    }

    // Lávové zásahy do náhodných živých políčok tohto gridu - ciele sa najprv krátko ukážu oranžovou
    private IEnumerator TakeLavaHits(int count, float previewTime)
    {
        var alive = new List<GridSlot>();
        foreach (var slot in slots)
        {
            if (slot != null && slot.HasAliveBlock) alive.Add(slot);
        }
        if (alive.Count == 0) yield break;

        var targets = new List<GridSlot>();
        for (int i = 0; i < count; i++)
        {
            GridSlot target = alive[Random.Range(0, alive.Count)];
            targets.Add(target);
            target.SetHighlight(true, new Color(1f, 0.55f, 0f, 0.9f));
        }

        yield return new WaitForSeconds(previewTime);

        foreach (GridSlot target in targets)
        {
            target.SetHighlight(false, Color.white);
            if (!target.HasAliveBlock) continue;

            int damage = Mathf.Max(0, 1 + GetElementBonus(BlockElement.Lava, target.currentBlock.element));
            target.currentBlock.TakeDamage(damage);
        }
    }

    private void SpreadNature(GridSlot natureSlot)
    {
        SlotValue nature = natureSlot.currentBlock;

        var candidates = new List<GridSlot>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int dir in directions)
        {
            int nx = natureSlot.x + dir.x;
            int ny = natureSlot.y + dir.y;
            if (nx < 0 || ny < 0 || nx >= totalWidth || ny >= totalHeight) continue;

            GridSlot neighbour = slots[nx, ny];

            // Prázdne políčko alebo živé Basic políčko toho istého vlastníka (nie iného nepriateľa)
            bool isEmpty = neighbour.currentBlock == null;
            bool isOwnBasic = neighbour.HasAliveBlock
                && neighbour.currentBlock.element == BlockElement.Basic
                && neighbour.currentBlock.ownerEnemyIndex == nature.ownerEnemyIndex;

            if (isEmpty || isOwnBasic) candidates.Add(neighbour);
        }

        if (candidates.Count == 0)
        {
            nature.Grow(1);
            return;
        }

        // Rozrastie sa iba na jedno políčko; jeho hodnota sa nezvyšuje
        GridSlot target = candidates[Random.Range(0, candidates.Count)];
        if (target.currentBlock == null)
        {
            SpawnSlotValueOnSlot(target, new CellSetup { element = BlockElement.Nature, thickness = 1 }, nature.ownerEnemyIndex);
        }
        else
        {
            ReplaceBlockElement(target, BlockElement.Nature);
        }
    }

    // Poradie živlov: Lava -> Ice -> Nature -> Rock -> Lava
    public static BlockElement GetNextElement(BlockElement element)
    {
        return element switch
        {
            BlockElement.Lava => BlockElement.Ice,
            BlockElement.Ice => BlockElement.Nature,
            BlockElement.Nature => BlockElement.Rock,
            BlockElement.Rock => BlockElement.Lava,
            _ => element
        };
    }

    // Svetová pozícia nad stredom horného okraja gridu (napr. pre text staminy)
    public Vector3 GetTopCenterWorld(float extraOffset = 0f)
    {
        float topEdge = totalHeight * spacing * 0.5f;
        return transform.TransformPoint(new Vector3(0f, topEdge + extraOffset, 0f));
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
            GameObject child = transform.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                child.transform.SetParent(null);
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
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
