using UnityEngine;
using UnityEngine.UI;

public class IgnoringAlphaPixels : MonoBehaviour
{
    void Start()
    {
        // Ignoruje kliknutia na pixely, ktoré sú priehľadnejšie ako 10%
        GetComponent<Image>().alphaHitTestMinimumThreshold = 0.1f;
    }
}