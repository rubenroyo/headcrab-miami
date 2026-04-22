using UnityEngine;

/// <summary>
/// Ajusta la posición vertical y la inclinación del Armature según donde están las patas,
/// dando sensación de peso y adaptación al terreno (efecto "smart body position" del vídeo).
///
/// SETUP EN UNITY:
///  1. Coloca este script en el ARMATURE (hijo directo del root Spider).
///  2. Arrastra los mismos IK Targets que en SpiderProceduralAnimation al array legIKTargets.
///  3. Ajusta bodyHeightSpeed y bodyTiltSpeed al gusto.
/// </summary>
[DefaultExecutionOrder(-50)]  // Corre después de SpiderProceduralAnimation (-100) pero antes de Animation Rigging
public class SpiderBodyController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Los mismos IK Targets que en SpiderProceduralAnimation.")]
    public Transform[] legIKTargets;

    [Header("Smart Body Position")]
    [Tooltip("Ajusta la altura del cuerpo según la media de las posiciones de las patas.")]
    public bool smartBodyPosition = true;

    [Tooltip("Velocidad de suavizado de la altura (más alto = cuerpo más pegado a las patas).")]
    public float bodyHeightSpeed = 5f;

    [Header("Body Tilt")]
    [Tooltip("Inclina el cuerpo para que siga la pendiente del suelo bajo las patas.")]
    public bool bodyTilt = true;

    [Tooltip("Velocidad de suavizado de la inclinación.")]
    public float bodyTiltSpeed = 5f;

    [Tooltip("Intensidad de la inclinación. 0 = ninguna, 1 = máxima (sigue exactamente el suelo).")]
    [Range(0f, 1f)]
    public float tiltStrength = 0.8f;

    // ---- Estado interno ----
    // Diferencia entre la Y del body y la Y media de las patas en el inicio de la escena.
    // Se preserva para que el cuerpo siempre flote a la misma distancia sobre las patas.
    private float baseBodyOffsetY;

    // -------------------------------------------------------

    void Start()
    {
        if (legIKTargets == null || legIKTargets.Length == 0)
        {
            Debug.LogWarning("[SpiderBodyController] No hay IK Targets asignados.", this);
            return;
        }

        // Calcular el offset base desde la posición inicial
        baseBodyOffsetY = transform.position.y - GetAverageLegY();
    }

    void Update()
    {
        if (legIKTargets == null || legIKTargets.Length == 0) return;

        if (smartBodyPosition) AdjustBodyHeight();
        if (bodyTilt)          AdjustBodyTilt();
    }

    // ---- Ajuste de altura ----

    private void AdjustBodyHeight()
    {
        float avgY    = GetAverageLegY();
        float targetY = avgY + baseBodyOffsetY;

        // Solo modificamos la Y en espacio mundial; X y Z las controla el movimiento externo
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * bodyHeightSpeed);
        transform.position = pos;
    }

    // ---- Ajuste de inclinación ----

    private void AdjustBodyTilt()
    {
        Vector3 legNormal = CalculateLegPlaneNormal();

        // Mezclar la normal del suelo con el "arriba" global según tiltStrength
        Vector3 blendedUp = Vector3.Lerp(Vector3.up, legNormal, tiltStrength).normalized;

        // Mantener la dirección de avance (yaw) y solo cambiar el "up"
        Vector3 forward = transform.forward;
        Vector3 right   = Vector3.Cross(blendedUp, forward).normalized;

        if (right == Vector3.zero) return; // Evitar división por cero si son paralelos

        forward = Vector3.Cross(right, blendedUp).normalized;

        Quaternion targetRot = Quaternion.LookRotation(forward, blendedUp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * bodyTiltSpeed);
    }

    // ---- Helpers ----

    private float GetAverageLegY()
    {
        float sum = 0f;
        foreach (var leg in legIKTargets)
            sum += leg.position.y;
        return sum / legIKTargets.Length;
    }

    /// <summary>
    /// Calcula la normal del plano aproximado definido por las patas,
    /// usando 3 puntos distribuidos uniformemente en el array.
    /// </summary>
    private Vector3 CalculateLegPlaneNormal()
    {
        int n = legIKTargets.Length;
        Vector3 a = legIKTargets[0].position;
        Vector3 b = legIKTargets[n / 3].position;
        Vector3 c = legIKTargets[n * 2 / 3].position;

        Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

        // Garantizar que la normal apunta hacia arriba
        return normal.y < 0f ? -normal : normal;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || legIKTargets == null || legIKTargets.Length == 0) return;

        // Mostrar la normal del plano de las patas
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, CalculateLegPlaneNormal() * 0.7f);
    }
}
