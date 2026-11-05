// Výber encountera z mapy - statický, aby prežil načítanie scény
public static class EncounterSelection
{
    // Konkrétna room (napr. boss) - má prednosť pred obtiažnosťou
    public static RoomData NextRoom;

    // null = nič nebolo zvolené (napr. scéna spustená priamo v editore) - použije sa hodnota z TurnManagera
    public static RoomDifficulty? NextDifficulty;
}
