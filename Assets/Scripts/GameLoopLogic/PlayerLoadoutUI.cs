using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Sekcia hráča v súboji: sloty zbraní vedľa gridu a sloty consumables pod gridom
public class PlayerLoadoutUI : MonoBehaviour
{
    [Header("Sloty zbraní (zhora nadol)")]
    [SerializeField] private List<WeaponButtonUI> weaponSlots = new List<WeaponButtonUI>();

    [Header("Sloty consumables (zľava doprava)")]
    [SerializeField] private List<SpriteRenderer> consumableSlots = new List<SpriteRenderer>();

    private void Start()
    {
        // Ak hráč prišiel z výberu postavy, dáme do slotov jeho zbrane.
        // Inak (spustenie scény priamo v editore) ostanú zbrane nastavené v scéne.
        if (PlayerStats.Instance != null)
        {
            FillWeaponSlots(PlayerStats.Instance.unlockedWeapons);
        }
        RefreshWeaponIcons();

        // Inventár consumables zatiaľ neexistuje - sloty sú prázdne
        foreach (SpriteRenderer slot in consumableSlots)
        {
            if (slot != null) slot.sprite = null;
        }
    }

    // Klávesy 1-4 vyberú zbraň z príslušného slotu
    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectWeaponSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectWeaponSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectWeaponSlot(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectWeaponSlot(3);
    }

    private void SelectWeaponSlot(int index)
    {
        if (index < 0 || index >= weaponSlots.Count || weaponSlots[index] == null) return;
        weaponSlots[index].SelectThisWeapon();
    }

    public void FillWeaponSlots(List<GridPreset> weapons)
    {
        for (int i = 0; i < weaponSlots.Count; i++)
        {
            if (weaponSlots[i] == null) continue;
            weaponSlots[i].weaponPreset = (weapons != null && i < weapons.Count) ? weapons[i] : null;
        }
    }

    // Ikona v slote sa vezme z Weapon Icon zbrane
    private void RefreshWeaponIcons()
    {
        foreach (WeaponButtonUI slot in weaponSlots)
        {
            if (slot == null) continue;

            SpriteRenderer icon = slot.GetComponent<SpriteRenderer>();
            if (icon != null) icon.sprite = slot.weaponPreset != null ? slot.weaponPreset.weaponIcon : null;
        }
    }
}
