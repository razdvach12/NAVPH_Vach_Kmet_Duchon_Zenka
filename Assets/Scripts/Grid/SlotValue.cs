using UnityEngine;

public enum BlockElement { None, Basic, Lava, Ice, Rock, Nature }

public class SlotValue : MonoBehaviour
{
    public BlockElement element;
    public int thickness = 1;    // Aktuálna hodnota HP (0 až maxThickness)
    public int maxThickness = 1; // Maximálna hodnota HP tohto políčka (1 až 3)

    [HideInInspector]
    public int ownerEnemyIndex = -1;

    public bool IsAlive => thickness > 0;

    [Header("Tower Layers (Sprites for thickness 1, 2, 3)")]
    [SerializeField] private GameObject bottomLayer;
    [SerializeField] private GameObject middleLayer;
    [SerializeField] private GameObject topLayer;

    // Inštancia vizuálu zničeného políčka (keď thickness == 0)
    private GameObject destroyedVisualInstance;
    private SpriteRenderer[] layerRenderers;

    private void Awake()
    {
        layerRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void Setup(int startThickness, int ownerIndex = -1, GameObject destroyedPrefab = null)
    {
        maxThickness = Mathf.Clamp(startThickness, 1, 3);
        thickness = maxThickness;
        ownerEnemyIndex = ownerIndex;

        // Ak sme dostali prefab pre zničené políčko, vytvoríme ho ako dieťa (na štarte je schovaný)
        if (destroyedPrefab != null && destroyedVisualInstance == null)
        {
            destroyedVisualInstance = Instantiate(destroyedPrefab, transform);
            destroyedVisualInstance.transform.localPosition = Vector3.zero;
            destroyedVisualInstance.name = "DestroyedVisual";
            destroyedVisualInstance.SetActive(false);

            // Znova načítame renderery, aby podsvietenie (Highlight) fungovalo aj na zničenom políčku
            layerRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        UpdateVisual();
    }

    // Šanca, že Rock / Nature políčko na konci kola vykoná svoju schopnosť
    public const float RockGrowChance = 0.5f;
    public const float NatureSpreadChance = 0.5f;

    // Ice políčko má šancu uhnúť sa zásahu podľa svojej hrúbky (1 = 30 %, 2 = 50 %, 3 = 70 %)
    public static float GetIceDodgeChance(int thickness)
    {
        return thickness switch
        {
            1 => 0.3f,
            2 => 0.5f,
            >= 3 => 0.7f,
            _ => 0f
        };
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive || damage <= 0) return;

        if (element == BlockElement.Ice && Random.value < GetIceDodgeChance(thickness)) return;

        thickness = Mathf.Max(0, thickness - damage);
        UpdateVisual();
    }

    // Zvýši hodnotu políčka (max 3) - ak prekročí maximum, zvýši sa aj maximum (Rock, Nature)
    public void Grow(int amount)
    {
        if (!IsAlive) return;

        thickness = Mathf.Min(3, thickness + amount);
        maxThickness = Mathf.Max(maxThickness, thickness);
        UpdateVisual();
    }

    public bool Heal(int amount)
    {
        if (thickness >= maxThickness) return false;

        thickness = Mathf.Min(maxThickness, thickness + amount);
        UpdateVisual();
        return true;
    }

    // Nastaví aktuálnu hrúbku (napr. pri výmene živlu, aby políčko neprišlo o poškodenie)
    public void SetCurrentThickness(int value)
    {
        thickness = Mathf.Clamp(value, 0, maxThickness);
        UpdateVisual();
    }

    public void UpgradeMaxThickness(int addMax)
    {
        maxThickness = Mathf.Clamp(maxThickness + addMax, 1, 3);
        thickness = Mathf.Clamp(thickness + addMax, 1, maxThickness);
        UpdateVisual();
    }

    public void SetHighlight(bool active, Color highlightColor)
    {
        if (layerRenderers == null) return;

        foreach (var sr in layerRenderers)
        {
            if (sr != null)
            {
                sr.color = active ? highlightColor : Color.white;
            }
        }
    }

    private void UpdateVisual()
    {
        // 1. Vrstvy živej vežičky (svietia pri thickness 1, 2, 3)
        if (bottomLayer != null) bottomLayer.SetActive(thickness >= 1);
        if (middleLayer != null) middleLayer.SetActive(thickness >= 2);
        if (topLayer != null)    topLayer.SetActive(thickness >= 3);

        // 2. Vizuál zničeného Max HP políčka (svieti iba keď thickness klesne na 0!)
        if (destroyedVisualInstance != null)
        {
            destroyedVisualInstance.SetActive(thickness == 0);
        }
    }
}