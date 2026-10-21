using UnityEngine;

[System.Serializable]
public struct CellSetup
{
    public BlockElement element; // None = prázdne miesto, Basic, Lava, Ice, Rock, Nature
    [Range(1, 3)]
    public int thickness;        // Hrúbka políčka 1 až 3
}

[CreateAssetMenu(fileName = "NewGridPreset", menuName = "Combat/Grid Preset")]
public class GridPreset : ScriptableObject
{
    public int width = 5;
    public int height = 5;

    // 1D pole reprezentujúce 2D mriežku (veľkosť = width * height)
    public CellSetup[] cells = new CellSetup[25];

    public CellSetup GetCell(int x, int y)
    {
        int index = y * width + x;
        if (cells != null && index >= 0 && index < cells.Length)
            return cells[index];
        
        return new CellSetup { element = BlockElement.None, thickness = 1 };
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