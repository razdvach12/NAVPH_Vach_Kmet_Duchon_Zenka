using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScene : MonoBehaviour
{
    public static LoadingScene Instance;
    public GameObject loadingScreen;
    public Slider ProgressBar;
    

    public RectTransform loadingSpinner;
    public float spinSpeed = 300f; 

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
            loadingSpinner.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        }
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
        float minimumLoadTime = 5f; 

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