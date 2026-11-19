using UnityEngine;
using UnityEngine.UI;

public class IgnoringAlphaPixels : MonoBehaviour
{
    void Start()
    {
        // Ignoruje kliknutia na pixely, ktoré sú priehľadnejšie ako 10% (len ak má textúra zapnuté Read/Write)
        Image img = GetComponent<Image>();
        if (img != null && img.sprite != null && img.sprite.texture != null && img.sprite.texture.isReadable)
        {
            img.alphaHitTestMinimumThreshold = 0.1f;
        }
    }
}