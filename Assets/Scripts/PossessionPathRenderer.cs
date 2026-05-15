using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Renderiza dos líneas desde el jugador hasta el objetivo de posesión:
///   - AMARILLO: ruta óptima de NavMesh (tal cual salen los corners)
///   - VERDE:    versión orgánica — Catmull-Rom + desplazamiento Perlin perpendicular
///
/// Ponlo en el mismo GameObject que PossessionAimingSystem.
/// La ruta se recalcula cada frame para seguir al jugador si se mueve.
/// El patrón orgánico se regenera sólo cuando cambia el objetivo.
/// </summary>
public class PossessionPathRenderer : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────

    [Header("Referencias")]
    [SerializeField] private PossessionAimingSystem aimingSystem;

    [Header("Línea NavMesh")]
    [SerializeField] private Color  navColor = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private float  navWidth = 0.05f;

    [Header("Línea Orgánica")]
    [SerializeField] private Color  organicColor = new Color(0.15f, 1f, 0.3f, 1f);
    [SerializeField] private float  organicWidth = 0.07f;

    [Header("Spline (Catmull-Rom)")]
    [Tooltip("Puntos interpolados entre cada par de waypoints NavMesh. " +
             "Más = línea más suave, más coste.")]
    [SerializeField] private int stepsPerSegment = 14;

    [Header("Deformación orgánica")]
    [Tooltip("Amplitud máxima del desplazamiento lateral en unidades de mundo.")]
    [SerializeField, Range(0f, 5f)]   private float organicStrength  = 0.8f;
    [Tooltip("Frecuencia espacial de las curvas. Más alto = más ondulado.")]
    [SerializeField, Range(0.1f, 8f)] private float organicFrequency = 1.5f;
    [Tooltip("Pasos de la segunda spline Catmull-Rom sobre los puntos desplazados. " +
             "Más alto = curvas más suaves.")]
    [SerializeField] private int organicSplineSteps = 8;

    [Header("Suelo")]
    [Tooltip("Elevación sobre el suelo para que las líneas sean visibles.")]
    [SerializeField] private float groundYOffset = 0.06f;

    // ─────────────────────────────────────────────
    //  ESTADO
    // ─────────────────────────────────────────────

    private LineRenderer    navLine;
    private LineRenderer    organicLine;
    private NavMeshPath     navPath;
    private EnemyController lastTarget;
    private float           currentNoiseSeed;   // cambia por objetivo
    private List<Vector3>   currentOrganicPath = new List<Vector3>();
    private bool            frozen;             // true: no recalcular, líneas congeladas

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Awake()
    {
        navPath      = new NavMeshPath();
        navLine      = CreateLine("NavPathLine",     navColor,     navWidth);
        organicLine  = CreateLine("OrganicPathLine", organicColor, organicWidth);
        SetVisible(false);
    }

    void Start()
    {
        if (aimingSystem == null)
            aimingSystem = GetComponent<PossessionAimingSystem>();
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────

    // ─────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>Devuelve una copia de la última ruta orgánica calculada.</summary>
    public List<Vector3> GetCurrentOrganicPath() => new List<Vector3>(currentOrganicPath);

    /// <summary>True si hay una ruta orgánica válida con al menos 2 puntos.</summary>
    public bool HasPath => currentOrganicPath.Count > 1;

    /// <summary>
    /// Congela las líneas: dejan de recalcularse pero permanecen visibles.
    /// Llámalo al iniciar el viaje de posesión.
    /// </summary>
    public void Freeze() => frozen = true;

    /// <summary>
    /// Descongela y borra las líneas. Llámalo al terminar la posesión.
    /// </summary>
    public void UnfreezeAndClear()
    {
        frozen = false;
        ClearPaths();
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────

    void Update()
    {
        if (frozen) return; // congelado: no tocar las líneas

        if (aimingSystem == null || !aimingSystem.IsActive)
        {
            if (lastTarget != null) ClearPaths();
            return;
        }

        EnemyController target = aimingSystem.BestTarget;

        if (target == null)
        {
            ClearPaths();
            return;
        }

        // Nuevo objetivo → nueva semilla de ruido
        if (target != lastTarget)
        {
            lastTarget       = target;
            currentNoiseSeed = Random.Range(0f, 100f);
        }

        BuildAndDrawPaths(target);
    }

    // ─────────────────────────────────────────────
    //  PATHS
    // ─────────────────────────────────────────────

    void BuildAndDrawPaths(EnemyController target)
    {
        bool found = NavMesh.CalculatePath(
            transform.position, target.transform.position,
            NavMesh.AllAreas, navPath);

        if (!found || navPath.corners.Length < 2)
        {
            ClearPaths();
            return;
        }

        // ── Línea NavMesh ──────────────────────────
        DrawNavPath(navPath.corners);

        // ── Línea orgánica ─────────────────────────
        List<Vector3> spline   = BuildCatmullRomSpline(navPath.corners);
        List<Vector3> displaced = ApplyOrganicDeformation(spline);
        List<Vector3> organic   = SmoothenWithCatmullRom(displaced, organicSplineSteps);
        currentOrganicPath      = organic;   // guardamos la ruta para que PlayerController la siga
        DrawList(organicLine, organic);

        SetVisible(true);
    }

    // ─────────────────────────────────────────────
    //  LÍNEA NAVMESH DIRECTA
    // ─────────────────────────────────────────────

    void DrawNavPath(Vector3[] corners)
    {
        navLine.positionCount = corners.Length;
        for (int i = 0; i < corners.Length; i++)
        {
            var p = corners[i];
            p.y += groundYOffset;
            navLine.SetPosition(i, p);
        }
    }

    // ─────────────────────────────────────────────
    //  CATMULL-ROM SPLINE
    // ─────────────────────────────────────────────

    List<Vector3> BuildCatmullRomSpline(Vector3[] pts)
    {
        var result = new List<Vector3>();

        for (int i = 0; i < pts.Length - 1; i++)
        {
            Vector3 p0 = pts[Mathf.Max(i - 1, 0)];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = pts[Mathf.Min(i + 2, pts.Length - 1)];

            for (int s = 0; s < stepsPerSegment; s++)
            {
                float   t  = s / (float)stepsPerSegment;
                Vector3 pt = CatmullRom(p0, p1, p2, p3, t);
                pt.y += groundYOffset;
                result.Add(pt);
            }
        }

        // Último punto exacto
        Vector3 last = pts[pts.Length - 1];
        last.y += groundYOffset;
        result.Add(last);

        return result;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2)                    * t +
            (2f*p0 - 5f*p1 + 4f*p2 - p3) * t2 +
            (-p0 + 3f*p1 - 3f*p2 + p3)   * t3
        );
    }

    // ─────────────────────────────────────────────
    //  DEFORMACIÓN ORGÁNICA
    // ─────────────────────────────────────────────

    List<Vector3> ApplyOrganicDeformation(List<Vector3> spline)
    {
        int          n      = spline.Count;
        var          result = new List<Vector3>(n);

        for (int i = 0; i < n; i++)
        {
            float t        = i / (float)(n - 1);
            // Envolvente senoidal: 0 en extremos, máximo en el centro → los extremos
            // permanecen anclados al jugador y al enemigo
            float envelope = Mathf.Sin(t * Mathf.PI);

            // Tangente local en XZ
            Vector3 tangent;
            if      (i < n - 1) tangent = spline[i + 1] - spline[i];
            else                tangent = spline[i]     - spline[i - 1];
            tangent.y = 0f;
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector3.forward;
            tangent.Normalize();

            // Vector perpendicular en XZ (rotar 90° a la derecha)
            Vector3 perp = new Vector3(-tangent.z, 0f, tangent.x);

            // Perlin noise con la semilla fija para este objetivo
            float noiseX = currentNoiseSeed + t * organicFrequency * 5f;
            float noiseY = currentNoiseSeed * 0.37f;
            float noise  = Mathf.PerlinNoise(noiseX, noiseY) * 2f - 1f; // [-1, 1]

            Vector3 pt = spline[i] + perp * (noise * organicStrength * envelope);
            pt.y = spline[i].y;   // mantener Y del spline (ya tiene groundYOffset)
            result.Add(pt);
        }

        return result;
    }

    // ─────────────────────────────────────────────
    //  SEGUNDA PASADA CATMULL-ROM SOBRE PUNTOS DESPLAZADOS
    // ─────────────────────────────────────────────

    List<Vector3> SmoothenWithCatmullRom(List<Vector3> pts, int steps)
    {
        if (pts.Count < 2) return pts;

        var result = new List<Vector3>();
        int n = pts.Count;

        for (int i = 0; i < n - 1; i++)
        {
            Vector3 p0 = pts[Mathf.Max(i - 1, 0)];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = pts[Mathf.Min(i + 2, n - 1)];

            for (int s = 0; s < steps; s++)
            {
                float t = s / (float)steps;
                result.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        result.Add(pts[n - 1]);
        return result;
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    void DrawList(LineRenderer lr, List<Vector3> pts)
    {
        lr.positionCount = pts.Count;
        for (int i = 0; i < pts.Count; i++)
            lr.SetPosition(i, pts[i]);
    }

    void ClearPaths()
    {
        lastTarget                = null;
        currentOrganicPath.Clear();
        navLine.positionCount     = 0;
        organicLine.positionCount = 0;
        SetVisible(false);
    }

    void SetVisible(bool v)
    {
        navLine.gameObject.SetActive(v);
        organicLine.gameObject.SetActive(v);
    }

    LineRenderer CreateLine(string goName, Color color, float width)
    {
        var go = new GameObject(goName);
        go.transform.SetParent(transform, false);

        var lr = go.AddComponent<LineRenderer>();

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.color = color;

        lr.material          = mat;
        lr.startWidth        = width;
        lr.endWidth          = width;
        lr.useWorldSpace     = true;
        lr.positionCount     = 0;
        lr.numCapVertices    = 3;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows    = false;

        return lr;
    }
}
