using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Combat/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public Sprite characterSprite;
    [Header("Počiatočné hodnoty")]
    public int startingGold = 100;
    public int startingStamina = 5;

    [Header("Grid a Zbrane")]
    public GridPreset healthGridPreset;       // Počiatočný Grid postavy
    public List<GridPreset> startingWeapons;  // Počiatočné zbrane (napr. Weapon 1 a Weapon 2)
}