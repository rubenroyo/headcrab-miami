using UnityEngine;

/// <summary>
/// Añade inercia al Camera Target para crear el efecto de cámara tipo Mario Odyssey.
///
/// SETUP:
///   1. Coloca este script en el GameObject "Camera Target" (hijo del Player).
///   2. En Awake se desvincula del Player y pasa a seguirle con SmoothDamp.
///      La cámara de Cinemachine —apuntada a este objeto— hereda el lag de forma natural.
///
/// IDEA:
///   El target se "queda atrás" ligeramente cuando el player acelera o gira,
///   lo que hace que la cámara "mire hacia donde va" sin ningún post-proceso.
/// </summary>
public class CameraTargetDamping : MonoBehaviour
{
    [Header("Damping horizontal (XZ)")]
    [Tooltip("Tiempo de suavizado en los ejes X y Z. 0 = sin lag.")]
    [SerializeField] private float horizontalSmoothTime = 0.12f;

    [Tooltip("Velocidad máxima de desplazamiento horizontal del target.")]
    [SerializeField] private float maxHorizontalSpeed = 30f;

    [Header("Damping vertical (Y)")]
    [Tooltip("Tiempo de suavizado en el eje Y. Normalmente menor que horizontal.")]
    [SerializeField] private float verticalSmoothTime = 0.05f;

    [Tooltip("Velocidad máxima de desplazamiento vertical del target.")]
    [SerializeField] private float maxVerticalSpeed = 20f;

    [Header("Offset")]
    [Tooltip("Desplazamiento fijo respecto al player (capturado automáticamente en Awake).")]
    [SerializeField] private bool overrideOffset = false;
    [SerializeField] private Vector3 manualOffset = Vector3.zero;

    [Header("Debug")]
    [SerializeField] private bool drawGizmo = false;

    // ── referencias ──────────────────────────────────────────────────────
    private Transform playerTransform;

    // Offset en espacio mundo desde el player hasta este target.
    // Se calcula en Awake justo antes de desvincularse del padre.
    private Vector3 worldOffset;

    // Velocidades para SmoothDamp (una por eje para poder usar smoothTimes distintos)
    private float velX, velY, velZ;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (transform.parent == null)
        {
            Debug.LogWarning("[CameraTargetDamping] No hay padre. El script requiere que Camera Target sea hijo del Player.", this);
            enabled = false;
            return;
        }

        playerTransform = transform.parent;

        // Calcular offset en mundo antes de desvincular
        worldOffset = overrideOffset ? manualOffset : transform.position - playerTransform.position;

        // Desvincular: el Camera Target pasa a ser un objeto raíz independiente
        transform.SetParent(null);
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        // Posición objetivo: donde estaría el target si siguiese al player al instante
        Vector3 goal = playerTransform.position + worldOffset;

        Vector3 cur = transform.position;

        float x = Mathf.SmoothDamp(cur.x, goal.x, ref velX, horizontalSmoothTime, maxHorizontalSpeed);
        float y = Mathf.SmoothDamp(cur.y, goal.y, ref velY, verticalSmoothTime,   maxVerticalSpeed);
        float z = Mathf.SmoothDamp(cur.z, goal.z, ref velZ, horizontalSmoothTime, maxHorizontalSpeed);

        transform.position = new Vector3(x, y, z);
    }

    // ── utilidad pública ─────────────────────────────────────────────────

    /// <summary>
    /// Teletransporta el target a la posición exacta del player (sin lag).
    /// Útil al cargar escena o al hacer respawn.
    /// </summary>
    public void SnapToPlayer()
    {
        if (playerTransform == null) return;

        transform.position = playerTransform.position + worldOffset;
        velX = velY = velZ = 0f;
    }

    // ── gizmos ────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;
        if (playerTransform == null) return;

        Vector3 goal = playerTransform.position + worldOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, goal);
        Gizmos.DrawWireSphere(goal, 0.08f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.08f);
    }
}
