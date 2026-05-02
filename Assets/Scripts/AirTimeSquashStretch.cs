using UnityEngine;

/// <summary>
/// Squash & Stretch en el aire: deforma el modelo del jugador según su velocidad
/// y dirección de movimiento mientras está airborne. Efecto cartoon clásico.
/// 
/// Coloca este componente en el mismo GameObject que PlayerController.
/// El Transform objetivo debe ser el hijo que contiene el mesh, NO el raíz,
/// para no afectar al CharacterController ni a los colliders.
/// </summary>
public class AirTimeSquashStretch : MonoBehaviour
{
    [Header("Activación")]
    [Tooltip("Activa o desactiva el efecto en tiempo de ejecución sin quitar el componente.")]
    public bool enabled = true;

    [Header("Referencias")]
    [Tooltip("Transform del hijo que contiene el mesh del jugador.")]
    [SerializeField] private Transform modelTransform;

    [Header("Stretch — eje de movimiento")]
    [Tooltip("Cuánto se estira el modelo en la dirección del movimiento al ir a máxima velocidad.")]
    [SerializeField] private float maxStretch = 0.4f;

    [Tooltip("Cuánto se aplasta en los ejes perpendiculares al estirarse.")]
    [SerializeField] private float maxSquash = 0.2f;

    [Tooltip("Velocidad total (horizontal + vertical) a partir de la cual se aplica el efecto máximo.")]
    [SerializeField] private float maxSpeedReference = 18f;

    [Tooltip("Velocidad mínima en el aire para que empiece a aplicarse cualquier deformación.")]
    [SerializeField] private float minSpeedThreshold = 2f;

    [Header("Suavizado")]
    [Tooltip("Velocidad a la que la escala vuelve a la normalidad al aterrizar o al ralentizarse.")]
    [SerializeField] private float returnSpeed = 8f;

    [Tooltip("Velocidad a la que la escala se adapta al cambio de dirección en el aire.")]
    [SerializeField] private float stretchSpeed = 10f;

    // ─────────────────────────────────────────────
    //  PRIVADOS
    // ─────────────────────────────────────────────

    private PlayerController playerController;
    private Vector3 currentScale = Vector3.one;
    private Vector3 originalScale;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (playerController == null)
            Debug.LogWarning("[AirTimeSquashStretch] No se encontró PlayerController en el mismo GameObject.");

        if (modelTransform == null)
            Debug.LogWarning("[AirTimeSquashStretch] Model Transform no asignado. Asígnalo en el Inspector.");
        else
            originalScale = modelTransform.localScale;

        currentScale = originalScale;
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────

    void Update()
    {
        if (modelTransform == null || playerController == null) return;

        Vector3 targetScale;

        if (!enabled || playerController.IsGrounded)
        {
            // En suelo o con el efecto desactivado: volver suavemente a escala original
            targetScale = originalScale;
        }
        else
        {
            targetScale = CalculateStretchScale();
        }

        currentScale = Vector3.Lerp(currentScale, targetScale,
            (playerController.IsGrounded ? returnSpeed : stretchSpeed) * Time.deltaTime);

        modelTransform.localScale = currentScale;
    }

    // ─────────────────────────────────────────────
    //  CÁLCULO
    // ─────────────────────────────────────────────

    Vector3 CalculateStretchScale()
    {
        // Vector de velocidad completo en espacio mundo
        Vector3 velocity = new Vector3(
            playerController.CurrentHorizontalSpeed > 0f
                ? (transform.forward * playerController.CurrentHorizontalSpeed).x
                : 0f,
            playerController.VerticalVelocity,
            playerController.CurrentHorizontalSpeed > 0f
                ? (transform.forward * playerController.CurrentHorizontalSpeed).z
                : 0f
        );

        // Usamos la dirección real del movimiento horizontal + vertical
        // Reconstruimos el vector de velocidad desde la velocidad horizontal y vertical
        Vector3 horizontalVelocity = transform.forward * playerController.CurrentHorizontalSpeed;
        velocity = new Vector3(horizontalVelocity.x, playerController.VerticalVelocity, horizontalVelocity.z);

        float speed = velocity.magnitude;

        if (speed < minSpeedThreshold)
            return originalScale;

        // t = 0 en minSpeedThreshold, t = 1 en maxSpeedReference
        float t = Mathf.Clamp01((speed - minSpeedThreshold) / (maxSpeedReference - minSpeedThreshold));

        // Dirección del movimiento en espacio local del modelo
        Vector3 moveDir = velocity.normalized;
        Vector3 localMoveDir = modelTransform.InverseTransformDirection(moveDir);

        // El stretch se aplica sobre la escala original para respetar
        // si el modelo ya tiene una escala no uniforme de base
        float stretchAmount = maxStretch * t;
        float squashAmount  = maxSquash  * t;

        // Calculamos cuánto contribuye cada eje local a la dirección del movimiento
        // y aplicamos stretch en la dirección dominante, squash en las perpendiculares
        float absX = Mathf.Abs(localMoveDir.x);
        float absY = Mathf.Abs(localMoveDir.y);
        float absZ = Mathf.Abs(localMoveDir.z);

        Vector3 scale = originalScale;
        scale.x += (absX * stretchAmount) - ((absY + absZ) * 0.5f * squashAmount);
        scale.y += (absY * stretchAmount) - ((absX + absZ) * 0.5f * squashAmount);
        scale.z += (absZ * stretchAmount) - ((absX + absY) * 0.5f * squashAmount);

        // Clamp para que nunca llegue a escala negativa o ridícula
        scale.x = Mathf.Max(scale.x, 0.1f);
        scale.y = Mathf.Max(scale.y, 0.1f);
        scale.z = Mathf.Max(scale.z, 0.1f);

        return scale;
    }
}
