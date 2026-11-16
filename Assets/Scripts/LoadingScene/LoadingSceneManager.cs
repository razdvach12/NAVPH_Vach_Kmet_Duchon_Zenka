using UnityEngine;

public class LoadingSceneManager : MonoBehaviour
{
    // Testovacie tlačidlá v MapTemplate
    public void OnClickPlay()
    {
        LoadingScene.Load(SceneIds.MapTemplate);
    }

    public void OnClickShop()
    {
        LoadingScene.Load(SceneIds.Shop);
    }

    public void OnClickExitShop()
    {
        LoadingScene.Load(SceneIds.Map);
    }

    // Tlačidlo na návrat do mapy (Chest, Meadow) - ukončí scénu a vráti hráča na mapu
    public void OnClickReturnToMap()
    {
        LoadingScene.Load(SceneIds.Map);
    }

    public void OnClickTreasureScene()
    {
        LoadingScene.Load(SceneIds.Chest);
    }

    public void OnClickMeadowScene()
    {
        LoadingScene.Load(SceneIds.Meadow);
    }

    // Normal encounter: 80 % Easy, 20 % Medium
    public void OnClickNormalEncounter()
    {
        EncounterSelection.NextDifficulty = Random.value < 0.8f ? RoomDifficulty.Easy : RoomDifficulty.Medium;
        LoadingScene.Load(SceneIds.Encounter);
    }

    // Challenging encounter: vždy Medium
    public void OnClickChallengingEncounter()
    {
        EncounterSelection.NextDifficulty = RoomDifficulty.Medium;
        LoadingScene.Load(SceneIds.Encounter);
    }

    // Encounter v konkrétnej room (napr. BossRoom1) - room sa priradí v On Click tlačidla
    public void OnClickRoomEncounter(RoomData room)
    {
        EncounterSelection.NextRoom = room;
        LoadingScene.Load(SceneIds.Encounter);
    }
}
