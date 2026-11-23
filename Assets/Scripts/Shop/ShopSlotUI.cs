using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour 
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    [Tooltip("Ďalšie tlačidlá, ktoré robia to isté ako buyButton (napr. viditeľný BUY button). Ak je prázdne, automaticky sa nájde child 'BuyButton2'.")]
    public Button[] extraBuyButtons;

    [Tooltip("Ak je zapnuté (alebo ak chýba priceText, napr. v ChestScene), predmet je zadarmo")]
    public bool isFreeReward = false;

    [HideInInspector]
    public ItemData assignedItem;

    // Predmety s priceIncreasePerPurchase > 0 po kúpe v obchode nezmiznú, ale zdražejú (platí iba počas tejto návštevy obchodu)
    private int priceIncrease;

    // Aktuálna cena vrátane zdraženia (asset sa nemení)
    public int CurrentPrice => assignedItem != null ? assignedItem.price + priceIncrease : 0;

    private bool IsRepeatable => assignedItem != null && assignedItem.priceIncreasePerPurchase > 0;

    // Po kúpe: opakovateľný predmet zdražie a ostane, ostatné zmiznú. Vráti true, ak predmet ostal v slote.
    private bool TryKeepAfterPurchase(bool free)
    {
        if (free || !IsRepeatable) return false;

        priceIncrease += assignedItem.priceIncreasePerPurchase;
        UpdatePriceText();
        return true;
    }

    public void SetItem(ItemData newItem)
    {
        assignedItem = newItem;
        priceIncrease = 0;

        ResolveChildReferencesIfNeeded();

        if (assignedItem != null)
        {
            gameObject.SetActive(true);

            if (iconImage != null)
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = assignedItem.icon;
                iconImage.preserveAspect = true;
            }
            if (nameText != null) nameText.text = assignedItem.itemName;
            UpdatePriceText();
            
            HookBuyButton(buyButton);

            if (extraBuyButtons != null)
            {
                foreach (Button extra in extraBuyButtons)
                {
                    HookBuyButton(extra);
                }
            }
        } 
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Napojí tlačidlo na kúpu predmetu. RemoveAllListeners maže iba listenery pridané z kódu,
    // takže zvuk kliknutia nastavený v Inspectore (OnClick -> AudioSource.Play) ostane zachovaný.
    private void HookBuyButton(Button btn)
    {
        if (btn == null) return;

        btn.interactable = true;
        btn.gameObject.SetActive(true);
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(TryBuyItem);
    }

    public void ResolveChildReferencesIfNeeded()
    {
        if (extraBuyButtons == null || extraBuyButtons.Length == 0)
        {
            // BuyButton2 je BasicButton prefab: klik zachytí vnútorný child "Button" (má Image), preto
            // berieme všetky Buttony v jeho hierarchii (koreňový Button bez grafiky klik nikdy nedostane, takže nekúpi 2x)
            Transform btn2Tr = transform.Find("BuyButton2");
            if (btn2Tr != null) extraBuyButtons = btn2Tr.GetComponentsInChildren<Button>(true);
        }

        if (iconImage == null)
        {
            Transform iconTr = transform.Find("ItemIcon") ?? transform.Find("IconImage");
            if (iconTr != null) iconImage = iconTr.GetComponent<Image>();
        }

        if (nameText == null)
        {
            Transform nameTr = transform.Find("ItemName") ?? transform.Find("NameText");
            if (nameTr != null) nameText = nameTr.GetComponent<TextMeshProUGUI>();
        }

        if (buyButton == null)
        {
            Transform btnTr = transform.Find("BuyButton") ?? transform.Find("ChooseButton");
            if (btnTr != null) buyButton = btnTr.GetComponent<Button>();
            if (buyButton == null) buyButton = GetComponentInChildren<Button>(true);
        }
    }

    private void UpdatePriceText()
    {
        if (priceText == null || assignedItem == null) return;

        if (isFreeReward || assignedItem.price <= 0)
            priceText.text = "";
        else
            priceText.text = $"{CurrentPrice}";
    }

    public void TryBuyItem()
    {
        if (assignedItem == null) return;

        PlayerStats stats = PlayerStats.EnsureInstance();
        bool free = isFreeReward || priceText == null || assignedItem.price <= 0;

        // --- ŠPECIÁLNA LOGIKA PRE UPGRADY GRIDU (ADD MAX HP / CHANGE ELEMENT) ---
        if (assignedItem.itemType == ItemType.Upgrade)
        {
            if (!free && stats.money < CurrentPrice)
            {
                Debug.Log("Nemáš dosť goldov na tento upgrade!");
                return;
            }

            RewardTargetingController targeting = RewardTargetingController.EnsureInstance();
            BlockElement targetElement = ConvertElementType(assignedItem.elementType);

            if (assignedItem.upgradeEffect == UpgradeEffect.WeaponSize)
            {
                // Bonus Weapon size: hráč vyberie zbraň a miesto pre nové políčko
                targeting.StartAddWeaponCellTargeting(CompleteUpgradePurchase);
            }
            else if (targetElement == BlockElement.None)
            {
                // Reward Type A: Add Max HP Cell (červený Basic štvorček)
                targeting.StartAddMaxHpTargeting(CompleteUpgradePurchase);
            }
            else
            {
                // Reward Type B: Change Cell Element (Lava, Ice, Nature, Rock)
                targeting.StartChangeElementTargeting(targetElement, CompleteUpgradePurchase);
            }
            return;
        }

        // Ak sme mali rozkliknutý targeting z iného slotu, zrušíme ho
        if (RewardTargetingController.Instance != null && RewardTargetingController.Instance.IsTargeting)
        {
            RewardTargetingController.Instance.CancelTargeting();
        }

        if (stats != null)
        {
            // --- KONTROLA LIMITOV (Zbrane, Potiony, Relikvie - max 4) ---
            if (assignedItem.itemType == ItemType.Weapon && stats.unlockedWeapons.Count >= 4)
            {
                Debug.Log("Máš plný arzenál! Nemôžeš niesť viac ako 4 zbrane.");
                return; 
            }

            if (assignedItem.itemType == ItemType.Consumable && stats.consumables.Count >= 4)
            {
                Debug.Log("Máš plný inventár potionov! Nemôžeš niesť viac ako 4 potiony.");
                return;
            }

            if (assignedItem.itemType == ItemType.Relic && stats.relics.Count >= 4)
            {
                Debug.Log("Máš plný počet relikvií! Nemôžeš ich niesť viac ako 4.");
                return;
            }
            // ------------------------------------------------

            int paidPrice = CurrentPrice;
            if (free || stats.TrySpendMoney(paidPrice))
            {
                Debug.Log(free
                    ? $"Vyberám odmenu: {assignedItem.itemName}."
                    : $"Kupujem {assignedItem.itemName} za {paidPrice} goldov.");

                stats.AddPurchasedItem(assignedItem);

                // Opakovateľný predmet ostane v obchode a zdražie
                if (TryKeepAfterPurchase(free)) return;

                SetItem(null);

                if (ShopManager.Instance != null)
                {
                    ShopManager.Instance.CheckIfSectionsAreEmpty();
                }

                if (ChestRewardManager.Instance != null)
                {
                    ChestRewardManager.Instance.OnRewardClaimed(this);
                }
            }
            else
            {
                Debug.Log("Nemáš dosť goldov na tento predmet!");
            }
        }
    }

    private void CompleteUpgradePurchase()
    {
        if (assignedItem == null) return;

        PlayerStats stats = PlayerStats.EnsureInstance();
        bool free = isFreeReward || priceText == null || assignedItem.price <= 0;

        if (!free)
        {
            if (!stats.TrySpendMoney(CurrentPrice))
            {
                Debug.Log("Nemáš dosť goldov na tento upgrade!");
                return;
            }
        }

        Debug.Log($"Aplikovaný upgrade: {assignedItem.itemName}");

        // Opakovateľný predmet v obchode ostane a zdražie (v truhle odmena zmizne ako doteraz)
        if (TryKeepAfterPurchase(free)) return;

        SetItem(null);

        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.CheckIfSectionsAreEmpty();
        }

        if (ChestRewardManager.Instance != null)
        {
            ChestRewardManager.Instance.OnRewardClaimed(this);
        }
    }

    public static BlockElement ConvertElementType(ElementType elementType)
    {
        switch (elementType)
        {
            case ElementType.Lava: return BlockElement.Lava;
            case ElementType.Ice: return BlockElement.Ice;
            case ElementType.Nature: return BlockElement.Nature;
            case ElementType.Rock: return BlockElement.Rock;
            default: return BlockElement.None;
        }
    }
}