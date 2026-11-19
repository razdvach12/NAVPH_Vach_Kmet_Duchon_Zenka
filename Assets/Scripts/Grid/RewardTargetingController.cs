using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum RewardTargetingMode
{
    None = 0,
    AddMaxHpCell = 1,
    ChangeCellElement = 2,
    AddWeaponCell = 3
}

public class RewardTargetingController : MonoBehaviour
{
    public static RewardTargetingController Instance { get; private set; }

    [Header("Cieľový grid hráča (ak nie je priradený, nájde sa automaticky)")]
    public GridManager playerGrid;

    [Header("Aktuálny stav výberu odmeny")]
    public RewardTargetingMode currentMode = RewardTargetingMode.None;
    public BlockElement targetElement = BlockElement.Basic;

    private GridSlot lastHoveredSlot;
    private Action onRewardAppliedCallback;

    public bool IsTargeting => currentMode != RewardTargetingMode.None;

    public static RewardTargetingController EnsureInstance()
    {
        if (Instance != null) return Instance;

        Instance = FindAnyObjectByType<RewardTargetingController>();
        if (Instance != null) return Instance;

        GameObject go = new GameObject("RewardTargetingController");
        Instance = go.AddComponent<RewardTargetingController>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        ResolvePlayerGrid();
    }

    private GridManager ResolvePlayerGrid()
    {
        if (playerGrid != null) return playerGrid;

        GridManager[] grids = FindObjectsByType<GridManager>();
        foreach (GridManager gm in grids)
        {
            if (gm != null && gm.IsPlayerHealthGrid)
            {
                playerGrid = gm;
                return playerGrid;
            }
        }

        foreach (GridManager gm in grids)
        {
            if (gm != null && gm.isPlayerGrid && gm.gameObject.name != "WeaponGrid")
            {
                playerGrid = gm;
                return playerGrid;
            }
        }

        return null;
    }

    // Spustí režim pridania nového Max HP políčka (Reward Type A)
    public void StartAddMaxHpTargeting(Action onApplied = null)
    {
        CancelTargeting();
        GridManager grid = ResolvePlayerGrid();

        currentMode = RewardTargetingMode.AddMaxHpCell;
        targetElement = BlockElement.Basic;
        onRewardAppliedCallback = onApplied;
        if (grid != null)
        {
            ShowCurrentPreview(grid, null);
        }
        Debug.Log("RewardTargeting: Vyber prázdne políčko susediace s tvojím Max HP gridom.");
    }

    // Spustí režim zmeny živlu existujúceho Max HP políčka (Reward Type B)
    public void StartChangeElementTargeting(BlockElement newElement, Action onApplied = null)
    {
        CancelTargeting();
        GridManager grid = ResolvePlayerGrid();

        currentMode = RewardTargetingMode.ChangeCellElement;
        targetElement = newElement;
        onRewardAppliedCallback = onApplied;
        if (grid != null)
        {
            ShowCurrentPreview(grid, null);
        }
        Debug.Log($"RewardTargeting: Vyber existujúce Max HP políčko pre zmenu živlu na {newElement}.");
    }

    // --- BONUS WEAPON SIZE ---
    // 1. hráč klikne na slot zbrane dole v UI, 2. namiesto HP gridu sa ukáže grid zbrane s voľným okrajom,
    // 3. klik na zelené políčko pridá zbrani políčko s 1 damage (uloží sa kópia, originálny asset sa nemení)

    private ShopUIController shopUI;
    private int weaponUpgradeIndex = -1;
    private GridPreset weaponUpgradePreview; // Kópia zbrane s prázdnym okrajom, ktorú ukazuje weaponUpgradeGrid
    private GridManager weaponUpgradeGrid;
    private bool hiddenHealthGrid;

