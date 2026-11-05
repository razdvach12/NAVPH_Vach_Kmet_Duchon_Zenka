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

            // Consumables s tvarom: Health Potion (lieči vlastný grid) a Bomba (zasiahne nepriateľa)
            case ConsumableEffect.HealPlayer:
            case ConsumableEffect.DamageEnemy:
                if (!turnManager.HasStamina(consumable.staminaCost)) return;
                if (targetingController == null) targetingController = FindAnyObjectByType<WeaponTargetingController>();
                if (targetingController == null) return;

                // Tvar sa vylosuje raz pri kliknutí, aby náhľad aj samotný efekt použili ten istý
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
                ConsumeItem(); // Najprv spotrebujeme, aby sa smoke bomb odobrala z inventára ešte pred zmenou scény
                turnManager.TryFlee();
                break;
        }
    }

    // Spotrebuje predmet - volá sa až po úspešnom efekte (pri bombe až po zásahu, nie pri výbere)
    public void ConsumeItem()
    {
        if (consumable == null) return;

        // Najprv vyprázdnime slot a až potom odoberieme predmet z inventára.
        // RemoveConsumable spustí OnConsumablesChanged a ShopUIController posunie ostatné predmety
        // do uvoľnených slotov - keby sme slot vynulovali až po tom, zmazali by sme predmet, ktorý sa sem práve posunul.
        ConsumableData used = consumable;
        consumable = null;
        Refresh();

        // PlayerStats nemusí existovať, ak sa Encounter spustí priamo v editore
        if (PlayerStats.Instance != null) PlayerStats.Instance.RemoveConsumable(used);
    }
}
