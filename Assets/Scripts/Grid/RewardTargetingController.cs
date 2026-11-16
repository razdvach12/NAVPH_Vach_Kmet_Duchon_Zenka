using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum RewardTargetingMode
{
    None = 0,
    AddMaxHpCell = 1,
    ChangeCellElement = 2
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

    public void CancelTargeting()
    {
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
