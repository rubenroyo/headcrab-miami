using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum PossessionHighlightType { None, Possessable, Targeted }

/// <summary>
/// Controlador base de enemigos.
/// Gestiona el estado de posesión, movimiento con colisiones y referencia al inventario.
/// </summary>
[RequireComponent(typeof(InventoryHolder))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CapsuleCollider))]
public class EnemyController : MonoBehaviour
{
    [Header("Stats del Enemigo")]
    [Tooltip("ScriptableObject con velocidades, precisión y características del tipo de enemigo.")]
    [SerializeField] private EnemyStats stats;

    [Header("Física")]
    [SerializeField] private float gravity = 20f;

    [Header("Ragdoll")]
    [SerializeField] private float ragdollForce = 5f;

    [Header("Disparo")]
    [SerializeField] private Transform muzzlePoint;

    public Transform MuzzlePoint => muzzlePoint;

    public bool CanBePossessed => true;

    // ─── Highlight de posesión ───
    private static readonly int BaseColorId        = Shader.PropertyToID("_BaseColor");
    private static readonly Color HighlightRed   = new Color(4f, 0f,  0f,  1f);   // HDR rojo saturado  (objetivo seleccionado)
    private static readonly Color HighlightWhite = new Color(4f, 4f,  4f,  1f);   // HDR blanco brillante (poseibles en rango)

    private Renderer[]               cachedRenderers;
    private MaterialPropertyBlock    highlightBlock;
    private PossessionHighlightType  currentHighlight = PossessionHighlightType.None;

    private bool isPossessed    = false;
    private bool isInjured      = false;
    private bool isDeadRagdoll  = false;
    private Coroutine injuredCoroutine;
    private float injuredGraceEndTime = 0f;
    private InventoryHolder inventory;
    private HitReactionController hitReactionController;
    private CharacterController characterController;
    private NavMeshAgent navAgent;
    private float verticalVelocity = 0f;

    public bool IsPossessed => isPossessed;
    public bool IsInjured  => isInjured;
    /// <summary>Solo true cuando el ragdoll está activo (muerte definitiva).</summary>
    public bool IsDead => isDeadRagdoll;
    public InventoryHolder Inventory => inventory;
    public CharacterController Controller => characterController;
    public NavMeshAgent NavAgent => navAgent;

    /// <summary>
    /// Stats del tipo de enemigo. Nunca null si el prefab está bien configurado.
    /// PlayerController y EnemyAI deben leer speeds desde aquí.
    /// </summary>
    public EnemyStats Stats => stats;

    /// <summary>
    /// Dirección de mirada real: forward del eye point (incluye pitch) si está poseído,
    /// o forward del cuerpo si no lo está.
    /// </summary>
    public Vector3 LookDirection
    {
        get
        {
            if (!isPossessed) return transform.forward;
            Transform eye = transform.Find("EyePoint") ?? transform.Find("_PossessionEyePoint");
            return eye != null ? eye.forward : transform.forward;
        }
    }

    /// <summary>
    /// Posición del eye point. Fallback a la cabeza (transform + altura stats) si no hay eye point.
    /// </summary>
    public Vector3 EyePosition
    {
        get
        {
            Transform eye = transform.Find("EyePoint") ?? transform.Find("_PossessionEyePoint");
            return eye != null ? eye.position : transform.position + Vector3.up * 1.6f;
        }
    }

