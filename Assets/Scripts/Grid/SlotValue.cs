using UnityEngine;

public enum BlockElement { None, Basic, Lava, Ice, Rock, Nature }

public class SlotValue : MonoBehaviour
{
    public BlockElement element;
    public int thickness = 1; // Hrúbka 1, 2 alebo 3

    [Header("Vrstvy vežičky (Sprity pre hrúbku 1, 2, 3)")]
    [SerializeField] private GameObject bottomLayer; // Hrúbka 1 (Earth_Health_bottom)
    [SerializeField] private GameObject middleLayer; // Hrúbka 2 (Earth_Health_middle)
    [SerializeField] private GameObject topLayer;    // Hrúbka 3 (Earth_Health_top)

    public void Setup(int startThickness)
    {
        thickness = Mathf.Clamp(startThickness, 1, 3);
        UpdateVisual();
    }

    public void TakeDamage(int dmg)
    {
        thickness -= dmg;
        if (thickness <= 0)
        {
            // Políčko sa úplne roztrieštilo!
            Destroy(gameObject);
        }
        else
        {
            // Postupne „odlupujeme“ vrchné poschodia vežičky
            UpdateVisual();
        }
    }

    // Pridané aj pre prípad liečenia (napr. Nature pasívka nepriateľa alebo Kováč)
    public void HealThickness(int amount)
    {
        thickness = Mathf.Clamp(thickness + amount, 1, 3);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        // Zapne príslušné vrstvy podľa aktuálnej hrúbky (1, 2 alebo 3)
        if (bottomLayer != null) bottomLayer.SetActive(thickness >= 1);
        if (middleLayer != null) middleLayer.SetActive(thickness >= 2);
        if (topLayer != null)    topLayer.SetActive(thickness >= 3);
    }
}