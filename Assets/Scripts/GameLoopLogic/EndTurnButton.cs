using UnityEngine;

public class EndTurnButton : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;

    private void Awake()
    {
        // Na kliknutie je potrebný collider
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }
    }

    private void OnMouseDown()
    {
        if (turnManager != null) turnManager.EndPlayerTurn();
    }
}
