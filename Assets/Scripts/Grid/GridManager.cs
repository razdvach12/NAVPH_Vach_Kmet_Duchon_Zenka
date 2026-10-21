using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GameObject gridSlotPrefab;
    [SerializeField] private int width, height;

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var spawnedTile = Instantiate(gridSlotPrefab, new  Vector3(x, y, 0), Quaternion.identity);
                spawnedTile.name = $"Tile _{x}_{y}";
            } 
        }
    }
}
