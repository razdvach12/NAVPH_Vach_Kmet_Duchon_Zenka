using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConsumableButtonUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Consumable v tomto slote (null = prázdny slot)")]
    public ConsumableData consumable;

    [Header("References")]
    public TurnManager turnManager;
    public WeaponTargetingController targetingController; // Ak nie je nastavený, nájde sa automaticky

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

            // Consumables s tvarom
            case ConsumableEffect.HealPlayer:
            case ConsumableEffect.DamageEnemy:
                if (!turnManager.HasStamina(consumable.staminaCost)) return;
                if (targetingController == null) targetingController = FindAnyObjectByType<WeaponTargetingController>();
                if (targetingController == null) return;

                GridPreset shape = consumable.GetRandomShape();
                if (shape == null)
                {
                    Debug.LogWarning($"Consumable '{consumable.name}' nemá nastavené žiadne consumableShapes.");
                    return;
                }
                targetingController.SelectConsumable(shape, this);
                break;

            case ConsumableEffect.Flee:
                if (!turnManager.HasStamina(consumable.staminaCost)) return;
                ConsumeItem(); 
                turnManager.TryFlee();
                break;
        }
    }

    // Spotrebuje predmet
    public void ConsumeItem()
    {
        if (consumable == null) return;

        ConsumableData used = consumable;
        consumable = null;
        Refresh();

        if (PlayerStats.Instance != null) PlayerStats.Instance.RemoveConsumable(used);
    }
}
