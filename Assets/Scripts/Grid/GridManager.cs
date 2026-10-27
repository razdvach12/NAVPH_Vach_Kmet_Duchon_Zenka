using UnityEngine;

//tu sa nastavuje celkove zlozenie gridu daneho objektu (ci uz zivoty alebo zbrane)

public class GridManager : MonoBehaviour
{
    [Header("Base background")]
    [SerializeField] private GameObject gridSlotPrefab;
    [SerializeField] private float spacing = 1.1f; // Rozostup medzi políčkami

    [Header("Preset")]
    [SerializeField] private GridPreset gridPreset;

    [Header("Slot prefabs")]
    [SerializeField] private GameObject basicHpPrefab;
    [SerializeField] private GameObject lavaHpPrefab;
    [SerializeField] private GameObject iceHpPrefab;
    [SerializeField] private GameObject rockHpPrefab;
    [SerializeField] private GameObject natureHpPrefab;

    [Header("Grid owner")]
    public bool isPlayer = true;
    
    // 2D pole, aby sme pri útokoch vedeli okamžite nájsť políčko na [x, y]
    private GameObject[,] spawnedTiles;

    void Start()
    {
        if (gridPreset != null)
        {
            GenerateGrid();
        }
    }
    
    public void GenerateGrid()
    {
        // Zmaže staré políčka, ak skúšaš generovanie viackrát za sebou
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        int width = gridPreset.width;
        int height = gridPreset.height;
        spawnedTiles = new GameObject[width, height];

        // Posun, aby bol stred vygenerovaného gridu presne na pozícii tohto GameObjectu
        Vector3 centerOffset = new Vector3((width - 1) * spacing * 0.5f, (height - 1) * spacing * 0.5f, 0);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // (height - 1 - y) otočí os Y tak, aby y=0 bolo hore (presne ako v naklikávacej tabuľke)
                Vector3 localPos = new Vector3(x * spacing, (height - 1 - y) * spacing, 0) - centerOffset;

                // 1. Vytvorenie základného podkladového štvorčeka ako dieťa tohto objektu
                var spawnedTile = Instantiate(gridSlotPrefab, transform);
                spawnedTile.transform.localPosition = localPos;
                spawnedTile.name = $"Tile_{x}_{y}";
                spawnedTiles[x, y] = spawnedTile;

                // 2. Kontrola v šablóne, či má byť na tomto políčku život (a aký živel + hrúbka)
                CellSetup cellData = gridPreset.GetCell(x, y);
                if (cellData.element != BlockElement.None)
                {
                    GameObject prefabToSpawn = GetVariantPrefab(cellData.element);
                    if (prefabToSpawn != null)
                    {
                        // Spawne tvoj konkrétny HP variant priamo do podkladového štvorčeka
                        var hpBlock = Instantiate(prefabToSpawn, spawnedTile.transform);
                        hpBlock.transform.localPosition = new Vector3(0, 0, -0.1f); // Kúsok vpredu pred pozadím
                        hpBlock.name = $"HP_{cellData.element}_{x}_{y}";

                        // Nastavenie hrúbky (1, 2, 3) na tvojom skripte na HP prefabe
                        var healthScript = hpBlock.GetComponent<SlotValue>();
                        if (healthScript != null)
                        {
                            healthScript.Setup(cellData.thickness);
                        }
                    }
                }
            }
        }
    }

    private GameObject GetVariantPrefab(BlockElement element)
    {
        return element switch
        {
            BlockElement.Basic => basicHpPrefab,
            BlockElement.Lava => lavaHpPrefab,
            BlockElement.Ice => iceHpPrefab,
            BlockElement.Rock => rockHpPrefab,
            BlockElement.Nature => natureHpPrefab,
            _ => null
        };
    }
}