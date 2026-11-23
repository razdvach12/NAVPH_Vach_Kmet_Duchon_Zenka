using UnityEngine;

// Nastaví pozadie panelu hráča podľa zvolenej postavy (CharacterData.backgroundSprite).
// Patrí na objekt so SpriteRendererom (PlayerSection -> BoardBordersBackground -> backgroundLeft).
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerBackgroundUI : MonoBehaviour
{
    [Tooltip("Nový obrázok sa roztiahne presne na plochu, ktorú zaberal pôvodný sprite (false = zachová pomer strán a zmestí sa dovnútra)")]
    [SerializeField] private bool stretchToFill = true;

    [Tooltip("Farba obrázka pozadia. Biela = pôvodné farby obrázka (pôvodný panel je zafarbený, čo by obrázok stmavilo)")]
    [SerializeField] private Color backgroundTint = Color.white;

    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        ApplyCharacterBackground();
    }

    public void ApplyCharacterBackground()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        // Bez zvolenej postavy (napr. scéna spustená priamo v editore) ostane pôvodný vzhľad z prefabu/scény
        CharacterData character = PlayerStats.Instance != null ? PlayerStats.Instance.selectedCharacter : null;
        if (character == null || character.backgroundSprite == null) return;

        SetBackground(character.backgroundSprite);
    }

    private void SetBackground(Sprite newSprite)
    {
        if (spriteRenderer.sprite == newSprite) return;

        // Plocha (v lokálnych jednotkách rodiča), ktorú panel zaberá teraz = veľkosť spritu × scale
        Vector2 targetSize = Vector2.one;
        if (spriteRenderer.sprite != null)
        {
            Vector2 oldSize = spriteRenderer.sprite.bounds.size;
            targetSize = new Vector2(oldSize.x * transform.localScale.x, oldSize.y * transform.localScale.y);
        }

        spriteRenderer.sprite = newSprite;
        spriteRenderer.color = backgroundTint;

        // Prepočítame scale tak, aby nový obrázok zabral rovnakú plochu bez ohľadu na jeho rozlíšenie / Pixels Per Unit
        Vector2 newSize = newSprite.bounds.size;
        if (newSize.x <= 0f || newSize.y <= 0f) return;

        float scaleX = targetSize.x / newSize.x;
        float scaleY = targetSize.y / newSize.y;

        if (!stretchToFill)
        {
            float uniform = Mathf.Min(scaleX, scaleY);
            scaleX = uniform;
            scaleY = uniform;
        }

        transform.localScale = new Vector3(scaleX, scaleY, transform.localScale.z);
    }
}
