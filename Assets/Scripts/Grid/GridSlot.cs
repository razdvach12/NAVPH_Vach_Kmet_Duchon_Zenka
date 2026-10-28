using UnityEngine;

public class GridSlot : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }
    public GridManager parentGrid { get; private set; }

    public SlotValue currentBlock;

    [SerializeField] private SpriteRenderer slotRenderer;
    private Color defaultColor;

    public void Init(int x, int y, GridManager grid)
    {
        this.x = x;
        this.y = y;
        this.parentGrid = grid;

        if (slotRenderer == null) slotRenderer = GetComponent<SpriteRenderer>();
        if (slotRenderer != null) defaultColor = slotRenderer.color;
    }

    public void SetHighlight(bool active, Color highlightColor)
    {
        // 1. Zafarbí šedý podkladový štvorček
        if (slotRenderer != null)
        {
            slotRenderer.color = active ? highlightColor : defaultColor;
        }

        // 2. Ak je na políčku reálne HPčko (SlotValue), zafarbí aj samotnú vežičku!
        if (currentBlock != null)
        {
            currentBlock.SetHighlight(active, highlightColor);
        }
    }
}