using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopUIController : MonoBehaviour
{
    [Header("UI Odkazy")]
    [SerializeField] private TextMeshProUGUI coinText;       
    [SerializeField] private GridManager playerHealthGrid;   

    [Header("Zbrane hráča v UI (Ikonky / Sloty)")]
    [SerializeField] private GameObject[] weaponSlots;       

    [Header("Potiony hráča v UI (Ikonky / Sloty)")]
    [SerializeField] private GameObject[] consumableSlots; 

    [Header("Relikvie hráča v UI (Ikonky / Sloty)")]
    [SerializeField] private GameObject[] relicSlots; // <-- SEM DÁŠ 4 SLOTY PRE RELIKVIE

    private void Start()
    {
        UpdateShopUI();

        // Zavesenie na signály
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnMoneyChanged += UpdateMoneyText;
            PlayerStats.Instance.OnWeaponsChanged += UpdateWeaponSlots;
            PlayerStats.Instance.OnConsumablesChanged += UpdateConsumableSlots;
            PlayerStats.Instance.OnRelicsChanged += UpdateRelicSlots; // Zavesenie relikvií
        }
    }

    private void OnDestroy()
    {
        // Odhlásenie pri zničení scény
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnMoneyChanged -= UpdateMoneyText;
            PlayerStats.Instance.OnWeaponsChanged -= UpdateWeaponSlots;
            PlayerStats.Instance.OnConsumablesChanged -= UpdateConsumableSlots;
            PlayerStats.Instance.OnRelicsChanged -= UpdateRelicSlots; 
        }
    }

    public void UpdateShopUI()
    {
        UpdateMoneyText();
        UpdateWeaponSlots();
        UpdateConsumableSlots();
        UpdateRelicSlots();

        if (PlayerStats.Instance != null && playerHealthGrid != null && PlayerStats.Instance.playerHealthPreset != null)
        {
            playerHealthGrid.isPlayerGrid = true;
            playerHealthGrid.GenerateSingleGrid(PlayerStats.Instance.playerHealthPreset);
        }
    }

    private void UpdateMoneyText()
    {
        if (PlayerStats.Instance != null && coinText != null)
        {
            coinText.text = PlayerStats.Instance.money.ToString();
        }
    }

    private void UpdateWeaponSlots()
    {
        if (PlayerStats.Instance == null) return;

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] == null) continue;

            if (i < PlayerStats.Instance.unlockedWeapons.Count)
            {
                weaponSlots[i].SetActive(true);
                GridPreset weapon = PlayerStats.Instance.unlockedWeapons[i];
                Image slotImage = weaponSlots[i].GetComponent<Image>();
                
                if (slotImage != null)
                {
                    if (weapon != null && weapon.weaponIcon != null)
                    {
                        slotImage.gameObject.SetActive(true);
                        slotImage.sprite = weapon.weaponIcon; 
                    }
                    else
                    {
                        slotImage.sprite = null;
                    }
                }
            }
            else
            {
                weaponSlots[i].SetActive(false);
            }
        }
    }

    private void UpdateConsumableSlots()
    {
        if (PlayerStats.Instance == null) return;

        for (int i = 0; i < consumableSlots.Length; i++)
        {
            if (consumableSlots[i] == null) continue;

            Image slotImage = consumableSlots[i].GetComponent<Image>();
            
            if (slotImage != null)
            {
                if (i < PlayerStats.Instance.consumables.Count)
                {
                    ItemData potion = PlayerStats.Instance.consumables[i];
                    if (potion != null && potion.icon != null)
                    {
                        slotImage.gameObject.SetActive(true); 
                        slotImage.sprite = potion.icon;       
                    }
                }
                else
                {
                    slotImage.sprite = null;
                }
            }
        }
    }

    // --- TÁTO METÓDA VYKRESLÍ RELIKVIE ---
    private void UpdateRelicSlots()
    {
        if (PlayerStats.Instance == null) return;

        for (int i = 0; i < relicSlots.Length; i++)
        {
            if (relicSlots[i] == null) continue;

            Image slotImage = relicSlots[i].GetComponent<Image>();
            
            if (slotImage != null)
            {
                if (i < PlayerStats.Instance.relics.Count)
                {
                    // Hráč MÁ relikviu na tejto pozícii
                    ItemData relic = PlayerStats.Instance.relics[i];
                    if (relic != null && relic.icon != null)
                    {
                        slotImage.gameObject.SetActive(true); 
                        slotImage.sprite = relic.icon;       
                    }
                }
                else
                {
                    // Hráč NEMÁ relikviu (slot je prázdny)
                    slotImage.sprite = null;
                }
            }
        }
    }
}