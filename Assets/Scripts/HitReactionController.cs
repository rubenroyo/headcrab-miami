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
