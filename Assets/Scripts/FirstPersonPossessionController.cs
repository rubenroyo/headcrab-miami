using UnityEngine;

/// <summary>
/// Controlador de primera persona para el modo posesión.
/// Maneja el mouse look (yaw/pitch) y proporciona direcciones de movimiento FPS.
/// Se activa/desactiva automáticamente cuando PlayerController entra/sale de posesión.
/// </summary>
public class FirstPersonPossessionController : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalSensitivity = 2f;
    
    [Header("Vertical Look Limits")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    
    [Header("Smoothing (optional)")]
    [SerializeField] private bool enableSmoothing = false;
    [SerializeField] private float smoothTime = 0.05f;
    
    // Estado interno
    private bool isActive = false;
    private Transform possessedTarget;    // El enemigo poseído (controla yaw)
    private Transform eyePoint;           // El punto de ojos (controla pitch)
    
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    
    // Smoothing
    private float yawVelocity = 0f;
    private float pitchVelocity = 0f;
    private float targetYaw = 0f;
    private float targetPitch = 0f;
    
    // Propiedades públicas
    public bool IsActive => isActive;
    public float CurrentPitch => currentPitch;
    public float CurrentYaw => currentYaw;
    public float MouseSensitivity { get => mouseSensitivity; set => mouseSensitivity = value; }
    public float VerticalSensitivity { get => verticalSensitivity; set => verticalSensitivity = value; }
    
    /// <summary>
    /// Activa el control de primera persona
    /// </summary>
    /// <param name="target">Transform del enemigo poseído (para yaw)</param>
    /// <param name="eye">Transform del punto de ojos (para pitch)</param>
    public void Activate(Transform target, Transform eye)
    {
        possessedTarget = target;
        eyePoint = eye;
        
        if (possessedTarget != null)
        {
            // Inicializar yaw desde la rotación actual del enemigo
            currentYaw = possessedTarget.eulerAngles.y;
            targetYaw = currentYaw;
        }
        
        // Iniciar pitch a 0 (mirando al frente)
        currentPitch = 0f;
        targetPitch = 0f;
        
        // Bloquear y ocultar cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        isActive = true;
        Debug.Log("[FirstPersonPossession] Activated");
    }
    
    /// <summary>
    /// Desactiva el control de primera persona
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        possessedTarget = null;
        eyePoint = null;
        
        // Restaurar cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        Debug.Log("[FirstPersonPossession] Deactivated");
    }
    
    void Update()
    {
        if (!isActive || possessedTarget == null) return;
        
        HandleMouseLook();
        ApplyRotations();
    }
    
    private void HandleMouseLook()
    {
        // Obtener input del ratón
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity;
        
        // Aplicar al target (con o sin smoothing)
        if (enableSmoothing)
        {
            targetYaw += mouseX;
            targetPitch -= mouseY; // Invertido: mover ratón arriba = mirar arriba
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
            
            currentYaw = Mathf.SmoothDamp(currentYaw, targetYaw, ref yawVelocity, smoothTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, smoothTime);
        }
        else
        {
            currentYaw += mouseX;
            currentPitch -= mouseY;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
            
            targetYaw = currentYaw;
            targetPitch = currentPitch;
        }
    }
    
    private void ApplyRotations()
    {
        // Aplicar yaw al enemigo (rotación horizontal completa del cuerpo)
        if (possessedTarget != null)
        {
            possessedTarget.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        }
        
        // Aplicar pitch al punto de ojos (rotación vertical solo de la "cabeza"/cámara)
        if (eyePoint != null)
        {
            eyePoint.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }
    }
    
    /// <summary>
    /// Obtiene el vector de movimiento en espacio del enemigo basado en el input WASD.
    /// Útil para PlayerController.HandlePossessedMovement()
    /// </summary>
    public Vector3 GetMovementDirection()
    {
        if (!isActive || possessedTarget == null) return Vector3.zero;
        
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        
        // Crear dirección relativa al forward del enemigo
        Vector3 forward = possessedTarget.forward;
        Vector3 right = possessedTarget.right;
        
        // Proyectar en el plano horizontal (ignorar componente Y)
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        // Combinar direcciones
        Vector3 moveDirection = (forward * v + right * h).normalized;
        
        return moveDirection;
    }
    
    /// <summary>
    /// Obtiene la dirección de disparo (hacia donde mira la cámara)
    /// </summary>
    public Vector3 GetAimDirection()
    {
        if (eyePoint != null)
        {
            return eyePoint.forward;
        }
        else if (possessedTarget != null)
        {
            return Quaternion.Euler(currentPitch, currentYaw, 0f) * Vector3.forward;
        }
        
        return Vector3.forward;
    }
    
    /// <summary>
    /// Fuerza una rotación específica (útil para sincronización)
    /// </summary>
    public void SetRotation(float yaw, float pitch)
    {
        currentYaw = yaw;
        targetYaw = yaw;
        currentPitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        targetPitch = currentPitch;
        
        ApplyRotations();
    }
}
