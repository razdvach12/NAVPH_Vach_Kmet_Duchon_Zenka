using UnityEngine;

//toto je zakladna forma presetu pre tvorbu gridu


[System.Serializable]
public struct CellSetup
{
    public BlockElement element; // None = prázdne miesto, Basic, Lava, Ice, Rock, Nature
    [Range(1, 3)]
    public int thickness;        // Hrúbka políčka 1 až 3

    public enum Gridtype
    {
        Character,
        Weapon,
        Heal
    }
}

[CreateAssetMenu(fileName = "NewGridPreset", menuName = "Combat/Grid Preset")]
public class GridPreset : ScriptableObject
{
    public int width = 5;
    public int height = 5;

    [Header("Vizuál zbrane v UI")]
    public Sprite weaponIcon;
    
    [Header("Grid type:")]
    public CellSetup.Gridtype actionType = CellSetup.Gridtype.Character;
    public int staminaCost = 1;
    
    // 1D pole reprezentujúce 2D mriežku (veľkosť = width * height)
    public CellSetup[] cells = new CellSetup[25];

    public CellSetup GetCell(int x, int y)
    {
        int index = y * width + x;
        if (cells != null && index >= 0 && index < cells.Length)
            return cells[index];
        
        return new CellSetup { element = BlockElement.None, thickness = 1 };
    }

    // Vytvorí dočasnú kópiu otočenú o 90° v smere hodinových ručičiek (originálny asset sa nemení)
    public GridPreset CreateRotatedClockwise()
    {
        GridPreset rotated = CreateInstance<GridPreset>();
        rotated.name = name;
        rotated.width = height;
        rotated.height = width;
        rotated.weaponIcon = weaponIcon;
        rotated.actionType = actionType;
        rotated.staminaCost = staminaCost;
        rotated.cells = new CellSetup[width * height];

        // (x, y) -> (height - 1 - y, x); riadky idú zhora nadol
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                rotated.cells[x * rotated.width + (height - 1 - y)] = GetCell(x, y);
            }
        }

        return rotated;
    }

    // Automaticky upraví veľkosť poľa, keď v Inspectore zmeníš width alebo height
    private void OnValidate()
    {
        int targetSize = Mathf.Max(1, width * height);
        if (cells == null || cells.Length != targetSize)
        {
            System.Array.Resize(ref cells, targetSize);
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i].thickness == 0) cells[i].thickness = 1;
            }
        }
    }
}