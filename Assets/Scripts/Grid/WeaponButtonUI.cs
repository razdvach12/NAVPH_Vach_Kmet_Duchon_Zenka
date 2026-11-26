using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Weapon Preset (e.g. 3x3 Sword)")]
    public GridPreset weaponPreset;

    [Header("References")]
    public GridManager weaponTooltipGrid; // Malý GridManager vedľa zbrane na zobrazenie jej tvaru
    public WeaponTargetingController targetingController;
    [Tooltip("Štvorec (SpriteRenderer), v ktorom sa vycentruje a ohraničí náhľad zbrane (napr. PlayerWeaponBg)")]
    public SpriteRenderer previewBox;

    [Header("Preview Box Fit Settings")]
    [Tooltip("Maximálna (základná) mierka gridu zbrane v štvorci; pri väčšej zbrani sa automaticky zmenší")]
    public float maxPreviewScale = 0.5f;
    [Tooltip("Akú časť štvorca môže grid zabrať (0.82 = 82 %, zvyšok je rezerva pre rámik štvorca)")]
    [Range(0.5f, 1f)]
    public float boxInnerFillRatio = 0.82f;

    [Header("Tooltip Position Offset (Fallback ak chýba previewBox)")]
    public Vector3 tooltipOffset = new Vector3(2.2f, 0f, 0f); // O koľko vedľa stĺpca zbraní sa zobrazí jej grid

    [Tooltip("Horný slot zbraní - náhľad sa vždy zobrazí vedľa neho (horný okraj gridu = horný okraj slotu). Ak nie je nastavený, použije sa táto zbraň.")]
    public Transform tooltipAnchor;

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
        weaponTooltipGrid.isPlayerGrid = true; // Aby náhľadový grid nereagoval na útoky
        weaponTooltipGrid.SetShowMaxHpOutline(false); // Náhľad zbrane nezobrazuje obvod Max HP
        weaponTooltipGrid.GenerateSingleGrid(weaponPreset);

        if (previewBox == null)
        {
            GameObject boxGo = GameObject.Find("PlayerWeaponBg");
            if (boxGo != null) previewBox = boxGo.GetComponent<SpriteRenderer>();
        }

        if (previewBox != null)
        {
            weaponTooltipGrid.FitInsideBounds(previewBox.bounds, maxPreviewScale, boxInnerFillRatio);
            return;
        }

        // Fallback, ak v scéne nie je PlayerWeaponBg
        Transform anchor = tooltipAnchor != null ? tooltipAnchor : transform;
        PlaceGridTopAt(weaponTooltipGrid, GetTopEdgeWorld(anchor) + tooltipOffset);
    }

    // Horný stred objektu vo svete (pre UI slot horný okraj RectTransformu, inak jeho pozícia)
    public static Vector3 GetTopEdgeWorld(Transform t)
    {
        if (t is RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners); // 1 = ľavý horný, 2 = pravý horný
            return new Vector3(t.position.x, corners[1].y, t.position.z);
        }
        return t.position;
    }

    // Posunie grid tak, aby jeho horný okraj (stred) bol v danom bode (Z = 0, aby nebol schovaný za UI/pozadím)
    public static void PlaceGridTopAt(GridManager grid, Vector3 topCenter)
    {
        float halfHeight = grid.GetTopCenterWorld().y - grid.transform.position.y;
        grid.transform.position = new Vector3(topCenter.x, topCenter.y - halfHeight, 0f);
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
