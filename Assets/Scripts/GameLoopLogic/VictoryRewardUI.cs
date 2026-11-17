using TMPro;
using UnityEngine;


public class VictoryRewardUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Panel popupu (VictoryPopup) - na začiatku vypnutý, zapne sa pri výhre")]
    [SerializeField] private GameObject popup;

    [Tooltip("Voliteľné - text, do ktorého sa vypíše, koľko zlata hráč získal")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Tooltip("Ak nie je priradený, nájde sa automaticky")]
    [SerializeField] private TurnManager turnManager;

    private void Awake()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        if (popup != null) popup.SetActive(false);
    }


    public void Show(int goldReward)
    {
        gameObject.SetActive(true);

        if (goldText != null) goldText.text = $"+{goldReward} gold pieces";
        if (popup != null) popup.SetActive(true);
    }

    // Napojiť na On Click tlačidla "Continue" - vráti hráča na mapu
    public void OnClickContinue()
    {
        if (ResolveTurnManager()) turnManager.ContinueAfterVictory();
    }

    private bool ResolveTurnManager()
    {
        if (turnManager == null) turnManager = FindAnyObjectByType<TurnManager>();
        return turnManager != null;
    }
}
