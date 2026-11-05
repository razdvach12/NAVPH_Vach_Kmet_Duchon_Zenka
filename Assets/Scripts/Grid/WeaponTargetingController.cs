using UnityEngine;
using UnityEngine.InputSystem; // Použitie nového Unity Input Systemu

public class WeaponTargetingController : MonoBehaviour
{
    [Header("Currently Selected Weapon")]
    public GridPreset selectedWeapon;

    [Header("Ťahy a stamina (voliteľné)")]
    public TurnManager turnManager;

    private GridSlot lastHoveredSlot;

    // Dočasná otočená kópia zbrane (originálny asset sa pri otáčaní nemení)
    private GridPreset rotatedWeapon;

    // Zavolá sa po kliknutí na ikonu zbrane
    public void SelectWeapon(GridPreset weaponPreset)
    {
        ClearLastPreview();
        DiscardRotatedWeapon();

        if (turnManager != null && !turnManager.HasStaminaFor(weaponPreset))
        {
            Debug.Log("Nedá sa útočiť - nie je tvoj ťah alebo nemáš dosť staminy.");
            selectedWeapon = null;
            return;
        }

        selectedWeapon = weaponPreset;
    }

    private void CancelSelection()
    {
        ClearLastPreview();
        DiscardRotatedWeapon();
        selectedWeapon = null;
        lastHoveredSlot = null;
    }

    // Otočí vybranú zbraň o 90° v smere hodinových ručičiek a prekreslí náhľad
    private void RotateSelectedWeapon()
    {
        GridPreset rotated = selectedWeapon.CreateRotatedClockwise();
        DiscardRotatedWeapon();
        rotatedWeapon = rotated;
        selectedWeapon = rotated;

        ClearLastPreview();
        if (lastHoveredSlot != null && lastHoveredSlot.parentGrid != null)
        {
            lastHoveredSlot.parentGrid.ShowWeaponPreview(lastHoveredSlot, selectedWeapon);
        }
    }

    private void DiscardRotatedWeapon()
    {
        if (rotatedWeapon != null) Destroy(rotatedWeapon);
        rotatedWeapon = null;
    }

    private void Update()
    {
        // Ak nie je vybraná žiadna zbraň alebo myš nie je pripojená, nič nerobíme
        if (selectedWeapon == null || Mouse.current == null) return;

        // Počas ťahu nepriateľov (alebo po konci súboja) sa zbraň zruší
        if (turnManager != null && !turnManager.CanPlayerAct)
        {
            CancelSelection();
            return;
        }

        // Escape zruší výber zbrane
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelSelection();
            return;
        }

        // Pravý klik myšou otočí zbraň o 90°
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            RotateSelectedWeapon();
        }

        // Zistíme pozíciu myši cez nový Input System
        Vector2 screenMousePos = Mouse.current.position.ReadValue();
        Vector2 worldMousePos = Camera.main.ScreenToWorldPoint(screenMousePos);
        RaycastHit2D hit = Physics2D.Raycast(worldMousePos, Vector2.zero);

        GridSlot currentSlot = hit.collider != null ? hit.collider.GetComponentInParent<GridSlot>() : null;
        // Ak sa myš presunula na iné políčko, prekreslíme náhľad zásahu
        if (currentSlot != lastHoveredSlot)
        {
            ClearLastPreview();
            if (currentSlot != null && currentSlot.parentGrid != null)
            {
                currentSlot.parentGrid.ShowWeaponPreview(currentSlot, selectedWeapon);
            }
            lastHoveredSlot = currentSlot;
        }

        // Ľavý klik myšou nad políčkom nepriateľa = ÚTOK (Damage)
        if (Mouse.current.leftButton.wasPressedThisFrame && currentSlot != null && currentSlot.parentGrid != null)
        {
            bool attackSuccess = currentSlot.parentGrid.ApplyWeaponDamage(currentSlot, selectedWeapon);
            if (attackSuccess)
            {
                GridPreset usedWeapon = selectedWeapon;
                selectedWeapon = null;
                lastHoveredSlot = null;
                if (turnManager != null) turnManager.OnPlayerUsedWeapon(usedWeapon);
                DiscardRotatedWeapon();
            }
        }
    }

    private void ClearLastPreview()
    {
        if (lastHoveredSlot != null && lastHoveredSlot.parentGrid != null)
        {
            lastHoveredSlot.parentGrid.ClearPreview();
        }
    }
}