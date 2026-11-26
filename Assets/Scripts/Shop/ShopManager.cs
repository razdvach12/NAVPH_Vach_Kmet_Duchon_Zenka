using UnityEngine;
using UnityEngine.UI; // Potrebné kvôli práci s komponentom Button
using System.Collections.Generic;

public class ShopManager : MonoBehaviour 
{
    public static ShopManager Instance;

    public enum ShopSection
    {
        Weapons,
        Consumables,
        Relics,
        Upgrades
    }

    [Header("Databáza všetkých predmetov")]
    public List<ItemData> allWeaponItems;
    public List<ItemData> allConsumableItems;
    public List<ItemData> allRelicItems; 
    public List<ItemData> allUpgradeItems; 

    [Header("UI Sloty v obchode")]
    public List<ShopSlotUI> weaponSlots;
    public List<ShopSlotUI> consumableSlots;
    public List<ShopSlotUI> relicSlots; 
    public List<ShopSlotUI> upgradeSlots; 

    [Header("Objekty sekcií (prázdne = rodič slotov danej sekcie)")]
    public GameObject weaponSectionObject;
    public GameObject consumableSectionObject;
    public GameObject relicSectionObject;
    public GameObject upgradeSectionObject;

    [Header("Texty pri vypredaní (UI)")]
    public GameObject emptyWeaponsText;
    public GameObject emptyConsumablesText;
    public GameObject emptyRelicsText;
    public GameObject emptyUpgradesText;

    // --- Tlačidlá jednotlivých sekcií pre prepínanie ---
    [Header("Tlačidlá sekcií v menu obchodu (prázdne = nájdu sa podľa mena)")]
    public Button weaponTabButton;
    public Button consumableTabButton;
    public Button relicTabButton;
    public Button upgradeTabButton;

    [Header("Reroll (znovu vygeneruje iba práve otvorenú sekciu)")]
    public Button rerollButton;
    [Tooltip("Cena za reroll v goldoch (0 = zadarmo)")]
    public int rerollCost = 0;
    [Tooltip("Maximálny počet rerollov za jednu návštevu obchodu")]
    [Min(0)] public int maxRerolls = 1;

    private int rerollsUsed = 0;

    private ShopSection currentSection = ShopSection.Weapons;
    private bool wasWeaponsEmpty;
    private bool wasConsumablesEmpty;
    private bool wasRelicsEmpty;
    private bool wasUpgradesEmpty;

    void Awake()
    {
        Instance = this;
        RewardTargetingController.EnsureInstance();
    }


    void Start()
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.RecordShopVisited();

        ResolveMissingReferences();

        // Automaticky naviažeme prepínanie sekcií na tlačidlá v obchode.
        // Runtime listenery sa volajú až po tých nastavených v Inspectore, takže výsledná viditeľnosť sekcií je vždy podľa kódu.
        if (weaponTabButton != null)
            weaponTabButton.onClick.AddListener(() => SelectSection(ShopSection.Weapons));
        if (consumableTabButton != null)
            consumableTabButton.onClick.AddListener(() => SelectSection(ShopSection.Consumables));
        if (relicTabButton != null)
            relicTabButton.onClick.AddListener(() => SelectSection(ShopSection.Relics));
        if (upgradeTabButton != null)
            upgradeTabButton.onClick.AddListener(() => SelectSection(ShopSection.Upgrades));
        if (rerollButton != null)
            rerollButton.onClick.AddListener(RerollCurrentSection);

        if (MusicManager.Instance != null) {
            MusicManager.Instance.PlayMusic("ShopBackground");
        }

