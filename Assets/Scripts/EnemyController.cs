using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controlador base de enemigos.
/// Gestiona el estado de posesión, movimiento con colisiones y referencia al inventario.
/// </summary>
[RequireComponent(typeof(InventoryHolder))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CapsuleCollider))]
public class EnemyController : MonoBehaviour
{
    [Header("Física")]
    [SerializeField] private float gravity = 20f;
    
    public bool CanBePossessed => true;

    private bool isPossessed = false;
    private InventoryHolder inventory;
    private CharacterController characterController;
    private NavMeshAgent navAgent;
    private float verticalVelocity = 0f;

    public bool IsPossessed => isPossessed;
    public InventoryHolder Inventory => inventory;
    public CharacterController Controller => characterController;
    public NavMeshAgent NavAgent => navAgent;

    void Awake()
    {
        inventory = GetComponent<InventoryHolder>();
        characterController = GetComponent<CharacterController>();
        navAgent = GetComponent<NavMeshAgent>();
        
        // Configurar CapsuleCollider como trigger para detectar pickups y otros triggers
        CapsuleCollider triggerCollider = GetComponent<CapsuleCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            // Ajustar para que coincida con el CharacterController
            triggerCollider.center = characterController.center;
            triggerCollider.radius = characterController.radius;
            triggerCollider.height = characterController.height;
        }
    }

    void Update()
    {
        // Si tiene NavMeshAgent activo (IA controlando), no aplicar gravedad manual
        // El NavMeshAgent ya maneja el movimiento y la gravedad
        if (navAgent != null && navAgent.enabled && !isPossessed)
        {
            return;
        }
        
        // Aplicar gravedad siempre
        UpdateGravity();
        
        // Si no está siendo movido por posesión, aplicar gravedad verticalmente
        if (!isPossessed)
        {
            characterController.Move(new Vector3(0f, verticalVelocity * Time.deltaTime, 0f));
        }
    }

    private void UpdateGravity()
    {
        if (characterController == null) return;
        
        if (characterController.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
    }

    /// <summary>
    /// Mueve al enemigo respetando colisiones (incluye gravedad calculada en Update).
    /// </summary>
    public void Move(Vector3 motion)
    {
        if (characterController == null) return;
        
        motion.y = verticalVelocity * Time.deltaTime;
        characterController.Move(motion);
    }

    public void OnPossessed()
    {
        isPossessed = true;
        
        // Desactivar NavMeshAgent cuando está poseído
        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }
        
        Debug.Log($"{name} poseído");
    }

    public void OnReleased()
    {
        isPossessed = false;
        
        // Reactivar NavMeshAgent cuando es liberado
        if (navAgent != null)
        {
            navAgent.enabled = true;
            navAgent.isStopped = false;
        }
        
        Debug.Log($"{name} liberado");
    }
}
