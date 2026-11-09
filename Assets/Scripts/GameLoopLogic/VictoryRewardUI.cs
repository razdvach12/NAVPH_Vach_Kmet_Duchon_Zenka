using TMPro;
using UnityEngine;

// Popup po výhre - hráč si vyberie, či sa vylieči na max HP, alebo zoberie zlato za nepriateľov.
// Skript patrí na objekt, ktorý je stále zapnutý (napr. VictoryCanvas), samotný panel je na začiatku vypnutý.
public class VictoryRewardUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Panel popupu (VictoryPopup) - na začiatku vypnutý, zapne sa pri výhre")]
    [SerializeField] private GameObject popup;

    [Tooltip("Voliteľné - text, do ktorého sa vypíše, koľko zlata hráč môže získať")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Tooltip("Ak nie je priradený, nájde sa automaticky")]
    [SerializeField] private TurnManager turnManager;

    private void Awake()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (popup != null) popup.SetActive(false);
    }

    // Zavolá TurnManager pri výhre
    public void Show(int goldReward)
    {
        // Canvas so skriptom môže byť v scéne vypnutý - zapneme ho spolu s popupom
        gameObject.SetActive(true);

        if (goldText != null) goldText.text = $"+{goldReward} zlata";
        if (popup != null) popup.SetActive(true);
    }

    // Napojiť na On Click tlačidla "Vyliečiť"
    public void OnClickHeal()
    {
        if (ResolveTurnManager()) turnManager.ChooseHealReward();
    }

    // Napojiť na On Click tlačidla "Zlato"
    public void OnClickGold()
    {
        if (ResolveTurnManager()) turnManager.ChooseGoldReward();
    }

    private bool ResolveTurnManager()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        return turnManager != null;
    }
}
