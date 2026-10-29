using UnityEngine;

public class GridSlot : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }
    public GridManager parentGrid { get; private set; }

    public SlotValue currentBlock;

    // Políčko je odomknuté v Max HP (aj keď je práve zničené na 0)
    public bool IsUnlockedMaxHp => currentBlock != null;

    // Políčko má aktuálne život > 0
    public bool HasAliveBlock => currentBlock != null && currentBlock.IsAlive;

    // Políčko sa dá liečiť potionom (je súčasťou Max HP a nemá plnú hrúbku)
    public bool CanBeHealed => currentBlock != null && currentBlock.thickness < currentBlock.maxThickness;

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
        if (slotRenderer != null)
        {
            slotRenderer.color = active ? highlightColor : defaultColor;
        }

        if (currentBlock != null)
        {
            currentBlock.SetHighlight(active, highlightColor);
        }
    }
}