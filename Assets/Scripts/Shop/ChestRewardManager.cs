using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum ChestRewardType
{
    AddMaxHpCell,
    ChangeElementLava,
    ChangeElementIce,
    ChangeElementNature,
    ChangeElementRock,
    Relic
}

public class ChestRewardManager : MonoBehaviour
{
    public static ChestRewardManager Instance { get; private set; }

    [Header("UI Referencie (ak nie sú priradené, nájdu sa automaticky)")]
    public Transform treasureSection;
    public List<ShopSlotUI> rewardSlots = new List<ShopSlotUI>();
    public Button leaveButton;

    [Header("Databáza odmien (HP / Element Upgrady a Relikvie)")]
    public List<ItemData> allUpgradeItems = new List<ItemData>();
    public List<ItemData> allRelicItems = new List<ItemData>();
    public List<Sprite> additionalRelicSprites = new List<Sprite>();

    [Header("Nastavenia výberu")]
    [Tooltip("Ak je zapnuté, po vybratí 1 odmeny z truhlice zmiznú zvyšné 2")]
    public bool singleChoiceOnly = false;

    private readonly List<ItemData> runtimeCreatedItems = new List<ItemData>();

    private void Awake()
    {
        Instance = this;
        RewardTargetingController.EnsureInstance();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        foreach (ItemData item in runtimeCreatedItems)
        {
            if (item != null) Destroy(item);
        }
        runtimeCreatedItems.Clear();
    }

    private void Start()
    {
        SetupTreasureUI();
        GenerateChestRewards();
    }

    private void SetupTreasureUI()
    {
        if (treasureSection == null)
        {
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas c in canvases)
            {
                Transform found = c.transform.Find("TreasureSection");
                if (found != null)
                {
                    treasureSection = found;
                    break;
                }
            }
        }

        if (treasureSection != null)
        {
            if (rewardSlots == null) rewardSlots = new List<ShopSlotUI>();
            rewardSlots.RemoveAll(s => s == null);

            if (rewardSlots.Count == 0)
            {
                ShopSlotUI[] existing = treasureSection.GetComponentsInChildren<ShopSlotUI>(true);
                rewardSlots.AddRange(existing);
            }

            // Ak sú v TreasureSection len 2 sloty (Relic1, Relic2), naklonujeme 3. slot (Relic3)
            while (rewardSlots.Count > 0 && rewardSlots.Count < 3)
            {
                ShopSlotUI template = rewardSlots[rewardSlots.Count - 1];
                GameObject cloneObj = Instantiate(template.gameObject, treasureSection);
                cloneObj.name = $"Relic{rewardSlots.Count + 1}";
                ShopSlotUI cloneSlot = cloneObj.GetComponent<ShopSlotUI>();
                rewardSlots.Add(cloneSlot);
            }

            // Zarovnáme 3 sloty vedľa seba rovnako ako v ShopScene (0, 349, 699)
            float[] xPositions = { 0f, 349f, 699f };
            for (int i = 0; i < rewardSlots.Count; i++)
            {
                ShopSlotUI slot = rewardSlots[i];
                if (slot == null) continue;

                slot.isFreeReward = true;

                RectTransform rt = slot.GetComponent<RectTransform>();
                if (rt != null && i < xPositions.Length)
                {
                    rt.anchoredPosition = new Vector2(xPositions[i], 0f);
                }

                if (slot.iconImage != null)
                {
                    slot.iconImage.preserveAspect = true;
                    RectTransform iconRt = slot.iconImage.rectTransform;
                    if (iconRt != null)
                    {
                        iconRt.sizeDelta = new Vector2(52f, 52f);
                        iconRt.localScale = new Vector3(2.66f, 2.66f, 1f);
                    }
                }
            }

            // Opravíme prípadne skryjeme duplicitný LeaveButton v TreasureSection,
            // keďže v ChestSection už je aktívny ReturnToMapButton na rovnakom mieste
            Transform leaveTr = treasureSection.Find("LeaveButton");
            if (leaveTr != null)
            {
                Button btn = leaveTr.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => LoadingScene.Load(SceneIds.Map));
                }

