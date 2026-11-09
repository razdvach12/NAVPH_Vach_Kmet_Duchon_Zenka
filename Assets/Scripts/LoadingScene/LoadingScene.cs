using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScene : MonoBehaviour
{
    public static LoadingScene Instance;
    public GameObject loadingScreen;
    public Slider ProgressBar;
    
    [Header("Nastavenie scény")]
    [Tooltip("Index scény (Build Index), ktorá sa má načítať.")]
    public int targetSceneID; 

    public RectTransform loadingSpinner;
    public float baseSpinSpeed = 300f;

    [Header("Nastavenia nepravidelného otáčania")]
    public float wobbleAmount = 200f;  
    public float wobbleFrequency = 3f; 

    private GameObject blackBackgroundObj;
    private bool isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            SetLoadingVisualsActive(false);
            return;
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        if (ProgressBar != null)
        {
            ProgressBar.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (loadingScreen != null && loadingScreen.activeSelf && loadingSpinner != null)
        {
            float currentSpeed = baseSpinSpeed + Mathf.Sin(Time.time * wobbleFrequency) * wobbleAmount;

            loadingSpinner.Rotate(0f, 0f, -currentSpeed * Time.deltaTime);
        }
    }

    public void SwitchToTargetScene()
    {
        SwitchToScene(targetSceneID);
    }

    public void SwitchToScene(int id)
    {
        if (Instance != null && Instance != this)
        {
            Instance.SwitchToScene(id);
            return;
        }

        if (isLoading)
        {
            return;
        }

        isLoading = true;
        SetLoadingVisualsActive(true);
        if (ProgressBar != null)
        {
            ProgressBar.value = 0;
            ProgressBar.gameObject.SetActive(false);
        }
        
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayMusic("LoadingBackground");
        }
        
        StartCoroutine(SwitchToSceneAsyc(id));
    }

    private void SetLoadingVisualsActive(bool isActive)
    {
        if (isActive)
        {
            EnsureBlackBackground();

            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }

            if (loadingSpinner != null)
            {
                loadingSpinner.gameObject.SetActive(true);
            }

            if (ProgressBar != null)
            {
                ProgressBar.gameObject.SetActive(false);
            }

            if (blackBackgroundObj != null)
            {
                blackBackgroundObj.SetActive(true);
                blackBackgroundObj.transform.SetAsFirstSibling();
            }
        }
        else
        {
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }

            if (blackBackgroundObj != null)
            {
                blackBackgroundObj.SetActive(false);
            }
        }
    }

    private void EnsureBlackBackground()
    {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>(true);
        }

        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            if (blackBackgroundObj == null)
            {
                blackBackgroundObj = new GameObject("LoadingBlackBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                blackBackgroundObj.transform.SetParent(canvas.transform, false);
                blackBackgroundObj.transform.SetAsFirstSibling();

                RectTransform rect = blackBackgroundObj.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;

                Image img = blackBackgroundObj.GetComponent<Image>();
                img.color = Color.black;
                img.raycastTarget = true;
            }
        }
    }

    IEnumerator SwitchToSceneAsyc(int id)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(id);
        asyncLoad.allowSceneActivation = false; 

        float elapsedTime = 0f;
        float minimumLoadTime = 1f; 

        while (asyncLoad.progress < 0.9f || elapsedTime < minimumLoadTime)
        {
            elapsedTime += Time.deltaTime;
            float timeProgress = elapsedTime / minimumLoadTime;
            if (ProgressBar != null)
            {
                ProgressBar.value = Mathf.Min(timeProgress, asyncLoad.progress / 0.9f);
            }
            
            yield return null;
        }

        if (ProgressBar != null)
        {
            ProgressBar.value = 1f;
        }
        yield return new WaitForSeconds(0.3f); 
        asyncLoad.allowSceneActivation = true; 
        yield return new WaitForSeconds(0.2f);
        SetLoadingVisualsActive(false);
        isLoading = false;
    }
}
