using UnityEngine;

/// <summary>
/// Animación procedural de patas para araña usando Animation Rigging.
///
/// SETUP EN UNITY:
///  1. Coloca este script en el Game Object RAÍZ de la araña (Spider).
///  2. Los IK Targets deben ser hijos de ese mismo objeto raíz.
///  3. Arrastra los 8 IK Targets al array legIKTargets.
///  4. Asigna una LayerMask de "suelo" en groundLayer para que el raycast no detecte la propia araña.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SpiderProceduralAnimation : MonoBehaviour
{
    [Header("Leg IK Targets")]
    [Tooltip("Arrastra aquí los IK Targets en orden. Deben ser hijos de este objeto raíz.")]
    public Transform[] legIKTargets;

    [Header("Step Settings")]
    [Tooltip("Distancia mínima entre la posición actual del pie y su posición ideal para disparar un paso.")]
    public float stepDistance = 1f;

    [Tooltip("Altura máxima del arco durante el paso. Más alto = paso más exagerado.")]
    public float stepHeight = 0.5f;

    [Tooltip("Velocidad de interpolación del paso. Prueba con valores entre 5 y 10.")]
    public float stepSpeed = 5f;

    [Header("Cycle Settings")]
    [Tooltip("Velocidad del ciclo de comprobación de pasos. Más alto = las patas reaccionan más rápido.")]
    public float cycleSpeed = 2f;

    [Tooltip("Fracción del ciclo durante la cual cada pata puede dar un paso (0-1). Prueba con 0.1.")]
    [Range(0.01f, 0.5f)]
    public float cycleLimit = 0.1f;

    [Tooltip(
        "Offset de ciclo entre patas consecutivas (0-1).\n" +
        "1/numPatas = una pata a la vez.\n" +
        "Para 8 patas: 0.125 = una a la vez, 0.0625 = dos a la vez.")]
    public float timingOffset = 0.125f;

    [Header("Manual Timings")]
    [Tooltip("Activa para asignar el offset de ciclo de cada pata individualmente.")]
    public bool setTimingsManually = false;

    [Tooltip("Un valor (0-1) por pata. Solo se usa si setTimingsManually está activado.")]
    public float[] manualTimings;

    [Header("Ground Detection")]
    [Tooltip("Radio del SphereCast para detectar el suelo bajo la posición ideal de cada pata.")]
    public float sphereCastRadius = 0.1f;

    [Tooltip("Distancia máxima del raycast hacia abajo.")]
    public float raycastRange = 2f;

    [Tooltip("Altura sobre la posición ideal desde donde empieza el raycast.")]
    public float raycastOffset = 1f;

    [Tooltip("Capas del suelo. DESACTIVA la capa de la propia araña para evitar autodetección.")]
    public LayerMask groundLayer = ~0;

    // ---------- Estado interno por pata ----------
    private Vector3[] homeOffsets;    // Posición inicial de cada pata en espacio LOCAL del root
    private Vector3[] footRestPos;    // Posición donde descansa el pie ahora mismo (world space)
    private Vector3[] stepFrom;       // Inicio del paso actual
    private Vector3[] stepTo;         // Destino del paso actual
    private float[]   stepProgress;   // Progreso del paso actual (0-1)
    private bool[]    isMoving;       // ¿Está esta pata en medio de un paso?
    private float[]   legTimings;     // Offset de ciclo asignado a esta pata

    private float cycleTimer;

    // Expuesto para SpiderBodyController
    public Vector3[] FootRestPositions => footRestPos;

    // -------------------------------------------------------

    void Start()
    {
        if (legIKTargets == null || legIKTargets.Length == 0)
        {
            Debug.LogError("[SpiderProceduralAnimation] No hay IK Targets asignados.", this);
            enabled = false;
            return;
        }

        int n = legIKTargets.Length;
        homeOffsets  = new Vector3[n];
        footRestPos  = new Vector3[n];
        stepFrom     = new Vector3[n];
        stepTo       = new Vector3[n];
        stepProgress = new float[n];
        isMoving     = new bool[n];
        legTimings   = new float[n];

        for (int i = 0; i < n; i++)
        {
            // Guardar el offset LOCAL para que la posición ideal siga la rotación del root
            homeOffsets[i]  = transform.InverseTransformPoint(legIKTargets[i].position);
            footRestPos[i]  = legIKTargets[i].position;
            stepProgress[i] = 1f; // Empezar como si el paso estuviese completado

            // Asignar timing
            if (setTimingsManually && manualTimings != null && i < manualTimings.Length)
                legTimings[i] = Mathf.Clamp01(manualTimings[i]);
            else
                legTimings[i] = (timingOffset * i) % 1f;
        }
    }

    void Update()
    {
        cycleTimer = (cycleTimer + Time.deltaTime * cycleSpeed) % 1f;

        for (int i = 0; i < legIKTargets.Length; i++)
            UpdateLeg(i);
    }

    void UpdateLeg(int i)
    {
        // ---- Si hay un paso en curso, animarlo ----
        if (isMoving[i])
        {
            stepProgress[i] = Mathf.MoveTowards(stepProgress[i], 1f, Time.deltaTime * stepSpeed);

            // SmoothStep para aceleración/deceleración + arco sinusoidal para la altura
            float t = Mathf.SmoothStep(0f, 1f, stepProgress[i]);
            Vector3 p = Vector3.Lerp(stepFrom[i], stepTo[i], t);
            p.y += stepHeight * Mathf.Sin(t * Mathf.PI);

            legIKTargets[i].position = p;

            if (stepProgress[i] >= 1f)
            {
                isMoving[i]              = false;
                footRestPos[i]           = stepTo[i];
                legIKTargets[i].position = footRestPos[i];
            }
            return;
        }

        // ---- Comprobar si estamos en la ventana de ciclo de esta pata ----
        float delta = cycleTimer - legTimings[i];
        if (delta < 0f) delta += 1f; // Wrap
        if (delta > cycleLimit) return;

        // ---- Calcular posición ideal y decidir si dar un paso ----
        Vector3 ideal = GetIdealPosition(i);

        if (Vector3.Distance(footRestPos[i], ideal) < stepDistance) return;

        // ---- Iniciar paso ----
        isMoving[i]     = true;
        stepProgress[i] = 0f;
        stepFrom[i]     = legIKTargets[i].position;
        stepTo[i]       = ideal;
    }

    private Vector3 GetIdealPosition(int i)
    {
        // El offset local se convierte a espacio mundial siguiendo la traslación Y rotación del root
        Vector3 worldHome = transform.TransformPoint(homeOffsets[i]);
        Vector3 origin    = worldHome + Vector3.up * raycastOffset;

        if (Physics.SphereCast(origin, sphereCastRadius, Vector3.down, out RaycastHit hit,
                               raycastRange + raycastOffset, groundLayer))
        {
            return hit.point;
        }

        // Fallback si no hay suelo detectado
        return worldHome;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || legIKTargets == null) return;

        for (int i = 0; i < legIKTargets.Length; i++)
        {
            Vector3 ideal = GetIdealPosition(i);

            // Posición ideal (amarillo)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(ideal, 0.07f);

            // Posición de reposo actual (verde = quieto, rojo = en movimiento)
            Gizmos.color = isMoving[i] ? Color.red : Color.green;
            Gizmos.DrawWireSphere(footRestPos[i], 0.1f);

            // Línea pie → ideal
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawLine(footRestPos[i], ideal);
        }
    }
}
