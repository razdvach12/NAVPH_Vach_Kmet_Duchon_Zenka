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
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(TryBuyItem);
            }
        } 
        else 
        {
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            if (nameText != null) nameText.text = "Prázdne";
            if (priceText != null) priceText.text = "";
            if (buyButton != null) buyButton.interactable = false;
        }
    }

    void TryBuyItem() 
    {
        if (assignedItem == null) return;
        Debug.Log(string.Format("Hráč kúpil predmet: {0} za {1} mincí.", assignedItem.itemName, assignedItem.price));
        SetItem(null); // Po nákupe slot vyčistí
    }
}