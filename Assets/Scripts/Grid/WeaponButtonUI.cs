using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Weapon Preset (e.g. 3x3 Sword)")]
    public GridPreset weaponPreset;

    [Header("References")]
    public GridManager weaponTooltipGrid; // Malý GridManager vedľa zbrane na zobrazenie jej tvaru
    public WeaponTargetingController targetingController;

    [Header("Tooltip Position Offset")]
    public Vector3 tooltipOffset = new Vector3(2.2f, 0f, 0f); // O koľko vedľa zbrane sa zobrazí jej grid

    // --- PRE UI OBJEKTY V CANVASE ---
    public void OnPointerEnter(PointerEventData eventData) => ShowWeaponGridTooltip();
    public void OnPointerExit(PointerEventData eventData)  => HideWeaponGridTooltip();
    public void OnPointerClick(PointerEventData eventData) => SelectThisWeapon();

    // --- PRE 2D SPRITY V SCÉNE (S COLLIDEROM) ---
    private void OnMouseEnter() => ShowWeaponGridTooltip();
    private void OnMouseExit()  => HideWeaponGridTooltip();
    private void OnMouseDown()  => SelectThisWeapon();

    private void ShowWeaponGridTooltip()
    {
        if (weaponTooltipGrid == null || weaponPreset == null) return;

        weaponTooltipGrid.gameObject.SetActive(true);
        // Presunie náhľadový grid hneď vedľa tejto ikony zbrane
        weaponTooltipGrid.transform.position = transform.position + tooltipOffset;
        weaponTooltipGrid.isPlayerGrid = true; // Aby náhľadový grid nereagoval na útoky
        weaponTooltipGrid.GenerateSingleGrid(weaponPreset);
    }

    private void HideWeaponGridTooltip()
    {
        if (weaponTooltipGrid == null) return;
        weaponTooltipGrid.gameObject.SetActive(false);
    }

    private void SelectThisWeapon()
    {
        if (targetingController != null && weaponPreset != null)
        {
            targetingController.SelectWeapon(weaponPreset);
            HideWeaponGridTooltip();
        }
    }
}