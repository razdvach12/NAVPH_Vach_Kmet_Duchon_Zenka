using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopSlotUI : MonoBehaviour 
{
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;
    public Button buyButton;

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
            if (priceText != null) priceText.text = $"{assignedItem.price} Gold";
            
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

    void TryBuyItem() 
    {
        if (assignedItem == null) return;

        if (PlayerStats.Instance != null)
        {
            // --- KONTROLA LIMITOV (Zbrane, Potiony, Relikvie - max 4) ---
            if (assignedItem.itemType == ItemType.Weapon && PlayerStats.Instance.unlockedWeapons.Count >= 4)
            {
                Debug.Log("Máš plný arzenál! Nemôžeš niesť viac ako 4 zbrane.");
                return; 
            }

            if (assignedItem.itemType == ItemType.Consumable && PlayerStats.Instance.consumables.Count >= 4)
            {
                Debug.Log("Máš plný inventár potionov! Nemôžeš niesť viac ako 4 potiony.");
                return;
            }

            if (assignedItem.itemType == ItemType.Relic && PlayerStats.Instance.relics.Count >= 4)
            {
                Debug.Log("Máš plný počet relikvií! Nemôžeš ich niesť viac ako 4.");
                return;
            }
            // ------------------------------------------------

            // Ak má miesto, otestujeme a minieme peniaze
            if (PlayerStats.Instance.TrySpendMoney(assignedItem.price))
            {
                Debug.Log($"Kupujem {assignedItem.itemName} za {assignedItem.price} goldov.");
                
                PlayerStats.Instance.AddPurchasedItem(assignedItem);
                
                SetItem(null);
            }
            else
            {
                Debug.Log("Nemáš dosť goldov na tento predmet!");
            }
        }
    }
}