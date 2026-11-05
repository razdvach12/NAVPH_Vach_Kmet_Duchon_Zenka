using UnityEngine;
using System.Collections;

public class ChestShaking : MonoBehaviour
{
    private RectTransform rectTransform;
    
    // Pôvodné hodnoty na návrat do normálu
    private Vector2 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;

    [Header("Nastavenia záchvevov (Twitch/Shake)")]
    [Tooltip("Ako ďaleko truhla odskočí pri zatrasení")]
    public float shakeIntensity = 8f; 
    [Tooltip("Ako veľmi sa nakloní (rotácia)")]
    public float shakeRotation = 4f;   
    [Tooltip("Ako dlho trvá jeden záchvev")]
    public float shakeDuration = 0.2f; 
    [Tooltip("Minimálny čas kľudu medzi záchvevmi")]
    public float minTimeBetweenShakes = 1.5f; 
    [Tooltip("Maximálny čas kľudu medzi záchvevmi")]
    public float maxTimeBetweenShakes = 4f; 

    [Header("Dýchanie (Breathing)")]
    public bool enableBreathing = true;
    [Tooltip("Rýchlosť dýchania")]
    public float breathSpeed = 1.5f;
    [Tooltip("Ako veľmi sa truhla pri dýchaní nafúkne")]
    public float breathAmount = 0.03f;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Zapamätáme si pôvodné hodnoty, aby truhla neušla z obrazovky
        originalPosition = rectTransform.anchoredPosition;
        originalRotation = rectTransform.localRotation;
        originalScale = rectTransform.localScale;

        // Spustíme nekonečnú slučku trasenia
        StartCoroutine(MimicRoutine());
    }

    private void Update()
    {
        // Jemné pulzovanie (dýchanie), kým truhla čaká
        if (enableBreathing)
        {
            float breath = Mathf.Sin(Time.time * breathSpeed) * breathAmount;
            rectTransform.localScale = originalScale + new Vector3(breath, breath, 0);
        }
    }

    private IEnumerator MimicRoutine()
    {
        while (true)
        {
            // 1. Čakáme náhodný čas (truhla sa tvári nenápadne)
            float waitTime = Random.Range(minTimeBetweenShakes, maxTimeBetweenShakes);
            yield return new WaitForSeconds(waitTime);

            // 2. Náhly agresívny záchvev
            float elapsed = 0f;
            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;

                // Generovanie náhodnej pozície a rotácie
                Vector2 randomOffset = Random.insideUnitCircle * shakeIntensity;
                float randomRot = Random.Range(-shakeRotation, shakeRotation);

                // Aplikovanie otrasu
                rectTransform.anchoredPosition = originalPosition + randomOffset;
                rectTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, randomRot);

                yield return null; // Počkáme na ďalší frame
            }

            // 3. Okamžitý návrat do pôvodnej (nevinnej) pózy
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localRotation = originalRotation;
        }
    }
}