using UnityEngine;

public class ConsumableButtonUI : MonoBehaviour
{
    [Header("Consumable v tomto slote (null = prázdny slot)")]
    public ConsumableData consumable;

    [Header("References")]
    public TurnManager turnManager;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Refresh();
    }

    // Nastaví ikonu podľa priradeného predmetu
    public void Refresh()
    {
        spriteRenderer.sprite = consumable != null ? consumable.icon : null;
    }

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
