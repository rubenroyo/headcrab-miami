using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// API de alto nivel para las acciones de combate de un enemigo:
/// Move, Run, Aim, Fire.
///
/// PUNTO CLAVE DE DISEÑO:
///   Este componente es la ÚNICA forma de ejecutar acciones sobre el enemigo,
///   tanto desde EnemyAI como desde PlayerController (en modo posesión FPS).
///   Nunca llames a EnemyController.Move() o InventoryHolder.TryFireInDirection()
///   directamente desde la IA o el input del jugador — hazlo siempre a través de aquí.
///
/// SEPARACIÓN INPUT / IA:
///   - PlayerController llama a estos métodos cuando posee al enemigo.
///   - EnemyAI llama a estos métodos cuando controla al enemigo autónomamente.
///   - Este componente no sabe ni le importa quién llama: solo ejecuta.
///
/// Colócalo en el mismo GameObject que EnemyController.
/// </summary>
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(EnemyLocomotion))]
[RequireComponent(typeof(InventoryHolder))]
public class EnemyCombatActions : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────

    private EnemyController  enemyController;
    private EnemyLocomotion  locomotion;
    private InventoryHolder  inventory;
    private NavMeshAgent     navAgent;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Awake()
    {
        enemyController = GetComponent<EnemyController>();
        locomotion      = GetComponent<EnemyLocomotion>();
        inventory       = GetComponent<InventoryHolder>();
        navAgent        = GetComponent<NavMeshAgent>();
    }

    // ─────────────────────────────────────────────
    //  MOVIMIENTO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Mueve al enemigo a velocidad de andar.
    /// Válido tanto para la IA como para el jugador poseído.
    /// </summary>
    public void Walk(Vector3 direction)
    {
        if (!CanMoveAsPlayer() && !CanMoveAsAI()) return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            locomotion.SetState(EnemyLocomotionState.Idle);
            return;
        }

        float speed = enemyController.Stats != null
            ? enemyController.Stats.walkSpeed
            : 3f;

        enemyController.Move(direction.normalized * speed * Time.deltaTime);
        locomotion.SetState(EnemyLocomotionState.Walking);
    }

    /// <summary>
    /// Mueve al enemigo a velocidad de carrera.
    /// Válido tanto para la IA como para el jugador poseído.
    /// </summary>
    public void Run(Vector3 direction)
    {
        if (!CanMoveAsPlayer() && !CanMoveAsAI()) return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
        {
            locomotion.SetState(EnemyLocomotionState.Idle);
            return;
        }

        float speed = enemyController.Stats != null
            ? enemyController.Stats.runSpeed
            : 6f;

        enemyController.Move(direction.normalized * speed * Time.deltaTime);
        locomotion.SetState(EnemyLocomotionState.Running);
    }

    public void StopMoving()
    {
        if (!locomotion.IsAiming)
            locomotion.SetState(EnemyLocomotionState.Idle);
    }

    // ─────────────────────────────────────────────
    //  ORIENTACIÓN
    // ─────────────────────────────────────────────

    /// <summary>
    /// Rota el enemigo para mirar hacia una posición objetivo.
    /// Útil para que la IA oriente al enemigo antes de disparar.
    /// </summary>
    /// <param name="targetPosition">Posición en espacio mundo hacia la que mirar.</param>
    /// <param name="rotationSpeed">Velocidad de rotación en grados/segundo.</param>
    public void LookAt(Vector3 targetPosition, float rotationSpeed = 360f)
    {
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  APUNTADO (ADS)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Activa o desactiva el modo apuntado (ADS).
    /// Cuando está activo, la dispersión del arma se reduce según WeaponType.aimingDispersionMultiplier.
    /// </summary>
    public void SetAiming(bool aiming)
    {
        locomotion.SetAiming(aiming);
    }

    // ─────────────────────────────────────────────
    //  DISPARO — modo IA (sin cámara)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Intenta disparar en la dirección indicada.
    /// Usa el estado de locomoción actual para calcular la dispersión.
    /// Modo IA: no requiere cámara.
    /// </summary>
    /// <param name="direction">Dirección normalizada en espacio mundo.</param>
    /// <param name="handTremor">Temblor adicional del enemigo (0 = mano firme).</param>
    /// <returns>True si disparó.</returns>
    public bool FireInDirection(Vector3 direction, float handTremor = 0f)
    {
        if (!CanFire()) return false;

        WeaponState weaponState = locomotion.WeaponStateForDispersion;
        return inventory.TryFireInDirection(direction, weaponState, handTremor);
    }

    /// <summary>
    /// Intenta disparar hacia una posición objetivo.
    /// Calcula la dirección automáticamente desde el muzzle.
    /// </summary>
    /// <param name="targetPosition">Posición en espacio mundo a la que apuntar.</param>
    /// <param name="handTremor">Temblor adicional del enemigo (0 = mano firme).</param>
    /// <returns>True si disparó.</returns>
    public bool FireAtPosition(Vector3 targetPosition, float handTremor = 0f)
    {
        if (!CanFire()) return false;

        Vector3 muzzlePos = inventory.GetMuzzlePosition();
        Vector3 direction = (targetPosition - muzzlePos).normalized;
        return FireInDirection(direction, handTremor);
    }

    // ─────────────────────────────────────────────
    //  DISPARO — modo jugador FPS (con cámara)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Intenta disparar usando la cámara del jugador como origen del raycast.
    /// Solo para uso de PlayerController cuando el jugador posee al enemigo.
    /// </summary>
    /// <param name="playerCamera">Cámara activa del jugador FPS.</param>
    /// <param name="handTremor">Temblor (generalmente 0 cuando el jugador controla).</param>
    /// <returns>True si disparó.</returns>
    public bool FireWithCamera(Camera playerCamera, float handTremor = 0f)
    {
        if (!CanFire()) return false;

        WeaponState weaponState = locomotion.WeaponStateForDispersion;
        return inventory.TryFire(playerCamera, weaponState, handTremor);
    }

    // ─────────────────────────────────────────────
    //  GUARDAS
    // ─────────────────────────────────────────────

    private bool CanMove() => !enemyController.IsDead && !enemyController.IsPossessed;

    /// <summary>El jugador puede mover al enemigo solo cuando lo está poseyendo.</summary>
    private bool CanMoveAsPlayer() => enemyController.IsPossessed && !enemyController.IsDead;

    /// <summary>La IA puede mover al enemigo solo cuando NO está poseído.</summary>
    private bool CanMoveAsAI()     => !enemyController.IsPossessed && !enemyController.IsDead;

    // La guarda de disparo no cambia: tanto jugador como IA pueden disparar
    // mientras el enemigo esté vivo y tenga arma.
    private bool CanFire() => !enemyController.IsDead && inventory.HasWeapon;

    // ─────────────────────────────────────────────
    //  PROPIEDADES DE CONSULTA
    // ─────────────────────────────────────────────

    /// <summary>Estado de locomoción actual. Útil para que la IA tome decisiones.</summary>
    public EnemyLocomotionState LocomotionState => locomotion.State;

    /// <summary>¿Tiene arma con balas?</summary>
    public bool HasAmmo => inventory.HasWeapon && !inventory.EquippedWeapon.IsEmpty;

    /// <summary>¿Está mirando hacia el objetivo? (dentro de un umbral de grados)</summary>
    public bool IsFacingTarget(Vector3 targetPosition, float thresholdDegrees = 10f)
    {
        Vector3 toTarget = (targetPosition - transform.position);
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.001f) return true;

        float angle = Vector3.Angle(transform.forward, toTarget.normalized);
        return angle <= thresholdDegrees;
    }

    // ─────────────────────────────────────────────
    //  CUERPO A CUERPO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Ejecuta un ataque melee con el arma equipada (solo si isMelee = true).
    /// Detecta objetivos en el radio del arma: enemigos poseídos y, en el futuro, el jugador seta.
    /// El cooldown entre golpes debe gestionarse desde quien llame (EnemyAI o input).
    /// </summary>
    /// <returns>True si impactó al menos un objetivo.</returns>
    public bool MeleeAttack()
    {
        if (!CanFire()) return false;

        WeaponType weaponType = inventory.EquippedWeapon?.weaponType;
        if (weaponType == null || !weaponType.isMelee) return false;

        Vector3    origin       = inventory.GetMuzzlePosition();
        Collider[] hits         = Physics.OverlapSphere(origin, weaponType.meleeRange);
        bool       hitSomething = false;

        foreach (Collider col in hits)
        {
            // No golpearse a sí mismo
            if (col.transform.IsChildOf(transform) || col.transform == transform) continue;

            // Golpear a un enemigo poseído (el jugador lo está controlando)
            EnemyController enemy = col.GetComponentInParent<EnemyController>();
            if (enemy != null && enemy.IsPossessed && !enemy.IsDead)
            {
                Vector3 hitDir = (col.transform.position - transform.position).normalized;
                enemy.TakeDamage(weaponType.meleeDamage,
                    col.ClosestPointOnBounds(transform.position), hitDir);
                hitSomething = true;
                continue;
            }

            // TODO: golpear al jugador en forma de seta cuando tenga sistema de vida.
            // PlayerController player = col.GetComponent<PlayerController>();
            // if (player != null) { player.TakeDamage(weaponType.meleeDamage); hitSomething = true; }
        }

        return hitSomething;
    }
}