    public void StartAddWeaponCellTargeting(Action onApplied = null)
    {
        CancelTargeting();

        if (shopUI == null) shopUI = FindAnyObjectByType<ShopUIController>();
        if (shopUI == null || PlayerStats.Instance == null || PlayerStats.Instance.unlockedWeapons.Count == 0)
        {
            Debug.Log("RewardTargeting: Nemáš žiadnu zbraň na vylepšenie.");
            return;
        }

        currentMode = RewardTargetingMode.AddWeaponCell;
        onRewardAppliedCallback = onApplied;
        weaponUpgradeIndex = -1;
        shopUI.SetWeaponSlotsHighlight(true);
        Debug.Log("RewardTargeting: Klikni na zbraň, ktorú chceš zväčšiť.");
    }

    private void SelectWeaponForUpgrade(int index)
    {
        DestroyWeaponUpgradeGrid();

        GridPreset weapon = PlayerStats.Instance.unlockedWeapons[index];
        if (weapon == null) return;

        GridManager hpGrid = ResolvePlayerGrid();
        if (hpGrid == null) return;

        weaponUpgradeIndex = index;
        weaponUpgradePreview = weapon.CreatePaddedCopy(1);

        // Grid zbrane sa zobrazí na mieste HP gridu v rovnakej veľkosti políčok
        // (názov "WeaponGrid" = GridManager ho neberie ako HP grid hráča)
        var go = new GameObject("WeaponGrid");
        go.transform.SetParent(hpGrid.transform.parent, false);
        go.transform.localPosition = hpGrid.transform.localPosition;
        go.transform.localRotation = hpGrid.transform.localRotation;
        go.transform.localScale = hpGrid.transform.localScale;

        weaponUpgradeGrid = go.AddComponent<GridManager>();
        weaponUpgradeGrid.isPlayerGrid = true;
        weaponUpgradeGrid.CopyPrefabsFrom(hpGrid);
        weaponUpgradeGrid.SetShowMaxHpOutline(false);
        weaponUpgradeGrid.SetSinglePreset(weaponUpgradePreview);
        weaponUpgradeGrid.GenerateSingleGrid(weaponUpgradePreview);

        hpGrid.gameObject.SetActive(false);
        hiddenHealthGrid = true;

        shopUI.SetWeaponSlotsHighlight(true, index);
        lastHoveredSlot = null;
    }

    private void UpdateWeaponCellTargeting()
    {
        Vector2 screenMousePos = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Klik na slot zbrane = výber (alebo zmena) zbrane
            int clickedWeapon = shopUI != null ? shopUI.GetWeaponSlotIndexAt(screenMousePos) : -1;
            if (clickedWeapon >= 0)
            {
                SelectWeaponForUpgrade(clickedWeapon);
                return;
            }
        }

        if (weaponUpgradeGrid == null || Camera.main == null) return;

        Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(screenMousePos);
        RaycastHit2D hit = Physics2D.Raycast(worldMousePos, Vector2.zero);
        GridSlot currentSlot = hit.collider != null ? hit.collider.GetComponentInParent<GridSlot>() : null;
        if (currentSlot != null && currentSlot.parentGrid != weaponUpgradeGrid) currentSlot = null;

        // Náhľad prekresľujeme každý frame - GridManager si v Start() grid vygeneruje znova (nové políčka bez zvýraznenia)
        weaponUpgradeGrid.ShowAddWeaponCellPreview(currentSlot);
        lastHoveredSlot = currentSlot;