    void Awake()
    {
        inventory = GetComponent<InventoryHolder>();
        characterController = GetComponent<CharacterController>();
        navAgent = GetComponent<NavMeshAgent>();
        hitReactionController = GetComponent<HitReactionController>();
        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        if (stats == null)
            Debug.LogWarning($"[{name}] EnemyController: no tiene EnemyStats asignado. " +
                             "Asigna un EnemyStats ScriptableObject en el Inspector.");

        // Configurar CapsuleCollider como trigger para pickups
        CapsuleCollider triggerCollider = GetComponent<CapsuleCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.center = characterController.center;
            triggerCollider.radius = characterController.radius;
            triggerCollider.height = characterController.height;
        }
    }

    void Update()
    {
        // Si el NavMeshAgent está activo (IA controlando), él gestiona el movimiento
        if (navAgent != null && navAgent.enabled && !isPossessed)
            return;

        UpdateGravity();

        if (!isPossessed)
            characterController.Move(new Vector3(0f, verticalVelocity * Time.deltaTime, 0f));
    }

    private void UpdateGravity()
    {
        if (characterController == null) return;

        if (characterController.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity -= gravity * Time.deltaTime;
    }

    /// <summary>
    /// Mueve al enemigo respetando colisiones (gravedad gestionada en Update).
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

        if (navAgent != null)
        {
            navAgent.isStopped = true;
            navAgent.enabled = false;
        }

        SetModelVisible(false);
    }

    public void OnReleased()
    {
        isPossessed = false;

        if (navAgent != null)
        {
            navAgent.enabled = true;
            navAgent.isStopped = false;
        }

        SetModelVisible(true);
    }

    /// <summary>
    /// Aplica daño. Si muere, activa ragdoll escalado por el daño final.
    /// </summary>
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (inventory == null) return;

        // Si ya está injured, cualquier golpe lo mata (respetando periodo de gracia)
        if (isInjured)
        {
            if (Time.time < injuredGraceEndTime) return;
            if (injuredCoroutine != null) StopCoroutine(injuredCoroutine);
            Die(damage, hitPoint, hitDirection);
            return;
        }

        if (inventory.IsDead) return;

        inventory.TakeDamage(damage);

        if (inventory.IsDead)
            BeginInjured(damage, hitPoint, hitDirection);
        else if (hitReactionController != null)
            hitReactionController.TriggerHitAnimation(hitPoint, hitDirection);
    }

    private void BeginInjured(float finalDamage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isInjured) return;
        isInjured = true;
        injuredGraceEndTime = Time.time + 0.5f;

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        if (navAgent != null) { navAgent.isStopped = true; navAgent.enabled = false; }

        if (hitReactionController != null)
            hitReactionController.EnterInjuredState();

        injuredCoroutine = StartCoroutine(InjuredCoroutine(finalDamage, hitPoint, hitDirection));
    }

    private IEnumerator InjuredCoroutine(float finalDamage, Vector3 hitPoint, Vector3 hitDirection)
    {
        yield return new WaitForSeconds(10f);
        Die(finalDamage, hitPoint, hitDirection);
    }

    private void Die(float finalDamage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDeadRagdoll) return;  // evitar doble muerte
        isDeadRagdoll = true;

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        if (navAgent != null) { navAgent.isStopped = true; navAgent.enabled = false; }
        if (characterController != null) characterController.enabled = false;

        // Soltar el arma aquí, en la muerte real
        if (inventory != null && inventory.HasWeapon) inventory.DropWeapon();

        SetModelVisible(true);

        // HitReactionController desactiva el Animator y activa el ragdoll físico
        if (hitReactionController != null)
        {
            hitReactionController.ActivateRagdoll(finalDamage, hitPoint, hitDirection);
        }
        else
        {
            // Fallback si no hay HitReactionController en el prefab
            Animator animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.enabled = false;

            Vector3 dir = hitDirection.normalized;
            foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            {
                rb.isKinematic = false;
                rb.AddForce(dir * ragdollForce, ForceMode.VelocityChange);
            }
        }
    }

    public void SetModelVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }

    /// <summary>
    /// Aplica o elimina el tinte de highlight de posesión vía MaterialPropertyBlock.
    /// Usa _BaseColor (URP). Si tu shader usa otro nombre, cámbialo en BaseColorId.
    /// </summary>
    public void SetPossessionHighlight(PossessionHighlightType type)
    {
        if (type == currentHighlight) return;
        currentHighlight = type;

        if (cachedRenderers == null)
            cachedRenderers = GetComponentsInChildren<Renderer>(true);

        if (type == PossessionHighlightType.None)
        {
            foreach (var r in cachedRenderers)
                r?.SetPropertyBlock(null);
            return;
        }

        if (highlightBlock == null) highlightBlock = new MaterialPropertyBlock();

        // Targeted (objetivo seleccionado) → rojo; Possessable (en rango) → blanco
        highlightBlock.SetColor(BaseColorId,
            type == PossessionHighlightType.Targeted ? HighlightRed : HighlightWhite);

        foreach (var r in cachedRenderers)
            r?.SetPropertyBlock(highlightBlock);
    }
}
