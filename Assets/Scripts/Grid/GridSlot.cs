using UnityEngine;

public class GridSlot : MonoBehaviour
{
    [SerializeField] public GameObject gridSlotPrefab;
    void Start()
    {
        if (gridSlotPrefab != null)
        {
            Instantiate(gridSlotPrefab, transform);
        }  
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
