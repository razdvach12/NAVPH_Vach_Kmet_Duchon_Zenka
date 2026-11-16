using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour 
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

    [Tooltip("Ak je zapnuté (alebo ak chýba priceText, napr. v ChestScene), predmet je zadarmo")]
    public bool isFreeReward = false;

    [HideInInspector]
    public ItemData assignedItem;

    public void SetItem(ItemData newItem) 
    {
        assignedItem = newItem;

        if (assignedItem != null) 
        {
            gameObject.SetActive(true);

            if (iconImage != null) 
            {
                iconImage.gameObject.SetActive(true);
                iconImage.sprite = assignedItem.icon;
            }
            if (nameText != null) nameText.text = assignedItem.itemName;
            if (priceText != null)
            {
                if (isFreeReward || assignedItem.price <= 0)
                    priceText.text = "";
                else
                    priceText.text = $"{assignedItem.price} Gold";
            }
            
            if (buyButton != null) 
            {
                buyButton.interactable = true;
                buyButton.gameObject.SetActive(true);
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(TryBuyItem);
            }
        } 
        else 
        {
            gameObject.SetActive(false);
        }
    }

    public void TryBuyItem() 
    {
        if (assignedItem == null) return;

        PlayerStats stats = PlayerStats.EnsureInstance();
        bool free = isFreeReward || priceText == null || assignedItem.price <= 0;

        // --- ŠPECIÁLNA LOGIKA PRE UPGRADY GRIDU (ADD MAX HP / CHANGE ELEMENT) ---
        if (assignedItem.itemType == ItemType.Upgrade)
        {
            if (!free && stats.money < assignedItem.price)
            {
                Debug.Log("Nemáš dosť goldov na tento upgrade!");
                return;
            }

            RewardTargetingController targeting = RewardTargetingController.EnsureInstance();
            BlockElement targetElement = ConvertElementType(assignedItem.elementType);

            if (targetElement == BlockElement.None)
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

            if (free || stats.TrySpendMoney(assignedItem.price))
            {
                Debug.Log(free
                    ? $"Vyberám odmenu: {assignedItem.itemName}."
                    : $"Kupujem {assignedItem.itemName} za {assignedItem.price} goldov.");
                
                stats.AddPurchasedItem(assignedItem);
                
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
            if (!stats.TrySpendMoney(assignedItem.price))
            {
                Debug.Log("Nemáš dosť goldov na tento upgrade!");
                return;
            }
        }

        Debug.Log($"Aplikovaný upgrade: {assignedItem.itemName}");
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