        if (Mouse.current.leftButton.wasPressedThisFrame && currentSlot != null && weaponUpgradeGrid.CanAddWeaponCellAt(currentSlot))
        {
            if (weaponUpgradeGrid.CanStackWeaponCellAt(currentSlot))
            {
                // Uloženie na existujúce políčko: damage +1 (max 3)
                CellSetup cell = weaponUpgradePreview.GetCell(currentSlot.x, currentSlot.y);
                cell.thickness = Mathf.Clamp(cell.thickness + 1, 1, 3);
                weaponUpgradePreview.SetCell(currentSlot.x, currentSlot.y, cell);
            }
            else
            {
                weaponUpgradePreview.SetCell(currentSlot.x, currentSlot.y, new CellSetup { element = BlockElement.Basic, thickness = 1 });
            }
            GridPreset upgraded = weaponUpgradePreview.CreateTrimmedCopy();
            int index = weaponUpgradeIndex;

            Action callback = onRewardAppliedCallback;
            CancelTargeting();

            PlayerStats.Instance.ReplaceWeapon(index, upgraded);
            Debug.Log($"RewardTargeting: Zbraň {upgraded.name} má nové políčko ({upgraded.width}x{upgraded.height}).");
            callback?.Invoke();
        }
    }

    private void DestroyWeaponUpgradeGrid()
    {
        if (weaponUpgradeGrid != null) Destroy(weaponUpgradeGrid.gameObject);
        weaponUpgradeGrid = null;

        if (weaponUpgradePreview != null) Destroy(weaponUpgradePreview);
        weaponUpgradePreview = null;
    }

    private void EndWeaponUpgrade()
    {
        DestroyWeaponUpgradeGrid();
        weaponUpgradeIndex = -1;

        if (hiddenHealthGrid && playerGrid != null) playerGrid.gameObject.SetActive(true);
        hiddenHealthGrid = false;

        if (shopUI != null) shopUI.SetWeaponSlotsHighlight(false);
    }

    public void CancelTargeting()
    {
        if (currentMode == RewardTargetingMode.AddWeaponCell) EndWeaponUpgrade();

        if (playerGrid != null)
        {
            playerGrid.ClearPreview();
        }
        else if (lastHoveredSlot != null && lastHoveredSlot.parentGrid != null)
        {
            lastHoveredSlot.parentGrid.ClearPreview();
        }

        currentMode = RewardTargetingMode.None;
        targetElement = BlockElement.Basic;
        lastHoveredSlot = null;
        onRewardAppliedCallback = null;
    }

    private void Update()
    {
        if (!IsTargeting || Mouse.current == null) return;

        // Escape alebo pravý klik zruší výber cieľa (hráč môže kliknúť na inú odmenu)
        if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || Mouse.current.rightButton.wasPressedThisFrame)
        {
            CancelTargeting();
            return;
        }

        if (currentMode == RewardTargetingMode.AddWeaponCell)
        {
            UpdateWeaponCellTargeting();
            return;
        }

        GridManager grid = ResolvePlayerGrid();
        if (grid == null || Camera.main == null) return;

        Vector2 screenMousePos = Mouse.current.position.ReadValue();
        Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(screenMousePos);
        RaycastHit2D hit = Physics2D.Raycast(worldMousePos, Vector2.zero);

        GridSlot currentSlot = hit.collider != null ? hit.collider.GetComponentInParent<GridSlot>() : null;
        if (currentSlot != null && currentSlot.parentGrid != grid)
        {
            currentSlot = null;
        }

        if (currentSlot != lastHoveredSlot)
        {
            ShowCurrentPreview(grid, currentSlot);
            lastHoveredSlot = currentSlot;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && currentSlot != null)
        {
            bool applied = false;

            if (currentMode == RewardTargetingMode.AddMaxHpCell)
            {
                applied = grid.ApplyAddMaxHpCell(currentSlot, BlockElement.Basic, 1);
            }
            else if (currentMode == RewardTargetingMode.ChangeCellElement)
            {
                applied = grid.ApplyChangeCellElement(currentSlot, targetElement);
            }

            if (applied)
            {
                Action callback = onRewardAppliedCallback;
                CancelTargeting();
                callback?.Invoke();
            }
        }
    }

    private void ShowCurrentPreview(GridManager grid, GridSlot hoveredSlot)
    {
        if (currentMode == RewardTargetingMode.AddMaxHpCell)
        {
            grid.ShowAddMaxHpPreview(hoveredSlot);
        }
        else if (currentMode == RewardTargetingMode.ChangeCellElement)
        {
            grid.ShowChangeElementPreview(hoveredSlot);
        }
    }
}