        DetectInitialActiveSection();
        GenerateShopStock();
        ApplySectionVisibility();
    }

    // Volajte túto metódu vždy, keď sa niečo v obchode kúpi (napr. zo ShopSlotUI)
    void Update()
    {
        // Preventívne priebežne kontrolujeme stav sekcií (prípadne to môžete volať len priamo pri nákupe)
        CheckIfSectionsAreEmpty();
    }

    // --- DOHĽADANIE CHÝBAJÚCICH ODKAZOV (aby nebolo nutné všetko ručne priraďovať v Inspectore) ---
    private void ResolveMissingReferences()
    {
        if (weaponSectionObject == null) weaponSectionObject = GetSectionObjectFromSlots(weaponSlots);
        if (consumableSectionObject == null) consumableSectionObject = GetSectionObjectFromSlots(consumableSlots);
        if (relicSectionObject == null) relicSectionObject = GetSectionObjectFromSlots(relicSlots);
        if (upgradeSectionObject == null) upgradeSectionObject = GetSectionObjectFromSlots(upgradeSlots);

        if (weaponTabButton == null) weaponTabButton = FindButtonByName("WeaponsButton");
        if (consumableTabButton == null) consumableTabButton = FindButtonByName("PotionsButton");
        if (relicTabButton == null) relicTabButton = FindButtonByName("RelicsButton");
        if (upgradeTabButton == null) upgradeTabButton = FindButtonByName("UpgradesButton");
        if (rerollButton == null) rerollButton = FindButtonByName("RerollButton");
    }

    private static GameObject GetSectionObjectFromSlots(List<ShopSlotUI> slots)
    {
        if (slots == null) return null;
        foreach (ShopSlotUI slot in slots)
        {
            if (slot != null && slot.transform.parent != null)
                return slot.transform.parent.gameObject;
        }
        return null;
    }

    // Nájde Button na objekte s daným menom alebo v jeho deťoch (aj neaktívnych)
    private static Button FindButtonByName(string objectName)
    {
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            if (t == null || t.name != objectName) continue;
            Button btn = t.GetComponentInChildren<Button>(true);
            if (btn != null) return btn;
        }
        return null;
    }

    public void SelectSection(ShopSection section)
    {
        if (RewardTargetingController.Instance != null && RewardTargetingController.Instance.IsTargeting)
        {
            RewardTargetingController.Instance.CancelTargeting();
        }
        currentSection = section;
        ApplySectionVisibility();
        CheckIfSectionsAreEmpty();
    }

    public void OpenWeaponsSection() => SelectSection(ShopSection.Weapons);
    public void OpenConsumablesSection() => SelectSection(ShopSection.Consumables);
    public void OpenRelicsSection() => SelectSection(ShopSection.Relics);
    public void OpenUpgradesSection() => SelectSection(ShopSection.Upgrades);

    // Zobrazí iba práve otvorenú sekciu
    private void ApplySectionVisibility()
    {
        if (weaponSectionObject != null) weaponSectionObject.SetActive(currentSection == ShopSection.Weapons);
        if (consumableSectionObject != null) consumableSectionObject.SetActive(currentSection == ShopSection.Consumables);
        if (relicSectionObject != null) relicSectionObject.SetActive(currentSection == ShopSection.Relics);
        if (upgradeSectionObject != null) upgradeSectionObject.SetActive(currentSection == ShopSection.Upgrades);
    }

    private void DetectInitialActiveSection()
    {
        if (IsSectionHierarchyActive(weaponSlots))
            currentSection = ShopSection.Weapons;
        else if (IsSectionHierarchyActive(consumableSlots))
            currentSection = ShopSection.Consumables;
        else if (IsSectionHierarchyActive(relicSlots))
            currentSection = ShopSection.Relics;
        else if (IsSectionHierarchyActive(upgradeSlots))
            currentSection = ShopSection.Upgrades;
    }

    private bool IsSectionHierarchyActive(List<ShopSlotUI> slots)
    {
        if (slots == null) return false;
        foreach (ShopSlotUI slot in slots)
        {
            if (slot != null && slot.transform.parent != null && slot.transform.parent.gameObject.activeInHierarchy)
                return true;
        }
        return false;
    }

    public void GenerateShopStock() 
    {
        FillSlotsRandomly(allWeaponItems, weaponSlots);
        FillSlotsRandomly(allConsumableItems, consumableSlots);
        FillSlotsRandomly(allRelicItems, relicSlots);
        FillSlotsRandomly(allUpgradeItems, upgradeSlots);

        wasWeaponsEmpty = IsSectionEmpty(weaponSlots);
        wasConsumablesEmpty = IsSectionEmpty(consumableSlots);
        wasRelicsEmpty = IsSectionEmpty(relicSlots);
        wasUpgradesEmpty = IsSectionEmpty(upgradeSlots);

        rerollsUsed = 0;
        UpdateRerollButtonState();
        CheckIfSectionsAreEmpty();
    }

    private void UpdateRerollButtonState()
    {
        if (rerollButton != null)
            rerollButton.interactable = rerollsUsed < maxRerolls;
    }

    // --- REROLL: nanovo vygeneruje iba práve otvorenú sekciu, ostatné ostanú bez zmeny ---
    public void RerollCurrentSection()
    {
        if (rerollsUsed >= maxRerolls)
        {
            Debug.Log("Reroll už bol v tomto obchode použitý!");
            UpdateRerollButtonState();
            return;
        }

        if (RewardTargetingController.Instance != null && RewardTargetingController.Instance.IsTargeting)
        {
            RewardTargetingController.Instance.CancelTargeting();
        }

        if (rerollCost > 0)
        {
            PlayerStats stats = PlayerStats.EnsureInstance();
            if (!stats.TrySpendMoney(rerollCost))
            {
                Debug.Log("Nemáš dosť goldov na reroll!");
                ApplySectionVisibility();
                CheckIfSectionsAreEmpty();
                return;
            }
        }

        rerollsUsed++;
        UpdateRerollButtonState();

        switch (currentSection)
        {
            case ShopSection.Weapons:
                RerollSlots(allWeaponItems, weaponSlots);
                wasWeaponsEmpty = IsSectionEmpty(weaponSlots);
                break;
            case ShopSection.Consumables:
                RerollSlots(allConsumableItems, consumableSlots);
                wasConsumablesEmpty = IsSectionEmpty(consumableSlots);
                break;
            case ShopSection.Relics:
                RerollSlots(allRelicItems, relicSlots);
                wasRelicsEmpty = IsSectionEmpty(relicSlots);
                break;
            case ShopSection.Upgrades:
                RerollSlots(allUpgradeItems, upgradeSlots);
                wasUpgradesEmpty = IsSectionEmpty(upgradeSlots);
                break;
        }

        // Tlačidlo môže mať v Inspectore nastavené aj prepínanie sekcií - vrátime zobrazenie na aktuálnu sekciu
        ApplySectionVisibility();
        CheckIfSectionsAreEmpty();
    }

    // Pri rerolle uprednostníme predmety, ktoré v sekcii práve neboli (ak ich je v databáze dosť)
    void RerollSlots(List<ItemData> sourceDatabase, List<ShopSlotUI> targetSlots)
    {
        if (targetSlots == null) return;

        if (sourceDatabase == null || sourceDatabase.Count == 0)
        {
            FillSlotsRandomly(sourceDatabase, targetSlots);
            return;
        }

        var currentItems = new HashSet<ItemData>();
        foreach (ShopSlotUI slot in targetSlots)
        {
            if (slot != null && slot.assignedItem != null) currentItems.Add(slot.assignedItem);
        }

        var freshItems = new List<ItemData>();
        var oldItems = new List<ItemData>();
        foreach (ItemData item in sourceDatabase)
        {
            if (item == null) continue;
            if (currentItems.Contains(item)) oldItems.Add(item);
            else freshItems.Add(item);
        }

        Shuffle(freshItems);
        Shuffle(oldItems);

        // Najprv nové predmety, až potom (ak nové nestačia) tie, ktoré tam už boli
        var ordered = new List<ItemData>(freshItems);
        ordered.AddRange(oldItems);

        int index = 0;
        foreach (ShopSlotUI slot in targetSlots)
        {
            if (slot == null) continue;
            slot.SetItem(index < ordered.Count ? ordered[index] : null);
            index++;
        }

        // Aby sa nové predmety nezobrazovali vždy v rovnakom poradí (nové vľavo, staré vpravo)
        ShuffleSlotItems(targetSlots);
    }

    private static void ShuffleSlotItems(List<ShopSlotUI> slots)
    {
        var items = new List<ItemData>();
        var validSlots = new List<ShopSlotUI>();
        foreach (ShopSlotUI slot in slots)
        {
            if (slot == null) continue;
            validSlots.Add(slot);
            items.Add(slot.assignedItem);
        }

        Shuffle(items);
        for (int i = 0; i < validSlots.Count; i++)
        {
            validSlots[i].SetItem(items[i]);
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void FillSlotsRandomly(List<ItemData> sourceDatabase, List<ShopSlotUI> targetSlots) 
    {
        if (targetSlots == null) return;

        if (sourceDatabase == null || sourceDatabase.Count == 0) 
        {
            foreach(var slot in targetSlots) if (slot != null) slot.SetItem(null);
            return;
        }

        List<ItemData> availableItems = new List<ItemData>(sourceDatabase);
        availableItems.RemoveAll(item => item == null);

        foreach (ShopSlotUI slot in targetSlots) 
        {
            if (slot == null) continue;

            if (availableItems.Count > 0) 
            {
                int randomIndex = Random.Range(0, availableItems.Count);
                ItemData chosenItem = availableItems[randomIndex];
                slot.SetItem(chosenItem);
                availableItems.RemoveAt(randomIndex);
            } 
            else 
            {
                slot.SetItem(null);
            }
        }
    }

    // --- KONTROLA VYPREDANIA A ZNEFUNKČNENIE TLAČIDIEL ---
    public void CheckIfSectionsAreEmpty()
    {
        // 1. Sekcia Zbraní
        bool weaponsEmpty = IsSectionEmpty(weaponSlots);
        if (!wasWeaponsEmpty && weaponsEmpty)
            currentSection = ShopSection.Weapons;
        wasWeaponsEmpty = weaponsEmpty;

        if (weaponTabButton != null) 
            weaponTabButton.interactable = !weaponsEmpty; // Ak je vypredané, interactor vypneme (false)

        // 2. Sekcia Spotrebného materiálu
        bool consumablesEmpty = IsSectionEmpty(consumableSlots);
        if (!wasConsumablesEmpty && consumablesEmpty)
            currentSection = ShopSection.Consumables;
        wasConsumablesEmpty = consumablesEmpty;

        if (consumableTabButton != null) 
            consumableTabButton.interactable = !consumablesEmpty;

        // 3. Sekcia Relikvií
        bool relicsEmpty = IsSectionEmpty(relicSlots);
        if (!wasRelicsEmpty && relicsEmpty)
            currentSection = ShopSection.Relics;
        wasRelicsEmpty = relicsEmpty;

        if (relicTabButton != null) 
            relicTabButton.interactable = !relicsEmpty;

        // 4. Sekcia Upgradov
        bool upgradesEmpty = IsSectionEmpty(upgradeSlots);
        if (!wasUpgradesEmpty && upgradesEmpty)
            currentSection = ShopSection.Upgrades;
        wasUpgradesEmpty = upgradesEmpty;

        if (upgradeTabButton != null)
            upgradeTabButton.interactable = !upgradesEmpty;

        // Najprv skryjeme všetky texty o vypredaní
        if (emptyWeaponsText != null) 
            emptyWeaponsText.SetActive(false);
        if (emptyConsumablesText != null) 
            emptyConsumablesText.SetActive(false);
        if (emptyRelicsText != null) 
            emptyRelicsText.SetActive(false);
        if (emptyUpgradesText != null)
            emptyUpgradesText.SetActive(false);

        // Zobrazíme príslušný text iba vtedy, ak je hráč práve v danej vypredanej sekcii
        if (currentSection == ShopSection.Weapons && weaponsEmpty && emptyWeaponsText != null)
            emptyWeaponsText.SetActive(true);
        else if (currentSection == ShopSection.Consumables && consumablesEmpty && emptyConsumablesText != null)
            emptyConsumablesText.SetActive(true);
        else if (currentSection == ShopSection.Relics && relicsEmpty && emptyRelicsText != null)
            emptyRelicsText.SetActive(true);
        else if (currentSection == ShopSection.Upgrades && upgradesEmpty && emptyUpgradesText != null)
            emptyUpgradesText.SetActive(true);
    }

    private bool IsSectionEmpty(List<ShopSlotUI> slots)
    {
        if (slots == null || slots.Count == 0) return true;
        
        foreach (ShopSlotUI slot in slots)
        {
            // Ak slot existuje, má priradený predmet A ZÁROVEŇ ešte nie je kupujúci/predmet minutý (predpokladáme, že po nákupe sa item zmaže alebo nastaví na null)
            if (slot != null && slot.assignedItem != null) 
            {
                return false; 
            }
        }
        return true; 
    }
}
