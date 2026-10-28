using UnityEngine;

public enum BlockElement { None, Basic, Lava, Ice, Rock, Nature }

public class SlotValue : MonoBehaviour
{
    public BlockElement element;
    public int thickness = 1; // Hrúbka políčka 1, 2 alebo 3

    // Index nepriateľa (-1 = Hráč, 0 = Prvý nepriateľ, 1 = Druhý nepriateľ, atď.)
    [HideInInspector]
    public int ownerEnemyIndex = -1;

    [Header("Tower Layers (Sprites for thickness 1, 2, 3)")]
    [SerializeField] private GameObject bottomLayer; // Hrúbka 1
    [SerializeField] private GameObject middleLayer; // Hrúbka 2
    [SerializeField] private GameObject topLayer;    // Hrúbka 3

    public void Setup(int startThickness, int ownerIndex = -1)
    {
        thickness = Mathf.Clamp(startThickness, 1, 3);
        ownerEnemyIndex = ownerIndex;
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