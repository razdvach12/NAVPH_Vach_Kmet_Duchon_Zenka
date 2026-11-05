using UnityEngine;
using TMPro;

public class CoinCounterUI : MonoBehaviour
{
    public TextMeshProUGUI coinText;

    void Start()
    {
        // Hneď pri načítaní scény nastaví správnu hodnotu
        UpdateUI();

        // Prihlásenie na odber zmien z PlayerStats (aby sa UI menilo samo pri nákupe)
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnMoneyChanged += UpdateUI;
        }
    }

    void OnDestroy()
    {
        // Odhlásenie pri zničení objektu (dôležité, aby hra nepadla pri prechode do inej scény)
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnMoneyChanged -= UpdateUI;
        }
    }

    void UpdateUI()
    {
        if (PlayerStats.Instance != null && coinText != null)
        {
            // Zoberie aktuálnu hodnotu z PlayerStats a prepíše text v UI
            coinText.text = PlayerStats.Instance.money.ToString();
        }
    }
}