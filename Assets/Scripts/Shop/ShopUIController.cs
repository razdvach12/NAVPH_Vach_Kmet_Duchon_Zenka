using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; 

public class ShopUIController : MonoBehaviour
{
    [Header("UI Odkazy")]
    [SerializeField] private TextMeshProUGUI coinText;       
    [SerializeField] private GridManager playerHealthGrid;   
    
    // --- ODKAZ NA OBRÁZOK V UI ---
    [SerializeField] private Image characterPortrait;

    [Header("Zbrane hráča v UI (Ikonky / Sloty)")]
    [SerializeField] private GameObject[] weaponSlots;       
    [SerializeField] private Button[] removeWeaponButtons;

    [Header("Potiony hráča v UI (Ikonky / Sloty)")]
    [SerializeField] private GameObject[] consumableSlots; 

    [Header("Relikvie hráča v UI (Ikonky / Sloty)")]
    [SerializeField] private GameObject[] relicSlots; 

    private void Start()
    {
        UpdateShopUI();

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnMoneyChanged += UpdateMoneyText;
            PlayerStats.Instance.OnWeaponsChanged += UpdateWeaponSlots;
            PlayerStats.Instance.OnConsumablesChanged += UpdateConsumableSlots;
            PlayerStats.Instance.OnRelicsChanged += UpdateRelicSlots; 
        }
    }

    private void OnDestroy()
    {
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

        // --- AKTUALIZÁCIA OBRÁZKA POSTAVY ---
        if (PlayerStats.Instance != null && characterPortrait != null && PlayerStats.Instance.playerSprite != null)
        {
            characterPortrait.sprite = PlayerStats.Instance.playerSprite;
        }
    }

    private void UpdateMoneyText()
    {
        if (PlayerStats.Instance != null && coinText != null)
            coinText.text = PlayerStats.Instance.money.ToString();
    }

    private void UpdateWeaponSlots()
    {
        if (PlayerStats.Instance == null) return;

        bool isInShopScene = (SceneManager.GetActiveScene().buildIndex == 2);

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] == null) continue;

            if (i < PlayerStats.Instance.unlockedWeapons.Count)
            {
                weaponSlots[i].SetActive(true); 
                GridPreset weapon = PlayerStats.Instance.unlockedWeapons[i];
                Image slotImage = weaponSlots[i].GetComponent<Image>();
                
                if (slotImage != null && weapon != null && weapon.weaponIcon != null)
                {
                    slotImage.sprite = weapon.weaponIcon; 
                }

                if (removeWeaponButtons != null && i < removeWeaponButtons.Length && removeWeaponButtons[i] != null)
                {
                    if (isInShopScene) 
                    {
                        removeWeaponButtons[i].gameObject.SetActive(true); 
                        removeWeaponButtons[i].onClick.RemoveAllListeners();
                        int slotIndex = i; 
                        removeWeaponButtons[i].onClick.AddListener(() => PlayerStats.Instance.RemoveWeapon(slotIndex));
                    }
                    else
                    {
                        removeWeaponButtons[i].gameObject.SetActive(false); 
                    }
                }
            }
            else
            {
                weaponSlots[i].SetActive(false); 
                
                if (removeWeaponButtons != null && i < removeWeaponButtons.Length && removeWeaponButtons[i] != null)
                {
                    removeWeaponButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    private void UpdateConsumableSlots()
    {
        if (PlayerStats.Instance == null) return;
        for (int i = 0; i < consumableSlots.Length; i++)
        {
            if (consumableSlots[i] == null) continue;
            
            if (i < PlayerStats.Instance.consumables.Count)
            {
                consumableSlots[i].SetActive(true); 
                Image slotImage = consumableSlots[i].GetComponent<Image>();
                
                if (slotImage != null)
                {
                    ItemData potion = PlayerStats.Instance.consumables[i];
                    if (potion != null && potion.icon != null)
                    {
                        slotImage.sprite = potion.icon;       
                    }
                }
            }
            else 
            {
                consumableSlots[i].SetActive(false); 
            }
        }
    }

    private void UpdateRelicSlots()
    {
        if (PlayerStats.Instance == null) return;
        for (int i = 0; i < relicSlots.Length; i++)
        {
            if (relicSlots[i] == null) continue;
            
            if (i < PlayerStats.Instance.relics.Count)
            {
                relicSlots[i].SetActive(true); 
                Image slotImage = relicSlots[i].GetComponent<Image>();
                
                if (slotImage != null)
                {
                    ItemData relic = PlayerStats.Instance.relics[i];
                    if (relic != null && relic.icon != null)
                    {
                        slotImage.sprite = relic.icon;       
                    }
                }
            }
            else 
            {
                relicSlots[i].SetActive(false); 
            }
        }
    }
}