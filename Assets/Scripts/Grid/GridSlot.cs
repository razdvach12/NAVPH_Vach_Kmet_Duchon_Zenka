using UnityEngine;


// tu absolutne netusim co sa deje zatial


public class GridSlot : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }
    public GridManager parentGrid { get; private set; }
    
    // Referencia na aktuálne políčko života, ktoré sedí na tomto slote
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

    // Rozsvietenie políčka pri mierení zbraňou/potionom
    public void SetHighlight(bool active, Color highlightColor)
    {
        if (slotRenderer == null) return;
        slotRenderer.color = active ? highlightColor : defaultColor;
    }
}