using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingSceneManager : MonoBehaviour
{
    public void OnClickPlay()
    {
        LoadingScene.Instance.SwitchToScene(2);
    }
}
