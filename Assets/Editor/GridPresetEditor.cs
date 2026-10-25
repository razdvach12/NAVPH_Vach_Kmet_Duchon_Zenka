using UnityEngine;
using UnityEditor;

//vdaka tomuto skriptu vieme vytvorit preset konkretneho gridu

[CustomEditor(typeof(GridPreset))]
public class GridPresetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GridPreset preset = (GridPreset)target;

        EditorGUI.BeginChangeCheck();
        preset.width = EditorGUILayout.IntSlider("Width", preset.width, 1, 7);
        preset.height = EditorGUILayout.IntSlider("Height", preset.height, 1, 7);

        int targetSize = preset.width * preset.height;
        if (preset.cells == null || preset.cells.Length != targetSize)
        {
            System.Array.Resize(ref preset.cells, targetSize);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Left Click = Change the Element\nRight Click = Change the value (1 - 3)", MessageType.Info);
        EditorGUILayout.Space(5);

        // Vykreslenie 2D mriežky tlačidiel
        for (int y = 0; y < preset.height; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < preset.width; x++)
            {
                int index = y * preset.width + x;
                CellSetup cell = preset.cells[index];

                GUI.backgroundColor = GetElementColor(cell.element);
                string label = cell.element == BlockElement.None ? "" : $"{cell.element.ToString()[0]}\n({cell.thickness})";

                if (GUILayout.Button(label, GUILayout.Width(48), GUILayout.Height(48)))
                {
                    if (Event.current.button == 1) // Pravý klik myši -> mení hrúbku 1, 2, 3
                    {
                        cell.thickness = (cell.thickness % 3) + 1;
                    }
                    else // Ľavý klik myši -> cykluje živly
                    {
                        cell.element = (BlockElement)(((int)cell.element + 1) % 6);
                        if (cell.thickness == 0) cell.thickness = 1;
                    }
                    preset.cells[index] = cell;
                    EditorUtility.SetDirty(preset);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(preset);
        }
    }

    private Color GetElementColor(BlockElement el)
    {
        return el switch
        {
            BlockElement.Basic => new Color(1f, 0.35f, 0.35f),  // Červená (základné HP)
            BlockElement.Lava => new Color(1f, 0.6f, 0f),       // Oranžová
            BlockElement.Ice => new Color(0.2f, 0.85f, 1f),     // Bledomodrá
            BlockElement.Rock => new Color(0.6f, 0.4f, 0.2f),   // Hnedá
            BlockElement.Nature => new Color(0.3f, 0.9f, 0.3f), // Zelená
            _ => new Color(0.25f, 0.25f, 0.25f)                 // Tmavošedá (prázdny slot)
        };
    }
}