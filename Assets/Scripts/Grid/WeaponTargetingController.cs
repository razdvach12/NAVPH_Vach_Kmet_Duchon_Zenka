using UnityEngine;
using UnityEngine.InputSystem; // Použitie nového Unity Input Systemu

public class WeaponTargetingController : MonoBehaviour
{
    [Header("Currently Selected Weapon")]
    public GridPreset selectedWeapon;

    private GridSlot lastHoveredSlot;

    // Zavolá sa po kliknutí na ikonu zbrane
    public void SelectWeapon(GridPreset weaponPreset)
    {
        ClearLastPreview();
        selectedWeapon = weaponPreset;
    }

    private void Update()
    {
        // Ak nie je vybraná žiadna zbraň alebo myš nie je pripojená, nič nerobíme
        if (selectedWeapon == null || Mouse.current == null) return;

        // Pravý klik myšou zruší výber zbrane
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClearLastPreview();
            selectedWeapon = null;
            return;
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
                selectedWeapon = null;
                lastHoveredSlot = null;
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