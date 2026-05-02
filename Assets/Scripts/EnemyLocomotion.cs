using UnityEngine;

/// <summary>
/// Estado de locomoción del enemigo.
/// Usado por EnemyCombatActions, EnemyAI y PlayerController (cuando posee)
/// para comunicar al HitscanShooter qué dispersión aplicar.
/// </summary>
public enum EnemyLocomotionState
{
    Idle,
    Walking,
    Running,
    Aiming   // Andando o quieto mientras apunta (ADS)
}

/// <summary>
/// Gestiona el estado de movimiento del enemigo y lo traduce a WeaponState
/// para que HitscanShooter calcule la dispersión correcta.
/// También conduce el Animator del modelo hijo con los parámetros
/// IsWalking (Bool) e IsRunning (Bool).
///
/// Colócalo en el mismo GameObject que EnemyController.
///
/// Tanto EnemyAI como PlayerController (en modo posesión) deben
/// leer y escribir aquí — nunca directamente en EnemyController.
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class EnemyLocomotion : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────

    // Se resuelve automáticamente desde el hijo; se puede sobrescribir en el Inspector.
    [SerializeField] private Animator animator;

    // Hashes precalculados para evitar strings en Update
    private static readonly int HashIsWalking = Animator.StringToHash("IsWalking");
    private static readonly int HashIsRunning = Animator.StringToHash("IsRunning");

    // ─────────────────────────────────────────────
    //  ESTADO ACTUAL
    // ─────────────────────────────────────────────

    private EnemyLocomotionState currentState = EnemyLocomotionState.Idle;

    /// <summary>Estado de locomoción actual del enemigo.</summary>
    public EnemyLocomotionState State => currentState;

    /// <summary>
    /// Traduce el estado de locomoción al enum WeaponState que usa HitscanShooter
    /// para calcular la dispersión. Úsalo al llamar a TryFire o TryFireInDirection.
    /// </summary>
    public WeaponState WeaponStateForDispersion => currentState switch
    {
        EnemyLocomotionState.Running => WeaponState.Sprinting,
        EnemyLocomotionState.Walking => WeaponState.Moving,
        EnemyLocomotionState.Aiming  => WeaponState.Aiming,
        _                            => WeaponState.Idle
    };

    // ─────────────────────────────────────────────
    //  PROPIEDADES DE CONVENIENCIA
    // ─────────────────────────────────────────────

    public bool IsIdle    => currentState == EnemyLocomotionState.Idle;
    public bool IsWalking => currentState == EnemyLocomotionState.Walking;
    public bool IsRunning => currentState == EnemyLocomotionState.Running;
    public bool IsAiming  => currentState == EnemyLocomotionState.Aiming;

    // ─────────────────────────────────────────────
    //  API — quien manda (IA o jugador) llama esto
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    /// <summary>Establece el estado de locomoción. Llamar cada frame o al cambiar.</summary>
    public void SetState(EnemyLocomotionState newState)
    {
        currentState = newState;
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool(HashIsWalking, currentState == EnemyLocomotionState.Walking ||
                                        currentState == EnemyLocomotionState.Aiming);
        animator.SetBool(HashIsRunning, currentState == EnemyLocomotionState.Running);
    }

    /// <summary>
    /// Shortcut: activa o desactiva el modo apuntado (ADS).
    /// Si el enemigo está en movimiento al apuntar, se considera Aiming igualmente.
    /// </summary>
    public void SetAiming(bool aiming)
    {
        if (aiming)
            currentState = EnemyLocomotionState.Aiming;
        else if (currentState == EnemyLocomotionState.Aiming)
            currentState = EnemyLocomotionState.Idle;
    }
}
