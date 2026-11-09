using UnityEngine;

public class GridSlot : MonoBehaviour
{
    public int x { get; private set; }
    public int y { get; private set; }
    public GridManager parentGrid { get; private set; }

    public SlotValue currentBlock;

    // Či toto políčko patrí do Max HP (z presetu postavy alebo odomknuté upgradom, nie dočasný rast Nature)
    public bool isMaxHp { get; private set; }

    // Políčko je odomknuté v Max HP (aj keď je práve zničené na 0)
    public bool IsUnlockedMaxHp => isMaxHp && currentBlock != null;

    // Políčko má aktuálne život > 0
    public bool HasAliveBlock => currentBlock != null && currentBlock.IsAlive;

    // Políčko sa dá liečiť potionom (je súčasťou Max HP a nemá plnú hrúbku)
    public bool CanBeHealed => IsUnlockedMaxHp && currentBlock.thickness < currentBlock.maxThickness;

    [SerializeField] private SpriteRenderer slotRenderer;
    private Color defaultColor;

    private SpriteRenderer topBorder;
    private SpriteRenderer bottomBorder;
    private SpriteRenderer leftBorder;
    private SpriteRenderer rightBorder;

    private static Sprite s_whitePixelSprite;

    public void Init(int x, int y, GridManager grid)
    {
        this.x = x;
        this.y = y;
        this.parentGrid = grid;
        this.isMaxHp = false;

        if (slotRenderer == null) slotRenderer = GetComponent<SpriteRenderer>();
        if (slotRenderer != null) defaultColor = slotRenderer.color;
    }

    public void SetMaxHp(bool value)
    {
        isMaxHp = value;
    }

    // Zobrazí biele čiary po obvode Max HP na tých stranách políčka, kde nesusedí s iným Max HP políčkom
    public void SetMaxHpOutline(bool top, bool bottom, bool left, bool right, float spacing, float thickness, Color color)
    {
        float halfSpacing = spacing * 0.5f;
        float length = spacing + thickness;
        const float zOffset = -0.15f;

        UpdateBorderEdge(ref topBorder, "MaxHpBorder_Top", top,
            new Vector3(0f, halfSpacing, zOffset),
            new Vector3(length, thickness, 1f), color);

        UpdateBorderEdge(ref bottomBorder, "MaxHpBorder_Bottom", bottom,
            new Vector3(0f, -halfSpacing, zOffset),
            new Vector3(length, thickness, 1f), color);

        UpdateBorderEdge(ref leftBorder, "MaxHpBorder_Left", left,
            new Vector3(-halfSpacing, 0f, zOffset),
            new Vector3(thickness, length, 1f), color);

        UpdateBorderEdge(ref rightBorder, "MaxHpBorder_Right", right,
            new Vector3(halfSpacing, 0f, zOffset),
            new Vector3(thickness, length, 1f), color);
    }

    private void UpdateBorderEdge(ref SpriteRenderer borderRenderer, string edgeName, bool active, Vector3 localPos, Vector3 localScale, Color color)
    {
        if (!active)
        {
            if (borderRenderer != null) borderRenderer.gameObject.SetActive(false);
            return;
        }

        if (borderRenderer == null)
        {
            Transform existing = transform.Find(edgeName);
            if (existing != null)
            {
                borderRenderer = existing.GetComponent<SpriteRenderer>();
            }

            if (borderRenderer == null)
            {
                GameObject edgeObj = new GameObject(edgeName);
                edgeObj.transform.SetParent(transform, false);
                borderRenderer = edgeObj.AddComponent<SpriteRenderer>();
            }
        }

        borderRenderer.gameObject.SetActive(true);
        borderRenderer.sprite = GetWhitePixelSprite();
        borderRenderer.color = color;

        if (slotRenderer != null)
        {
            borderRenderer.sharedMaterial = slotRenderer.sharedMaterial;
            borderRenderer.sortingLayerID = slotRenderer.sortingLayerID;
        }
        // Nad GridSlot (2) aj nad všetkými vrstvami HP vežičky (3, 4, 5)
        borderRenderer.sortingOrder = 6;

        Transform t = borderRenderer.transform;
        t.localPosition = localPos;
        t.localRotation = Quaternion.identity;
        t.localScale = localScale;
    }

    private static Sprite GetWhitePixelSprite()
    {
        if (s_whitePixelSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            s_whitePixelSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                tex.width
            );
            s_whitePixelSprite.name = "MaxHpWhitePixel";
        }
        return s_whitePixelSprite;
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
