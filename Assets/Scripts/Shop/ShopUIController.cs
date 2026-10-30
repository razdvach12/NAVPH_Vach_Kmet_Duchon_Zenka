using UnityEngine;
using UnityEngine.UI; // Dôležité pre prácu s Image komponentom
using TMPro;

public class ShopUIController : MonoBehaviour
{
    [Header("UI Odkazy")]
    [SerializeField] private TextMeshProUGUI coinText;       
    [SerializeField] private GridManager playerHealthGrid;   

    [Header("Zbrane hráča v UI (Ikonky / Sloty)")]
    [Tooltip("Sem pretiahni sloty (Circle alebo objekty, ktoré majú na sebe Image komponent pre zbraň)")]
    [SerializeField] private GameObject[] weaponSlots;       

    private void Start()
    {
        UpdateShopUI();
    }

    public void UpdateShopUI()
    {
        if (PlayerStats.Instance != null)
        {
            if (coinText != null)
            {
                coinText.text = PlayerStats.Instance.money.ToString();
            }

            if (playerHealthGrid != null && PlayerStats.Instance.playerHealthPreset != null)
            {
                playerHealthGrid.isPlayerGrid = true;
                playerHealthGrid.GenerateSingleGrid(PlayerStats.Instance.playerHealthPreset);
            }

            UpdateWeaponSlots();
        }
    }

    private void UpdateWeaponSlots()
    {
        if (PlayerStats.Instance == null) return;

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] == null) continue;

            // Ak má hráč na tejto pozícií zbraň
            if (i < PlayerStats.Instance.unlockedWeapons.Count)
            {
                weaponSlots[i].SetActive(true);

                // Získame GridPreset zbrane z PlayerStats
                GridPreset weapon = PlayerStats.Instance.unlockedWeapons[i];

                // Nájdeme Image komponent na tomto slote (alebo v jeho deťoch)
                Image slotImage = weaponSlots[i].GetComponent<Image>();
                if (slotImage != null)
                {
                    if (weapon.weaponIcon != null)
                    {
                        slotImage.gameObject.SetActive(true);
                        slotImage.sprite = weapon.weaponIcon; // <-- Toto natiahne tvoju novú ikonku do UI!
                    }
                    else
                    {
                        slotImage.sprite = null;
                    }
                }
            }
            else
            {
                // Ak hráč nemá toľko zbraní, slot schováme
                weaponSlots[i].SetActive(false);
            }
        }
    }
}