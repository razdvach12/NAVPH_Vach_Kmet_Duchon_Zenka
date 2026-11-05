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
    
    // Normal encounter: 80 % Easy, 20 % Medium
    public void OnClickNormalEncounter()
    {
        EncounterSelection.NextDifficulty = Random.value < 0.8f ? RoomDifficulty.Easy : RoomDifficulty.Medium;
        LoadingScene.Instance.SwitchToScene(7);
    }

    // Challenging encounter: vždy Medium
    public void OnClickChallengingEncounter()
    {
        EncounterSelection.NextDifficulty = RoomDifficulty.Medium;
        LoadingScene.Instance.SwitchToScene(7);
    }

    // Encounter v konkrétnej room (napr. BossRoom1) - room sa priradí v On Click tlačidla
    public void OnClickRoomEncounter(RoomData room)
    {
        EncounterSelection.NextRoom = room;
        LoadingScene.Instance.SwitchToScene(7);
    }

}
