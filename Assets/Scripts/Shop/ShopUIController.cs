using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

    [Header("Combat Odkazy (automaticky sa nájdu v bojovej scéne)")]
    [SerializeField] private WeaponTargetingController targetingController;
    [SerializeField] private GridManager weaponTooltipGrid;
    [SerializeField] private TurnManager turnManager;

    private bool IsInCombat => targetingController != null && turnManager != null;

    private void Awake()
    {
        ResolveCombatReferences();

        if (turnManager != null && playerHealthGrid != null)
        {
            turnManager.playerGrid = playerHealthGrid;
        }

        if (PlayerStats.Instance != null && playerHealthGrid != null && PlayerStats.Instance.playerHealthPreset != null)
        {
            playerHealthGrid.isPlayerGrid = true;
            playerHealthGrid.SetSinglePreset(PlayerStats.Instance.playerHealthPreset);
        }
    }

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

    private void Update()
    {
        // V bojovej miestnosti umožníme výber zbrane aj klávesami 1-4
        if (!IsInCombat || Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectWeaponSlot(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectWeaponSlot(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectWeaponSlot(2);
        else if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectWeaponSlot(3);
    }

    private void SelectWeaponSlot(int index)
    {
        if (weaponSlots == null || index < 0 || index >= weaponSlots.Length || weaponSlots[index] == null) return;
        if (!weaponSlots[index].activeInHierarchy) return;

        WeaponButtonUI weaponBtn = weaponSlots[index].GetComponent<WeaponButtonUI>();
        if (weaponBtn != null)
        {
            weaponBtn.SelectThisWeapon();
        }
    }

    private void ResolveCombatReferences()
    {
        if (turnManager == null)
        {
            turnManager = FindFirstObjectByType<TurnManager>();
        }

        if (targetingController == null)
        {
            targetingController = FindFirstObjectByType<WeaponTargetingController>();
        }

        if (weaponTooltipGrid == null && (turnManager != null || targetingController != null))
        {
            GridManager[] allGrids = FindObjectsByType<GridManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (GridManager gm in allGrids)
            {
                if (gm != null && gm != playerHealthGrid && gm.gameObject.name == "WeaponGrid" && gm.transform.root.gameObject.activeInHierarchy)
                {
                    weaponTooltipGrid = gm;
                    break;
                }
            }
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
        bool isInShopScene = (SceneManager.GetActiveScene().buildIndex == 2);

        if (PlayerStats.Instance == null)
        {
            if (!isInShopScene && removeWeaponButtons != null)
            {
                foreach (Button removeBtn in removeWeaponButtons)
                {
                    if (removeBtn != null) removeBtn.gameObject.SetActive(false);
                }
            }

            // Ak sa bojová scéna spustí priamo v editore bez PlayerStats, prepojíme aspoň existujúce sloty
            if (IsInCombat && weaponSlots != null)
            {
                foreach (GameObject slot in weaponSlots)
                {
                    if (slot == null) continue;
                    WeaponButtonUI weaponBtn = slot.GetComponent<WeaponButtonUI>();
                    if (weaponBtn != null)
                    {
                        weaponBtn.targetingController = targetingController;
                        weaponBtn.weaponTooltipGrid = weaponTooltipGrid;
                    }
                }
            }
            return;
        }

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

                // V bojovej miestnosti pripojíme WeaponButtonUI pre náhľad gridu a útočenie
                if (IsInCombat)
                {
                    WeaponButtonUI weaponBtn = weaponSlots[i].GetComponent<WeaponButtonUI>();
                    if (weaponBtn == null) weaponBtn = weaponSlots[i].AddComponent<WeaponButtonUI>();

                    weaponBtn.weaponPreset = weapon;
                    weaponBtn.targetingController = targetingController;
                    weaponBtn.weaponTooltipGrid = weaponTooltipGrid;
                }
                else
                {
                    WeaponButtonUI weaponBtn = weaponSlots[i].GetComponent<WeaponButtonUI>();
                    if (weaponBtn != null)
                    {
                        weaponBtn.targetingController = null;
                        weaponBtn.weaponTooltipGrid = null;
                    }
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
                ItemData potion = PlayerStats.Instance.consumables[i];
                Image slotImage = consumableSlots[i].GetComponent<Image>();
                
                if (slotImage != null && potion != null && potion.icon != null)
                {
                    slotImage.sprite = potion.icon;       
                }

                if (IsInCombat)
                {
                    ConsumableButtonUI consumableBtn = consumableSlots[i].GetComponent<ConsumableButtonUI>();
                    if (consumableBtn == null) consumableBtn = consumableSlots[i].AddComponent<ConsumableButtonUI>();

                    consumableBtn.consumable = potion as ConsumableData;
                    consumableBtn.turnManager = turnManager;
                    consumableBtn.Refresh();
                }
                else
                {
                    ConsumableButtonUI consumableBtn = consumableSlots[i].GetComponent<ConsumableButtonUI>();
                    if (consumableBtn != null)
                    {
                        consumableBtn.turnManager = null;
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
