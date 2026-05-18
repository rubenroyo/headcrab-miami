using UnityEngine;

/// <summary>
/// Gestiona el ragdoll de muerte del enemigo.
/// Los Rigidbodies permanecen kinematic durante el juego normal;
/// al morir se activan y reciben una fuerza proporcional al daño final.
///
/// Los Rigidbodies de los huesos deben existir (creados con
/// GameObject > 3D Object > Ragdoll en el editor).
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class HitReactionController : MonoBehaviour
{
    [Header("Muerte — Ragdoll")]
    [Tooltip("Fuerza base del ragdoll de muerte.")]
    [SerializeField] private float baseDeathForce = 5f;

    [Tooltip("Daño de referencia para escalar la fuerza (30 = pistola). " +
             "Más daño → más fuerza, menos → menos (clamp 0.5×–4×).")]
    [SerializeField] private float damageReference = 30f;

    [Header("Hit Reaction — Animación")]
    [Tooltip("Altura desde transform.position por encima de la cual se considera zona de cabeza. " +
             "Ajusta según la escala del modelo (ej. 1.5 para un humano de 1.8 m).")]
    [SerializeField] private float headHeightThreshold = 1.5f;

    // Triggers del Animator (nombre exacto debe coincidir con el Animator Controller)
    private static readonly int TriggerHitBody  = Animator.StringToHash("HitBody");
    private static readonly int TriggerHitHead  = Animator.StringToHash("HitHead");
    private static readonly int TriggerHitRight = Animator.StringToHash("HitRight");
    private static readonly int TriggerHitLeft  = Animator.StringToHash("HitLeft");

    private Rigidbody[] ragdollBodies;
    private Animator    animator;

    void Awake()
    {
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        animator      = GetComponentInChildren<Animator>();

        // Kinematic hasta la muerte: el Animator controla los huesos.
        foreach (var rb in ragdollBodies)
            rb.isKinematic = true;
    }

    // ─────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Reproduce la animación de impacto adecuada según la dirección del disparo.
    /// Prioridad: cabeza (por posición del hit) > por detrás > lateral > frontal.
    /// </summary>
    public void TriggerHitAnimation(Vector3 hitPoint, Vector3 hitDirection)
    {
        if (animator == null || !animator.enabled) return;

        animator.SetTrigger(SelectHitTrigger(hitPoint, hitDirection));
    }

    /// <summary>
    /// Activa el ragdoll completo al morir.
    /// La fuerza se escala con el daño final y se atenúa en huesos alejados del impacto.
    /// </summary>
    public void ActivateRagdoll(float finalDamage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (ragdollBodies == null) return;

        if (animator != null) animator.enabled = false;

        float forceScale = Mathf.Clamp(finalDamage / Mathf.Max(damageReference, 1f), 0.5f, 4f);
        float force      = baseDeathForce * forceScale;
        Vector3 dir      = hitDirection.normalized;

        Rigidbody closestRb = FindClosestBone(hitPoint);

        foreach (var rb in ragdollBodies)
        {
            rb.isKinematic = false;
            float distFactor = 1f / (1f + Vector3.Distance(rb.transform.position, hitPoint));
            rb.AddForce(dir * force * distFactor, ForceMode.VelocityChange);
        }

        if (closestRb != null)
            closestRb.AddForce(dir * force * 0.5f, ForceMode.VelocityChange);
    }

    // ─────────────────────────────────────────────
    //  PRIVADO
    // ─────────────────────────────────────────────

    private int SelectHitTrigger(Vector3 hitPoint, Vector3 hitDirection)
    {
        // 1. Zona de cabeza: el hit point está por encima del threshold
        if (hitPoint.y >= transform.position.y + headHeightThreshold)
            return TriggerHitHead;

        // Convertir la dirección de la bala al espacio local del enemigo.
        // El atacante viene de la dirección OPUESTA a hitDirection.
        Vector3 localDir = transform.InverseTransformDirection(-hitDirection.normalized);

        float absX = Mathf.Abs(localDir.x);
        float absZ = Mathf.Abs(localDir.z);

        // 2. Desde detrás (localDir.z < 0 y domina sobre X)
        if (localDir.z < 0f && absZ >= absX)
            return TriggerHitHead;

        // 3. Lateral: X domina sobre Z
        if (absX >= absZ)
            return localDir.x > 0f ? TriggerHitRight : TriggerHitLeft;

        // 4. Frontal (localDir.z > 0)
        return TriggerHitBody;
    }

    private Rigidbody FindClosestBone(Vector3 point)
    {
        Rigidbody closest = null;
        float minDist = float.MaxValue;
        foreach (var rb in ragdollBodies)
        {
            float d = Vector3.Distance(rb.transform.position, point);
            if (d < minDist) { minDist = d; closest = rb; }
        }
        return closest;
    }
}
