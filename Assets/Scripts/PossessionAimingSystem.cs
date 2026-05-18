using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema de apuntado para posesión.
/// Muestra un círculo discontinuo en el suelo (LineRenderers) y resalta enemigos:
///   - ROJO  → todos los enemigos posesionables dentro del rango
///   - BLANCO → el mejor objetivo según la dirección horizontal de la cámara
/// </summary>
public class PossessionAimingSystem : MonoBehaviour
{
    [Header("Rango")]
    [SerializeField] private float possessionRange = 15f;

    [Header("Círculo de rango")]
    [SerializeField] private Color circleColor = new Color(1f, 0.08f, 0.08f, 1f);
    [SerializeField] private float circleWidth = 0.08f;
    [Tooltip("Número de trazos discontinuos.")]
    [SerializeField] private int dashCount = 20;
    [Tooltip("Fracción de cada segmento que está rellena (0=todo hueco, 1=círculo completo).")]
    [SerializeField, Range(0.1f, 0.95f)] private float dashFill = 0.55f;
    [Tooltip("Puntos por trazo — más = arco más suave.")]
    [SerializeField] private int pointsPerDash = 8;
    [Tooltip("Distancia al suelo medida desde el pivot del jugador.")]
    [SerializeField] private float groundYOffset = 0.05f;

    [Header("Targeting")]
    [Tooltip("Peso de la dirección de la cámara vs distancia.\n" +
             "1 = puramente por dirección de cámara, 0 = puramente por distancia.")]
    [SerializeField, Range(0f, 1f)] private float directionWeight = 0.75f;

    // ─────────────────────────────────────────────
    //  ESTADO
    // ─────────────────────────────────────────────

    private Camera mainCamera;
    private bool isActive;
    private List<LineRenderer> dashRenderers = new();
    private Material dashMaterial;

    private readonly List<EnemyController> inRange = new();
    private EnemyController bestTarget;

    public EnemyController BestTarget => bestTarget;
    public bool HasTarget  => bestTarget != null;
    public bool IsActive   => isActive;
    public float Range     => possessionRange;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Awake()
    {
        dashMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        dashMaterial.color = circleColor;

        BuildDashRenderers();
        SetCircleVisible(false);
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void BuildDashRenderers()
    {
        foreach (var lr in dashRenderers)
            if (lr != null) Destroy(lr.gameObject);
        dashRenderers.Clear();

        for (int i = 0; i < dashCount; i++)
        {
            var go = new GameObject($"Dash_{i}");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.material          = dashMaterial;
            lr.startWidth        = circleWidth;
            lr.endWidth          = circleWidth;
            lr.useWorldSpace     = true;
            lr.positionCount     = pointsPerDash;
            lr.numCapVertices    = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows    = false;

            dashRenderers.Add(lr);
        }
    }

    // ─────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────

    public void Activate()
    {
        if (isActive) return;
        isActive = true;
        if (mainCamera == null) mainCamera = Camera.main;
        SetCircleVisible(true);
    }

    public void Deactivate()
    {
        if (!isActive) return;
        isActive = false;
        SetCircleVisible(false);
        ClearAllHighlights();
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────

    void Update()
    {
        if (!isActive) return;

        UpdateCirclePositions();
        RefreshInRange();
        UpdateBestTarget();
        ApplyHighlights();
    }

    // ─────────────────────────────────────────────
    //  CÍRCULO DISCONTINUO
    // ─────────────────────────────────────────────

    void UpdateCirclePositions()
    {
        float groundY = transform.position.y - 1f;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down,
                            out RaycastHit hit, 10f))
            groundY = hit.point.y + groundYOffset;

        float segmentAngle = 360f / dashCount;
        float dashAngle    = segmentAngle * dashFill;
        float gapHalf      = (segmentAngle - dashAngle) * 0.5f;

        for (int i = 0; i < dashCount; i++)
        {
            float startDeg = i * segmentAngle + gapHalf;
            float endDeg   = startDeg + dashAngle;

            for (int p = 0; p < pointsPerDash; p++)
            {
                float t     = p / (float)(pointsPerDash - 1);
                float angle = Mathf.Lerp(startDeg, endDeg, t) * Mathf.Deg2Rad;
                float x     = transform.position.x + Mathf.Cos(angle) * possessionRange;
                float z     = transform.position.z + Mathf.Sin(angle) * possessionRange;
                dashRenderers[i].SetPosition(p, new Vector3(x, groundY, z));
            }
        }
    }

    void SetCircleVisible(bool v)
    {
        foreach (var lr in dashRenderers)
            if (lr != null) lr.gameObject.SetActive(v);
    }

    // ─────────────────────────────────────────────
    //  DETECCIÓN
    // ─────────────────────────────────────────────

    void RefreshInRange()
    {
        for (int i = inRange.Count - 1; i >= 0; i--)
        {
            var ec = inRange[i];
            if (ec == null || ec.IsDead || ec.IsInjured ||
                Vector3.Distance(transform.position, ec.transform.position) > possessionRange)
            {
                ec?.SetPossessionHighlight(PossessionHighlightType.None);
                inRange.RemoveAt(i);
            }
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, possessionRange);
        foreach (var col in hits)
        {
            var ec = col.GetComponentInParent<EnemyController>();
            if (ec == null || !ec.CanBePossessed || ec.IsPossessed || ec.IsDead || ec.IsInjured) continue;
            if (!inRange.Contains(ec))
                inRange.Add(ec);
        }
    }

    // ─────────────────────────────────────────────
    //  SCORING
    // ─────────────────────────────────────────────

    void UpdateBestTarget()
    {
        if (inRange.Count == 0) { bestTarget = null; return; }

        Vector3 camFwdXZ = mainCamera != null
            ? Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up)
            : transform.forward;
        if (camFwdXZ.sqrMagnitude > 0.001f) camFwdXZ.Normalize();

        EnemyController best      = null;
        float           bestScore = float.MinValue;

        foreach (var ec in inRange)
        {
            if (ec == null) continue;

            Vector3 toEnemyXZ = Vector3.ProjectOnPlane(
                ec.transform.position - transform.position, Vector3.up);
            float dist = toEnemyXZ.magnitude;
            if (dist < 0.01f) continue;

            float alignment = Vector3.Dot(toEnemyXZ / dist, camFwdXZ);
            float distScore = 1f - Mathf.Clamp01(dist / possessionRange);
            float score     = alignment * directionWeight + distScore * (1f - directionWeight);

            if (score > bestScore) { bestScore = score; best = ec; }
        }

        bestTarget = best;
    }

    // ─────────────────────────────────────────────
    //  HIGHLIGHTS
    // ─────────────────────────────────────────────

    void ApplyHighlights()
    {
        foreach (var ec in inRange)
        {
            if (ec == null) continue;
            ec.SetPossessionHighlight(ec == bestTarget
                ? PossessionHighlightType.Targeted
                : PossessionHighlightType.Possessable);
        }
    }

    void ClearAllHighlights()
    {
        foreach (var ec in inRange)
            ec?.SetPossessionHighlight(PossessionHighlightType.None);
        inRange.Clear();
        bestTarget = null;
    }
}