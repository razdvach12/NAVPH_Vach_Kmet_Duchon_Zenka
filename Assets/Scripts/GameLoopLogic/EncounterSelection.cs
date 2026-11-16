// Výber encountera z mapy - statický, aby prežil načítanie scény
public static class EncounterSelection
{
    // Konkrétna room - má prednosť pred obtiažnosťou
    public static RoomData NextRoom;

    // null = nič nebolo zvolené - použije sa hodnota z TurnManagera
    public static RoomDifficulty? NextDifficulty;
}
