using UnityEngine;

public enum BlockElement { None, Basic, Lava, Ice, Rock, Nature }

public class SlotValue : MonoBehaviour
{
    public BlockElement element;
    public int thickness = 1;

    [HideInInspector]
    public int ownerEnemyIndex = -1;

    [Header("Tower Layers (Sprites for thickness 1, 2, 3)")]
    [SerializeField] private GameObject bottomLayer;
    [SerializeField] private GameObject middleLayer;
    [SerializeField] private GameObject topLayer;

    private SpriteRenderer[] layerRenderers;

    private void Awake()
    {
        // Uložíme si všetky SpriteRenderery z vrstiev vežičky (Bottom, Middle, Top)
        layerRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void Setup(int startThickness, int ownerIndex = -1)
    {
        thickness = Mathf.Clamp(startThickness, 1, 3);
        ownerEnemyIndex = ownerIndex;
        UpdateVisual();
    }

    public void TakeDamage(int damage)
    {
        thickness -= damage;
        if (thickness <= 0)
        {
            Destroy(gameObject);
        }
        else
        {
            UpdateVisual();
        }
    }

    // Zafarbí priamo vrstvy HP vežičky pri mierení zbraňou
    public void SetHighlight(bool active, Color highlightColor)
    {
        if (layerRenderers == null) return;

        foreach (var sr in layerRenderers)
        {
            if (sr != null)
            {
                // Keď je active = false, vráti sa biela (čo v Unity znamená pôvodné farby spritu)
                sr.color = active ? highlightColor : Color.white;
            }
        }
    }

    private void UpdateVisual()
    {
        if (bottomLayer != null) bottomLayer.SetActive(thickness >= 1);
        if (middleLayer != null) middleLayer.SetActive(thickness >= 2);
        if (topLayer != null)    topLayer.SetActive(thickness >= 3);
    }
}