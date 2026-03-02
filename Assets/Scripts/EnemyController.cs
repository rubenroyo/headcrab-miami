using UnityEngine;

/// <summary>
/// Controlador base de enemigos.
/// Gestiona el estado de posesión y referencia al inventario.
/// </summary>
[RequireComponent(typeof(InventoryHolder))]
public class EnemyController : MonoBehaviour
{
    public bool CanBePossessed => true;

    private bool isPossessed = false;
    private InventoryHolder inventory;

    public bool IsPossessed => isPossessed;
    public InventoryHolder Inventory => inventory;

    void Awake()
    {
        inventory = GetComponent<InventoryHolder>();
    }

    public void OnPossessed()
    {
        isPossessed = true;
        Debug.Log($"{name} poseído");
    }

    public void OnReleased()
    {
        isPossessed = false;
        Debug.Log($"{name} liberado");
    }
}
