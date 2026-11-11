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

    private void OnDisable() => HideWeaponGridTooltip();

    private void ShowWeaponGridTooltip()
    {
        // Cena zbrane sa ukáže pri stamine hráča
        if (targetingController != null && targetingController.turnManager != null && weaponPreset != null)
        {
            targetingController.turnManager.ShowStaminaCost(targetingController.turnManager.GetStaminaCost(weaponPreset));
        }

        if (weaponTooltipGrid == null || weaponPreset == null) return;

        weaponTooltipGrid.gameObject.SetActive(true);
        // Presunie náhľadový grid hneď vedľa tejto ikony zbrane (Z = 0, aby nebol schovaný za UI/pozadím)
        Vector3 tooltipPos = transform.position + tooltipOffset;
        tooltipPos.z = 0f;
        weaponTooltipGrid.transform.position = tooltipPos;
        weaponTooltipGrid.isPlayerGrid = true; // Aby náhľadový grid nereagoval na útoky
        weaponTooltipGrid.SetShowMaxHpOutline(false); // Náhľad zbrane nezobrazuje obvod Max HP
        weaponTooltipGrid.GenerateSingleGrid(weaponPreset);
    }

    private void HideWeaponGridTooltip()
    {
        if (targetingController != null && targetingController.turnManager != null)
        {
            targetingController.turnManager.HideStaminaCost();
        }

        if (weaponTooltipGrid == null) return;
        weaponTooltipGrid.gameObject.SetActive(false);
    }

    // Volá sa po kliknutí na ikonu alebo stlačení klávesy slotu (1-4)
    public void SelectThisWeapon()
    {
        if (targetingController != null && weaponPreset != null)
        {
            targetingController.SelectWeapon(weaponPreset);
            HideWeaponGridTooltip();
        }
    }
}
