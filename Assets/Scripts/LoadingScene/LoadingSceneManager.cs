using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneManager : MonoBehaviour
{
    public void OnClickPlay()
    {
        LoadingScene.Instance.SwitchToScene(5);
    }

    public void OnClickShop()
    {
        // Zmeň číslo 3 na presný Build Index tvojej Shop scény v Build Settings
        LoadingScene.Instance.SwitchToScene(2); 
    }

    public void OnClickExitShop()
    {
        // Zmeňte číslo 4 na presný Build Index scény (napr. Mapy), do ktorej sa chcete vrátiť
        LoadingScene.Instance.SwitchToScene(5); //
    }

    public void OnClickTreasureScene()
    {
        // Zmeňte číslo 4 na presný Build Index scény (napr. Mapy), do ktorej sa chcete vrátiť
        LoadingScene.Instance.SwitchToScene(6); //
    }

}
