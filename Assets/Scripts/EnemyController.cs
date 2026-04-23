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

    [Header("Ragdoll")]
    [SerializeField] private float ragdollForce = 5f;
    
    public bool CanBePossessed => true;

    private bool isPossessed = false;
    private InventoryHolder inventory;
    private HitReactionController hitReactionController;
    private CharacterController characterController;
    private NavMeshAgent navAgent;
    private float verticalVelocity = 0f;

    public bool IsPossessed => isPossessed;
    public bool IsDead => inventory != null && inventory.IsDead;
    public InventoryHolder Inventory => inventory;
    public CharacterController Controller => characterController;
    public NavMeshAgent NavAgent => navAgent;

    void Awake()
    {
        inventory = GetComponent<InventoryHolder>();
        characterController = GetComponent<CharacterController>();
        navAgent = GetComponent<NavMeshAgent>();
        hitReactionController = GetComponent<HitReactionController>();
        
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
        
        // Ocultar modelo para vista en primera persona
        SetModelVisible(false);
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
        
        // Mostrar modelo de nuevo
        SetModelVisible(true);
        
        Debug.Log($"{name} liberado");
    }

    /// <summary>
    /// Aplica daño al enemigo. Si muere, activa el ragdoll con fuerza en el punto de impacto.
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (inventory == null || inventory.IsDead) return;

        inventory.TakeDamage(damage);

        if (inventory.IsDead)
            Die(hitPoint, hitDirection);
        else
            hitReactionController?.ReactToHit(hitPoint, hitDirection);
    }

    private void Die(Vector3 hitPoint, Vector3 hitDirection)
    {
        // Detener IA
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        // Detener agente y controlador de movimiento
        if (navAgent != null) { navAgent.isStopped = true; navAgent.enabled = false; }
        if (characterController != null) characterController.enabled = false;

        // Desactivar animador
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.enabled = false;

        // Asegurarse de que el modelo es visible
        SetModelVisible(true);

        // Activar ragdoll: hacer todos los Rigidbody hijos no-cinemáticos
        Rigidbody[] ragdollBodies = GetComponentsInChildren<Rigidbody>();
        Rigidbody closestRb = null;
        float closestDist = float.MaxValue;

        foreach (Rigidbody rb in ragdollBodies)
        {
            rb.isKinematic = false;
            float dist = Vector3.Distance(rb.transform.position, hitPoint);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestRb = rb;
            }
        }

        // Aplicar fuerza de impacto en el hueso más cercano al punto de impacto
        // Aplicar velocidad a todos los huesos (distribuida) para evitar que salgan volando
        Vector3 impulseDir = hitDirection.normalized;
        foreach (Rigidbody rb in ragdollBodies)
        {
            float distFactor = 1f / (1f + Vector3.Distance(rb.transform.position, hitPoint));
            rb.AddForce(impulseDir * ragdollForce * distFactor, ForceMode.VelocityChange);
        }

        // Fuerza extra en el hueso del impacto
        if (closestRb != null)
            closestRb.AddForce(impulseDir * ragdollForce * 0.5f, ForceMode.VelocityChange);
    }
    
    /// <summary>
    /// Muestra u oculta todos los renderers del modelo (para primera persona)
    /// </summary>
    public void SetModelVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }
}
