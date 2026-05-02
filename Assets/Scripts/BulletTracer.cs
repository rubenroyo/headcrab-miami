using UnityEngine;

/// <summary>
/// Efecto visual de disparo hitscan: línea desde el muzzle hasta el impacto
/// más un flash de luz en el muzzle. Se autodesactiva tras su duración.
/// 
/// Configuración del prefab:
///   - Este componente en el raíz
///   - Un LineRenderer en el mismo GameObject
///   - Un hijo llamado "MuzzleFlash" con un componente Light
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    [Header("Línea")]
    [Tooltip("Color de la línea al inicio (muzzle). Alpha controla la opacidad inicial.")]
    [SerializeField] private Color lineStartColor = new Color(1f, 0.95f, 0.7f, 0.8f);

    [Tooltip("Color de la línea al final (impacto).")]
    [SerializeField] private Color lineEndColor = new Color(0.8f, 0.8f, 0.8f, 0f);

    [Tooltip("Grosor de la línea en el muzzle.")]
    [SerializeField] private float lineStartWidth = 0.03f;

    [Tooltip("Grosor de la línea en el impacto.")]
    [SerializeField] private float lineEndWidth = 0.01f;

    [Header("Luz de muzzle flash")]
    [Tooltip("Intensidad máxima de la luz al inicio.")]
    [SerializeField] private float maxLightIntensity = 3f;

    [Tooltip("Color de la luz del flash.")]
    [SerializeField] private Color lightColor = new Color(1f, 0.85f, 0.5f);

    // ─────────────────────────────────────────────
    //  PRIVADOS
    // ─────────────────────────────────────────────

    private LineRenderer lineRenderer;
    private Light muzzleLight;

    private float duration;
    private float elapsed;
    private bool isActive;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // Buscar la luz en los hijos
        muzzleLight = GetComponentInChildren<Light>();
        if (muzzleLight != null)
        {
            muzzleLight.color = lightColor;
            muzzleLight.enabled = false;
        }
    }

    // ─────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Inicializa el tracer con origen, destino y duración.
    /// Llamar justo después de sacar del pool y activar el GameObject.
    /// </summary>
    public void Play(Vector3 origin, Vector3 destination, float tracerDuration)
    {
        duration = tracerDuration;
        elapsed  = 0f;
        isActive = true;

        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, destination);

        // Colores y grosor iniciales
        lineRenderer.startColor = lineStartColor;
        lineRenderer.endColor   = lineEndColor;
        lineRenderer.startWidth = lineStartWidth;
        lineRenderer.endWidth   = lineEndWidth;
        lineRenderer.enabled    = true;

        // Flash de luz
        if (muzzleLight != null)
        {
            muzzleLight.intensity = maxLightIntensity;
            muzzleLight.enabled   = true;
        }
    }

    // ─────────────────────────────────────────────
    //  UPDATE — fade out
    // ─────────────────────────────────────────────

    void Update()
    {
        if (!isActive) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration); // 0→1 durante la vida del tracer

        // Fade out de la línea
        float alpha = 1f - t;
        Color start = lineStartColor; start.a = lineStartColor.a * alpha;
        Color end   = lineEndColor;   end.a   = lineEndColor.a   * alpha;
        lineRenderer.startColor = start;
        lineRenderer.endColor   = end;

        // Fade out de la luz
        if (muzzleLight != null)
            muzzleLight.intensity = maxLightIntensity * (1f - t);

        // Desactivar al terminar
        if (t >= 1f)
            Deactivate();
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────

    private void Deactivate()
    {
        isActive = false;
        lineRenderer.enabled = false;
        if (muzzleLight != null) muzzleLight.enabled = false;
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        isActive = false;
        if (lineRenderer != null) lineRenderer.enabled = false;
        if (muzzleLight  != null) muzzleLight.enabled  = false;
    }
}
