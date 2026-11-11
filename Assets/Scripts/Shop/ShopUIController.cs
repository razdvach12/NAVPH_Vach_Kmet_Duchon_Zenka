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

    [Header("Obrázky pre ne-bojové scény (Shop / Chest)")]
    [SerializeField] private Sprite merchantSprite;

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

    private static Sprite cachedCircleSprite;

    private bool IsInCombat => targetingController != null && turnManager != null;

    private void Awake()
    {
        ResolveCombatReferences();
        ResolveUpperPartReferences();

#if UNITY_EDITOR
        if (merchantSprite == null)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/UI/Characters/Merchant2.png");
            foreach (var asset in assets)
            {
                if (asset is Sprite s)
                {
                    merchantSprite = s;
                    break;
                }
            }
        }
#endif

        if (turnManager != null && playerHealthGrid != null)
        {
            turnManager.playerGrid = playerHealthGrid;
        }

        if (PlayerStats.Instance != null && playerHealthGrid != null && PlayerStats.Instance.playerHealthPreset != null)
        {
            playerHealthGrid.isPlayerGrid = true;
            playerHealthGrid.SetSinglePreset(PlayerStats.Instance.playerHealthPreset);
        }

        // Nastavíme portréty podľa scény hneď v Awake (ShopScene = Merchant2, ChestScene = nikto, Encounter = nepriatelia)
        UpdateEnemyPortraits();
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
        if (weaponBtn != null && weaponBtn.enabled && weaponBtn.weaponPreset != null)
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
            foundNpcImages.Sort((b, a) => b.rectTransform.anchoredPosition.x.CompareTo(a.rectTransform.anchoredPosition.x));
            enemyPortraits = foundNpcImages.ToArray();
        }
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnMoneyChanged += UpdateMoneyText;
            PlayerStats.Instance.OnWeaponsChanged += UpdateWeaponSlots;
            PlayerStats.Instance.OnConsumablesChanged += UpdateConsumableSlots;
            PlayerStats.Instance.OnRelicsChanged += UpdateRelicSlots; 
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

        // --- AKTUALIZÁCIA OBRÁZKOV NEPRIATEĽOV / MERCHANT2 V HORNOM PÁSE ---
        UpdateEnemyPortraits();
    }

    public void UpdateEnemyPortraits(int attackingEnemyIndex = -1)
    {
        ResolveCombatReferences();
        ResolveUpperPartReferences();

        if (enemyPortraits == null || enemyPortraits.Length == 0)
            return;

        // Ak nie sme v súboji (turnManager alebo enemyGrid chýba - napr. ShopScene alebo ChestScene)
        if (turnManager == null || turnManager.enemyGrid == null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            int sceneIndex = SceneManager.GetActiveScene().buildIndex;
            bool isShop = (sceneName == "ShopScene" || sceneIndex == 2);

            for (int i = 0; i < enemyPortraits.Length; i++)
            {
                Image portrait = enemyPortraits[i];
                if (portrait == null) continue;

                SetAttackIndicator(portrait, false);

                // V ShopScene sa zobrazí iba prvý portrét s Merchant2, ostatné sa skryjú
                // V ChestScene (alebo iných scénach) sa skryjú všetky (nevykreslí sa nikto)
                if (isShop && i == 0)
                {
                    portrait.gameObject.SetActive(true);
                    if (merchantSprite != null)
                    {
                        portrait.sprite = merchantSprite;
                    }
                }
                else
                {
                    portrait.gameObject.SetActive(false);
                }
            }
            return;
        }

        // Sme v súboji (Encounter):
        // Zoznam indexov skutočných nepriateľov v encounteri (zachová priradenie k príslušnému NPCSprite slotu)
        var validEnemyIndices = new List<int>();
        for (int idx = 0; idx < turnManager.enemyGrid.Enemies.Count; idx++)
        {
            if (turnManager.enemyGrid.Enemies[idx] != null)
            {
                validEnemyIndices.Add(idx);
            }
        }

        for (int i = 0; i < enemyPortraits.Length; i++)
        {
            Image portrait = enemyPortraits[i];
            if (portrait == null) continue;

            if (i < validEnemyIndices.Count)
            {
                int enemyIndex = validEnemyIndices[i];
                EnemyData enemy = turnManager.enemyGrid.Enemies[enemyIndex];
                bool isAlive = !turnManager.enemyGrid.HasGeneratedSlots || turnManager.enemyGrid.IsEnemyAlive(enemyIndex);

                if (isAlive)
                {
                    portrait.gameObject.SetActive(true);
                    Sprite enemySprite = enemy.GetEnemySprite();
                    if (enemySprite != null)
                    {
                        portrait.sprite = enemySprite;
                    }
                    SetAttackIndicator(portrait, enemyIndex == attackingEnemyIndex);
                }
                else
                {
                    SetAttackIndicator(portrait, false);
                    portrait.gameObject.SetActive(false);
                }
            }
            else
            {
                SetAttackIndicator(portrait, false);
                portrait.gameObject.SetActive(false);
            }
        }
    }

    // Zobrazí alebo skryje červenú guličku nad portrétom práve útočiaceho nepriateľa
    private void SetAttackIndicator(Image portrait, bool active)
    {
        if (portrait == null) return;

        Transform indicatorTr = portrait.transform.Find("AttackIndicator");
        if (!active && indicatorTr == null) return;

        Image indicatorImg;
        if (indicatorTr == null)
        {
            GameObject go = new GameObject("AttackIndicator");
            go.transform.SetParent(portrait.transform, false);
            indicatorImg = go.AddComponent<Image>();
            indicatorImg.sprite = GetCircleSprite();
            indicatorImg.color = new Color(1f, 0.15f, 0.15f, 1f);
            indicatorImg.raycastTarget = false;

            RectTransform rt = indicatorImg.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 16f);
            rt.sizeDelta = new Vector2(28f, 28f);

            // Kompenzácia mierky rodičovského NPCSprite, aby gulička zostala guľatá
            Vector3 parentScale = portrait.rectTransform.localScale;
            float sx = Mathf.Abs(parentScale.x) > 0.001f ? 1f / Mathf.Abs(parentScale.x) : 1f;
            float sy = Mathf.Abs(parentScale.y) > 0.001f ? 1f / Mathf.Abs(parentScale.y) : 1f;
            rt.localScale = new Vector3(sx, sy, 1f);

            indicatorTr = go.transform;
        }

        indicatorTr.gameObject.SetActive(active);
    }

    private static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) * 0.5f;
        float radius = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cachedCircleSprite;
    }

    private void UpdateMoneyText()
    {
        if (PlayerStats.Instance != null && coinText != null)
            coinText.text = PlayerStats.Instance.money.ToString();
    }

    private void UpdateWeaponSlots()
    {
        bool isInShopScene = (SceneManager.GetActiveScene().buildIndex == 2 || SceneManager.GetActiveScene().name == "ShopScene");

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
                    slot.SetActive(true);
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

            // Všetky sloty zostávajú aktívne na svojich fixných miestach (nerozťahujú sa)
            weaponSlots[i].SetActive(true);

            Transform itemSlotTr = weaponSlots[i].transform.Find("ItemSlot");
            Image slotImage = itemSlotTr != null ? itemSlotTr.GetComponent<Image>() : weaponSlots[i].GetComponentInChildren<Image>();

            if (i < PlayerStats.Instance.unlockedWeapons.Count)
            {
                GridPreset weapon = PlayerStats.Instance.unlockedWeapons[i];

                if (itemSlotTr != null)
                {
                    itemSlotTr.gameObject.SetActive(true);
                }
                if (slotImage != null)
                {
                    slotImage.enabled = true;
                    if (weapon != null && weapon.weaponIcon != null)
                    {
                        slotImage.sprite = weapon.weaponIcon;
                    }
                }

                // V bojovej miestnosti pripojíme WeaponButtonUI pre náhľad gridu a útočenie
                if (IsInCombat)
                {
                    WeaponButtonUI weaponBtn = weaponSlots[i].GetComponent<WeaponButtonUI>();
                    if (weaponBtn == null) weaponBtn = weaponSlots[i].AddComponent<WeaponButtonUI>();

                    weaponBtn.enabled = true;
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
                // Prázdny slot: ikonka zbrane sa skryje, ale samotný rámik slotu ostáva zobrazený na pevnom mieste
                if (itemSlotTr != null)
                {
                    itemSlotTr.gameObject.SetActive(false);
                }
                else if (slotImage != null && slotImage.gameObject != weaponSlots[i] && slotImage.gameObject.name != "bg")
                {
                    slotImage.gameObject.SetActive(false);
                }

                WeaponButtonUI weaponBtn = weaponSlots[i].GetComponent<WeaponButtonUI>();
                if (weaponBtn != null)
                {
                    weaponBtn.weaponPreset = null;
                    weaponBtn.targetingController = null;
                    weaponBtn.weaponTooltipGrid = null;
                    weaponBtn.enabled = false;
                }

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
