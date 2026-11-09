using System.Collections.Generic;
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
    
    // --- ODKAZY NA OBRÁZKY V UPPERPART UI ---
    [SerializeField] private Image characterPortrait;
    [SerializeField] private Image[] enemyPortraits;

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
        ResolveUpperPartReferences();

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

    private void ResolveUpperPartReferences()
    {
        GameObject upperPart = GameObject.Find("UpperPart");
        if (upperPart == null) return;

        if (characterPortrait == null)
        {
            Transform playerSpriteTr = upperPart.transform.Find("PlayerSprite");
            if (playerSpriteTr != null)
            {
                characterPortrait = playerSpriteTr.GetComponent<Image>();
            }
        }

        if (enemyPortraits == null || enemyPortraits.Length == 0)
        {
            var foundNpcImages = new List<Image>();
            for (int i = 0; i < upperPart.transform.childCount; i++)
            {
                Transform child = upperPart.transform.GetChild(i);
                if (child != null && child.name.StartsWith("NPCSprite"))
                {
                    Image img = child.GetComponent<Image>();
                    if (img != null) foundNpcImages.Add(img);
                }
            }

            // Zoradíme zľava doprava podľa X pozície v UpperPart
            foundNpcImages.Sort((a, b) => a.rectTransform.anchoredPosition.x.CompareTo(b.rectTransform.anchoredPosition.x));
            enemyPortraits = foundNpcImages.ToArray();
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
        ResolveUpperPartReferences();

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

        // --- AKTUALIZÁCIA OBRÁZKOV NEPRIATEĽOV V ENCOUNTERI ---
        UpdateEnemyPortraits();
    }

    public void UpdateEnemyPortraits()
    {
        ResolveCombatReferences();
        ResolveUpperPartReferences();

        if (turnManager == null || turnManager.enemyGrid == null || enemyPortraits == null || enemyPortraits.Length == 0)
            return;

        var activeEnemies = new List<EnemyData>();
        foreach (EnemyData enemy in turnManager.enemyGrid.Enemies)
        {
            if (enemy != null) activeEnemies.Add(enemy);
        }

        for (int i = 0; i < enemyPortraits.Length; i++)
        {
            Image portrait = enemyPortraits[i];
            if (portrait == null) continue;

            if (i < activeEnemies.Count)
            {
                portrait.gameObject.SetActive(true);
                Sprite enemySprite = activeEnemies[i].GetEnemySprite();
                if (enemySprite != null)
                {
                    portrait.sprite = enemySprite;
                }
            }
            else
            {
                portrait.gameObject.SetActive(false);
            }
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
                Transform itemSlotTr = weaponSlots[i].transform.Find("ItemSlot");
                Image slotImage = itemSlotTr != null ? itemSlotTr.GetComponent<Image>() : weaponSlots[i].GetComponentInChildren<Image>();
                
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
