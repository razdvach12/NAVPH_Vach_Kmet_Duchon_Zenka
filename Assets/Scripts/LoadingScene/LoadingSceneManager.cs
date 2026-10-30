using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneManager : MonoBehaviour
{
    public void OnClickPlay()
    {
        LoadingScene.Instance.SwitchToScene(2);
    }

    public void OnClickShop()
    {
        // Zmeň číslo 3 na presný Build Index tvojej Shop scény v Build Settings
        LoadingScene.Instance.SwitchToScene(2); 
    }

}
