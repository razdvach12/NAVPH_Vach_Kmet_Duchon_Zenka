using UnityEngine;
using TMPro;

public enum BlockElement { None, Basic, Lava, Ice, Rock, Nature }

public class SlotValue : MonoBehaviour
{
    public BlockElement element;
    public int thickness = 1; // Hrúbka 1, 2 alebo 3
    public TextMeshPro thicknessText; // Text priamo na políčku

    public void Setup(int startThickness)
    {
        thickness = startThickness;
        UpdateVisual();
    }

    public void TakeDamage(int dmg)
    {
        thickness -= dmg;
        if (thickness <= 0)
        {
            // Políčko sa roztriešti!
            Destroy(gameObject);
        }
        else
        {
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (thicknessText != null)
            thicknessText.text = thickness.ToString();
    }
}