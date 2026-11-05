using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConsumableButtonUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Consumable v tomto slote (null = prázdny slot)")]
    public ConsumableData consumable;

    [Header("References")]
    public TurnManager turnManager;

    private SpriteRenderer spriteRenderer;
    private Image uiImage;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        uiImage = GetComponent<Image>();
        Refresh();
    }

    // Nastaví ikonu podľa priradeného predmetu
    public void Refresh()
    {
        Sprite icon = consumable != null ? consumable.icon : null;
        if (spriteRenderer != null) spriteRenderer.sprite = icon;
        if (uiImage != null && icon != null) uiImage.sprite = icon;
    }

    // Pre UI objekty v Canvase (napr. PotionSlot v PlayerSection)
    public void OnPointerClick(PointerEventData eventData) => UseConsumable();

    // Pre 2D sprity v scéne (potrebný collider)
    private void OnMouseDown() => UseConsumable();

    public void UseConsumable()
    {
        if (consumable == null || turnManager == null) return;

        switch (consumable.consumableEffect)
        {
            case ConsumableEffect.FreeNextAttack:
                if (!turnManager.HasStamina(consumable.staminaCost)) return;
                turnManager.GrantFreeAttack();
                ConsumeItem();
                turnManager.SpendStamina(consumable.staminaCost);
                break;

            // TODO: ostatné efekty (DamageEnemy, HealPlayer, Flee)
        }
    }

    // Spotrebuje predmet - volá sa až po úspešnom efekte (pri bombe až po zásahu, nie pri výbere)
    public void ConsumeItem()
    {
        if (consumable == null) return;

        // PlayerStats nemusí existovať, ak sa Encounter spustí priamo v editore
        if (PlayerStats.Instance != null) PlayerStats.Instance.RemoveConsumable(consumable);

        consumable = null;
        Refresh();
    }
}