                GameObject returnBtn = GameObject.Find("ReturnToMapButton");
                if (returnBtn != null && returnBtn.activeInHierarchy)
                {
                    leaveTr.gameObject.SetActive(false);
                }
            }
        }
    }

    public void GenerateChestRewards()
    {
        if (rewardSlots == null || rewardSlots.Count == 0) return;

        List<ItemData> candidatePool = BuildRewardPool();
        if (candidatePool.Count == 0) return;

        // Rozdelíme odmeny na:
        // 1) Add Max HP Cell (Reward Type A)
        // 2) Change Cell Element (Reward Type B)
        // 3) Všetky zvyšné (ďalšie elementy + relikvie)
        ItemData addMaxHpItem = candidatePool.Find(i => i != null && i.itemType == ItemType.Upgrade && i.elementType == ElementType.None);
        List<ItemData> elementUpgrades = candidatePool.FindAll(i => i != null && i.itemType == ItemType.Upgrade && i.elementType != ElementType.None);

        List<ItemData> chosenRewards = new List<ItemData>();

        if (addMaxHpItem != null)
        {
            chosenRewards.Add(addMaxHpItem);
        }

        if (elementUpgrades.Count > 0)
        {
            ItemData randomElem = elementUpgrades[Random.Range(0, elementUpgrades.Count)];
            chosenRewards.Add(randomElem);
        }

        List<ItemData> remainingPool = new List<ItemData>(candidatePool);
        foreach (ItemData alreadyChosen in chosenRewards)
        {
            remainingPool.Remove(alreadyChosen);
        }

        while (chosenRewards.Count < rewardSlots.Count && remainingPool.Count > 0)
        {
            int idx = Random.Range(0, remainingPool.Count);
            chosenRewards.Add(remainingPool[idx]);
            remainingPool.RemoveAt(idx);
        }

        // Náhodne premiešame poradie 3 vybraných tlačidiel
        for (int i = chosenRewards.Count - 1; i > 0; i--)
        {
            int swapIdx = Random.Range(0, i + 1);
            ItemData temp = chosenRewards[i];
            chosenRewards[i] = chosenRewards[swapIdx];
            chosenRewards[swapIdx] = temp;
        }

        for (int i = 0; i < rewardSlots.Count; i++)
        {
            if (rewardSlots[i] == null) continue;
            rewardSlots[i].isFreeReward = true;
            rewardSlots[i].SetItem(i < chosenRewards.Count ? chosenRewards[i] : null);
        }
    }

    private List<ItemData> BuildRewardPool()
    {
        List<ItemData> pool = new List<ItemData>();

        if (allUpgradeItems != null)
        {
            foreach (ItemData item in allUpgradeItems)
            {
                if (item != null && !pool.Contains(item))
                {
                    pool.Add(item);
                }
            }
        }

        if (allRelicItems != null)
        {
            foreach (ItemData item in allRelicItems)
            {
                if (item != null && !pool.Contains(item))
                {
                    pool.Add(item);
                }
            }
        }

        if (additionalRelicSprites != null)
        {
            foreach (Sprite sp in additionalRelicSprites)
            {
                if (sp == null) continue;
                bool alreadyHasSprite = pool.Exists(i => i != null && i.icon == sp);
                if (!alreadyHasSprite)
                {
                    ItemData runtimeRelic = ScriptableObject.CreateInstance<ItemData>();
                    runtimeRelic.itemName = FormatRelicName(sp.name);
                    runtimeRelic.icon = sp;
                    runtimeRelic.price = 0;
                    runtimeRelic.itemType = ItemType.Relic;
                    runtimeCreatedItems.Add(runtimeRelic);
                    pool.Add(runtimeRelic);
                }
            }
        }

        return pool;
    }

    private static string FormatRelicName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName)) return "Relic";
        string cleaned = rawName.Replace("_0", "").Replace('_', ' ');
        return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned.ToLowerInvariant());
    }

    public void OnRewardClaimed(ShopSlotUI claimedSlot)
    {
        if (!singleChoiceOnly || rewardSlots == null) return;

        foreach (ShopSlotUI slot in rewardSlots)
        {
            if (slot != null && slot != claimedSlot)
            {
                slot.SetItem(null);
            }
        }
    }
}
