using UnityEngine;

public class GridSlot : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }

    // Referencia na aktuálne políčko života (SlotValue), ktoré sedí na tomto slote
    public SlotValue currentBlock;

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}