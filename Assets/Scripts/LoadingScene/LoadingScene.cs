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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }


    private void Update()
    {
        if (loadingScreen.activeSelf && loadingSpinner != null)
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
        loadingScreen.SetActive(true);
        ProgressBar.value = 0;
        StartCoroutine(SwitchToSceneAsyc(id));
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
            ProgressBar.value = Mathf.Min(timeProgress, asyncLoad.progress / 0.9f);
            
            yield return null;
        }

        ProgressBar.value = 1f; 
        yield return new WaitForSeconds(0.3f); 
        asyncLoad.allowSceneActivation = true; 
        yield return new WaitForSeconds(0.2f);
        loadingScreen.SetActive(false);
    }
}