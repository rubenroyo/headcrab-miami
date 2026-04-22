using UnityEngine;

/// <summary>
/// Visualiza la dirección de salto SOLO EN EL EDITOR (Gizmos).
/// Calcula la dirección de lanzamiento basada en la cámara.
/// El salto real usa física de Unity (AddForce + gravedad).
/// </summary>
public class JumpTrajectoryVisualizer : MonoBehaviour
{
    [Header("Jump Direction")]
    [Tooltip("Offset vertical aplicado a la dirección del salto")]
    [SerializeField] private float verticalOffset = -0.4f;
    
    [Tooltip("Longitud de la línea de visualización en editor")]
    [SerializeField] private float gizmoLineLength = 15f;
    
    [Header("Jump Power")]
    [Tooltip("Fuerza mínima de salto")]
    [SerializeField] private float minJumpForce = 5f;
    
    [Tooltip("Fuerza máxima de salto (al cargar completamente)")]
    [SerializeField] private float maxJumpForce = 25f;
    
    [Tooltip("Tiempo para cargar el salto al máximo (segundos)")]
    [SerializeField] private float chargeTime = 1.5f;
    
    [Header("Visual Settings (Editor Only)")]
    [SerializeField] private Color trajectoryColor = Color.green;
    [SerializeField] private Color chargedColor = Color.yellow;
    
    [Header("Collision")]
    [SerializeField] private LayerMask wallMask;
    [SerializeField] private LayerMask groundMask;
    
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    
    [Tooltip("Altura desde donde sale el salto (relativa al jugador)")]
    [SerializeField] private float startHeightOffset = 0.5f;
    
    // Estado
    private bool isActive = false;
    private Vector3 launchDirection;
    private Vector3 trajectoryStartPosition;
    private float currentChargeProgress = 0f;
    
    // Propiedades públicas
    public Vector3 LaunchDirection => launchDirection;
    public Vector3 StartPosition => trajectoryStartPosition;
    public float MinJumpForce => minJumpForce;
    public float MaxJumpForce => maxJumpForce;
    public float ChargeTime => chargeTime;
    public float VerticalOffset => verticalOffset;
    public bool IsActive => isActive;
    
    /// <summary>
    /// Obtiene la fuerza de salto según el progreso de carga (0-1)
    /// </summary>
    public float GetJumpForce(float chargeProgress)
    {
        return Mathf.Lerp(minJumpForce, maxJumpForce, chargeProgress);
    }
    
    /// <summary>
    /// Obtiene la velocidad inicial del salto (dirección * fuerza)
    /// </summary>
    public Vector3 GetLaunchVelocity(float chargeProgress)
    {
        CalculateDirection();
        return launchDirection * GetJumpForce(chargeProgress);
    }
    
    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>()?.transform;
            
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (player == null) return;
        
        // Siempre calcular la dirección para que esté disponible
        CalculateDirection();
    }
    
    /// <summary>
    /// Calcula la dirección del salto basada en la cámara
    /// </summary>
    void CalculateDirection()
    {
        if (player == null) return;
        
        // Posición inicial del salto
        trajectoryStartPosition = player.position + Vector3.up * startHeightOffset;
        
        // Obtener dirección desde la cámara
        Vector3 cameraPos = mainCamera != null ? mainCamera.transform.position : player.position + Vector3.up * 2f;
        Vector3 cameraForward = mainCamera != null ? mainCamera.transform.forward : player.forward;
        
        // Raycast desde la cámara para encontrar el "target"
        Vector3 targetPoint;
        if (Physics.Raycast(cameraPos, cameraForward, out RaycastHit hit, 100f, groundMask | wallMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = cameraPos + cameraForward * 50f;
        }
        
        // Calcular dirección desde el jugador hacia el target
        Vector3 direction = (targetPoint - trajectoryStartPosition).normalized;
        
        // Aplicar offset vertical (baja la dirección)
        direction = new Vector3(direction.x, direction.y + verticalOffset, direction.z).normalized;
        
        launchDirection = direction;
    }
    
    /// <summary>
    /// Actualiza el progreso de carga para visualización en Gizmos
    /// </summary>
    public void SetChargeProgress(float progress)
    {
        currentChargeProgress = Mathf.Clamp01(progress);
    }
    
    /// <summary>
    /// Activa/desactiva la visualización (solo afecta a Gizmos)
    /// </summary>
    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            currentChargeProgress = 0f;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Siempre dibujar cuando está seleccionado o activo
        if (player == null) return;
        
        CalculateDirection();
        
        // Color según carga
        Color lineColor = isActive ? Color.Lerp(trajectoryColor, chargedColor, currentChargeProgress) : trajectoryColor;
        Gizmos.color = lineColor;
        
        // Dibujar línea de dirección
        Vector3 endPoint = trajectoryStartPosition + launchDirection * gizmoLineLength;
        Gizmos.DrawLine(trajectoryStartPosition, endPoint);
        
        // Dibujar esfera en el origen
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(trajectoryStartPosition, 0.15f);
        
        // Dibujar flecha en la punta
        Gizmos.color = lineColor;
        Vector3 arrowSize = Vector3.one * 0.3f;
        Gizmos.DrawWireCube(endPoint, arrowSize);
        
        // Si está activo, dibujar indicador de fuerza
        if (isActive)
        {
            // Línea más gruesa según carga
            float chargedLength = gizmoLineLength * (0.3f + currentChargeProgress * 0.7f);
            Vector3 chargedEnd = trajectoryStartPosition + launchDirection * chargedLength;
            
            Gizmos.color = chargedColor;
            Gizmos.DrawWireSphere(chargedEnd, 0.1f + currentChargeProgress * 0.2f);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Dibujar siempre cuando está seleccionado
        OnDrawGizmos();
    }
#endif
}
