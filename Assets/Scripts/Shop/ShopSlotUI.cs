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
            // Ak máme predmet, uistíme sa, že celý slot (GameObject) je zapnutý a viditeľný
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
            // AK JE PREDMET KÚPENÝ / PRÁZDNY:
            // Úplne vypne celý tento GameObject (slot úplne zmizne z obrazovky)
            gameObject.SetActive(false);
        }
    }

    void TryBuyItem() 
    {
        //if (assignedItem == null) return;

        // Vypíše do konzoly nákup
        Debug.Log($"Kupujem {assignedItem.itemName} za {assignedItem.price} goldov.");

        // Tu neskôr pridáte logiku na odčítanie mincí hráčovi

        // Po úspešnom nákupe nastavíme predmet na null, čo zavolá else vetvu a skryjeme celý slot
        SetItem(null);
    }
}