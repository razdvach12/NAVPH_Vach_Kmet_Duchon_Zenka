using UnityEngine;

// Popup po prehre - oznámi prehru a tlačidlom vráti hráča do menu.
// Skript patrí na objekt DefeatCanvas (v scéne môže byť vypnutý), samotný panel je na začiatku vypnutý.
public class DefeatPopupUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Panel popupu (DefeatPopUp) - na začiatku vypnutý, zapne sa pri prehre")]
    [SerializeField] private GameObject popup;

    [Tooltip("Ak nie je priradený, nájde sa automaticky")]
    [SerializeField] private TurnManager turnManager;

    private void Awake()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (popup != null) popup.SetActive(false);
    }

    // Zavolá TurnManager pri prehre
    public void Show()
    {
        // Canvas so skriptom môže byť v scéne vypnutý - zapneme ho spolu s popupom
        gameObject.SetActive(true);
        if (popup != null) popup.SetActive(true);
    }

    // Napojiť na On Click tlačidla "Menu" - vráti hráča do menu
    public void OnClickMenu()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (turnManager != null) turnManager.ReturnToMenuAfterDefeat();
    }
}
