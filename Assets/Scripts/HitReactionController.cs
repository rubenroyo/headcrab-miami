using System.Collections;
using UnityEngine;

/// <summary>
/// Reacciones físicas a impactos de bala por zona corporal.
/// Pausa el Animator brevemente y aplica un impulso al hueso más cercano al golpe.
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class HitReactionController : MonoBehaviour
{
    [Header("Fuerza por zona")]
    [SerializeField] private float headForce    = 60f;
    [SerializeField] private float torsoForce   = 40f;
    [SerializeField] private float legsForce    = 25f;

    [Header("Duración de la reacción")]
    [SerializeField] private float reactionDuration = 0.18f;

    // Umbrales de altura relativa (0 = pies, 1 = cabeza)
    private const float HEAD_THRESHOLD  = 0.72f;
    private const float TORSO_THRESHOLD = 0.35f;

    private EnemyController enemyController;
    private CharacterController characterController;
    private Animator animator;
    private bool reacting = false;

    void Awake()
    {
        enemyController       = GetComponent<EnemyController>();
        characterController   = GetComponent<CharacterController>();
        animator              = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Llama esto cuando la bala impacta al enemigo (y aún está vivo).
    /// </summary>
    public void ReactToHit(Vector3 hitPoint, Vector3 hitDirection)
    {
        if (reacting || enemyController.IsDead) return;

        Rigidbody[] bones = GetComponentsInChildren<Rigidbody>();
        if (bones.Length == 0) return;

        // Determinar zona por altura relativa del impacto
        float charHeight = characterController != null ? characterController.height : 1.8f;
        float relativeHeight = Mathf.Clamp01((hitPoint.y - transform.position.y) / charHeight);

        float force;
        if (relativeHeight >= HEAD_THRESHOLD)
            force = headForce;
        else if (relativeHeight >= TORSO_THRESHOLD)
            force = torsoForce;
        else
            force = legsForce;

        // Hueso más cercano al punto de impacto
        Rigidbody targetBone = GetClosestBone(bones, hitPoint);
        if (targetBone == null) return;

        StartCoroutine(HitReactionRoutine(targetBone, hitDirection, force));
    }

    private Rigidbody GetClosestBone(Rigidbody[] bones, Vector3 hitPoint)
    {
        Rigidbody closest = null;
        float closestDist = float.MaxValue;
        foreach (var rb in bones)
        {
            float dist = Vector3.Distance(rb.transform.position, hitPoint);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = rb;
            }
        }
        return closest;
    }

    private IEnumerator HitReactionRoutine(Rigidbody bone, Vector3 hitDirection, float force)
    {
        reacting = true;

        // Pausar el Animator para que no luche con la física
        if (animator != null) animator.speed = 0f;

        bone.isKinematic = false;
        bone.AddForce(hitDirection.normalized * force, ForceMode.Impulse);

        yield return new WaitForSeconds(reactionDuration);

        // Restaurar: volver a cinemático y reanudar Animator
        if (bone != null)
            bone.isKinematic = true;

        if (animator != null) animator.speed = 1f;

        reacting = false;
    }
}
