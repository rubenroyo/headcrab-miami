using UnityEngine;

public class LegGroundSnap : MonoBehaviour
{
    [Header("IK Targets")]
    public Transform[] legTargets;

    [Header("Next Targets")]
    [Tooltip("Empty GameObjects hijos del spider. Se mueven con el cuerpo por jerarquía.")]
    public Transform[] nextTargets;

    [Header("Stepping")]
    public float stepThreshold = 0.5f;
    public float stepHeight    = 0.3f;
    public float stepDuration  = 0.2f;

    [Header("Idle")]
    [Tooltip("Segundos quieto antes de pasar al idle.")]
    public float idleDelay = 2f;
    [Tooltip("Tiempo en segundos para interpolar las patas a la pose idle.")]
    public float idleBlendDuration = 0.4f;
    [Tooltip("Umbral de velocidad (unidades/s) por debajo del cual se considera quieto.")]
    public float movementThreshold = 0.02f;
    [Tooltip("Posiciones locales de cada pata en pose idle. Click derecho en el componente para capturarlas.")]
    public Vector3[] idleLegLocalPositions;

    // Posición plantada: fija en world space
    private Vector3[] plantedWorldPositions;

    // Estado del paso por pata
    private bool[]    isStepping;
    private Vector3[] stepFromPos;
    private Vector3[] stepToPos;
    private float[]   stepTime;

    // Estado idle
    private enum LegState { Walk, TransitionToIdle, Idle }
    private LegState legState    = LegState.Walk;
    private float    idleTimer   = 0f;
    private float    blendTimer  = 0f;
    private Vector3[] idleBlendFrom;
    private Vector3   lastPosition;

    void Start()
    {
        int n = legTargets.Length;
        plantedWorldPositions = new Vector3[n];
        isStepping            = new bool[n];
        stepFromPos           = new Vector3[n];
        stepToPos             = new Vector3[n];
        stepTime              = new float[n];
        idleBlendFrom         = new Vector3[n];

        for (int i = 0; i < n; i++)
            plantedWorldPositions[i] = legTargets[i].position;

        lastPosition = transform.position;
    }

    void LateUpdate()
    {
        float speed = Vector3.Distance(transform.position, lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
        bool moving = speed > movementThreshold;

        switch (legState)
        {
            case LegState.Walk:
                if (!moving)
                {
                    idleTimer += Time.deltaTime;
                    if (idleTimer >= idleDelay) EnterTransitionToIdle();
                }
                else
                {
                    idleTimer = 0f;
                }
                UpdateWalk();
                break;

            case LegState.TransitionToIdle:
                if (moving) { ExitIdle(); break; }
                blendTimer += Time.deltaTime;
                float bt = Mathf.Clamp01(blendTimer / idleBlendDuration);
                UpdateIdleBlend(bt);
                if (bt >= 1f) legState = LegState.Idle;
                break;

            case LegState.Idle:
                if (moving) { ExitIdle(); break; }
                for (int i = 0; i < legTargets.Length; i++)
                    legTargets[i].position = plantedWorldPositions[i];
                break;
        }
    }

    void UpdateWalk()
    {
        for (int i = 0; i < legTargets.Length; i++)
        {
            if (isStepping[i])
            {
                stepTime[i] += Time.deltaTime;
                float t = Mathf.Clamp01(stepTime[i] / stepDuration);

                Vector3 pos = Vector3.Lerp(stepFromPos[i], stepToPos[i], t);
                pos.y += stepHeight * Mathf.Sin(t * Mathf.PI);
                legTargets[i].position = pos;

                if (t >= 1f)
                {
                    isStepping[i]            = false;
                    plantedWorldPositions[i] = stepToPos[i];
                    legTargets[i].position   = plantedWorldPositions[i];
                }
            }
            else
            {
                if (i < nextTargets.Length && nextTargets[i] != null)
                {
                    float dist = Vector3.Distance(plantedWorldPositions[i], nextTargets[i].position);
                    if (dist > stepThreshold)
                    {
                        isStepping[i]  = true;
                        stepTime[i]    = 0f;
                        stepFromPos[i] = plantedWorldPositions[i];
                        stepToPos[i]   = nextTargets[i].position;
                    }
                }
                legTargets[i].position = plantedWorldPositions[i];
            }
        }
    }

    void EnterTransitionToIdle()
    {
        legState   = LegState.TransitionToIdle;
        blendTimer = 0f;
        for (int i = 0; i < legTargets.Length; i++)
        {
            isStepping[i]    = false;
            idleBlendFrom[i] = legTargets[i].position;
            if (nextTargets != null && i < nextTargets.Length && nextTargets[i] != null)
                nextTargets[i].gameObject.SetActive(false);
        }
    }

    void UpdateIdleBlend(float t)
    {
        if (idleLegLocalPositions == null) return;
        for (int i = 0; i < legTargets.Length; i++)
        {
            if (i >= idleLegLocalPositions.Length) break;
            Vector3 idleWorld        = transform.TransformPoint(idleLegLocalPositions[i]);
            Vector3 pos              = Vector3.Lerp(idleBlendFrom[i], idleWorld, t);
            legTargets[i].position   = pos;
            plantedWorldPositions[i] = pos;
        }
    }

    void ExitIdle()
    {
        legState  = LegState.Walk;
        idleTimer = 0f;
        for (int i = 0; i < legTargets.Length; i++)
        {
            if (nextTargets != null && i < nextTargets.Length && nextTargets[i] != null)
                nextTargets[i].gameObject.SetActive(true);
            plantedWorldPositions[i] = legTargets[i].position;
            isStepping[i] = false;
        }
    }

    [ContextMenu("Capture Current Leg Positions as Idle Pose")]
    void CaptureIdlePose()
    {
        idleLegLocalPositions = new Vector3[legTargets.Length];
        for (int i = 0; i < legTargets.Length; i++)
            idleLegLocalPositions[i] = transform.InverseTransformPoint(legTargets[i].position);
        Debug.Log("[LegGroundSnap] Idle pose capturada.");
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    void OnDrawGizmosSelected()
    {
        // Next targets (cyan)
        if (nextTargets != null)
        {
            for (int i = 0; i < nextTargets.Length; i++)
            {
                if (nextTargets[i] == null) continue;
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.DrawWireSphere(nextTargets[i].position, stepThreshold);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(nextTargets[i].position, 0.05f);
                if (legTargets != null && i < legTargets.Length && legTargets[i] != null)
                {
                    Gizmos.color = Application.isPlaying && isStepping != null && isStepping[i]
                        ? Color.magenta : Color.cyan;
                    Gizmos.DrawLine(nextTargets[i].position, legTargets[i].position);
                }
            }
        }

        // Idle pose preview (amarillo)
        if (idleLegLocalPositions != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < idleLegLocalPositions.Length; i++)
                Gizmos.DrawWireSphere(transform.TransformPoint(idleLegLocalPositions[i]), 0.06f);
        }
    }
}

