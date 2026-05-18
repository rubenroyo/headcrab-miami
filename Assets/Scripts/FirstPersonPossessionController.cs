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
    [SerializeField] private bool enableSmoothing = true;
    [Tooltip("Cu\u00e1nto tarda la c\u00e1mara en alcanzar la velocidad del rat\u00f3n (segundos). "
           + "M\u00e1s alto = m\u00e1s inercia al arrancar. 0 = instant\u00e1neo.")]
    [SerializeField] private float accelerationTime = 0.04f;
    [Tooltip("Cu\u00e1nto tarda la c\u00e1mara en detenerse cuando el rat\u00f3n para (segundos). "
           + "M\u00e1s alto = m\u00e1s deriva al soltar. 0 = parada instant\u00e1nea.")]
    [SerializeField] private float decelerationTime = 0.08f;
    
    [Header("Camera Tilt (Sprint + Strafe)")]
    [Tooltip("Ángulo máximo de inclinación lateral de la cámara (grados)")]
    [SerializeField] private float tiltMaxAngle = 5f;
    [Tooltip("Velocidad de interpolación de la inclinación (mayor = más rápido)")]
    [SerializeField] private float tiltSpeed = 5f;
    [Tooltip("Solo inclinar cuando el jugador está corriendo (Shift)")]
    [SerializeField] private bool tiltOnlyWhileSprinting = true;
    
    // Estado interno
    private bool isActive = false;
    private Transform possessedTarget;    // El enemigo poseído (controla yaw)
    private Transform eyePoint;           // El punto de ojos (controla pitch)
    
    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private float currentRoll = 0f;
    
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
        UpdateTilt();
        ApplyRotations();
    }
    
    private void UpdateTilt()
    {
        float h = Input.GetAxisRaw("Horizontal");
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        bool shouldTilt = !tiltOnlyWhileSprinting || isSprinting;
        float targetRoll = shouldTilt ? -h * tiltMaxAngle : 0f;
        
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, tiltSpeed * Time.unscaledDeltaTime);
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

            // Aceleración cuando el ratón se mueve, deceleración cuando para
            float yawSmooth   = Mathf.Abs(mouseX) > 0.001f ? accelerationTime : decelerationTime;
            float pitchSmooth = Mathf.Abs(mouseY) > 0.001f ? accelerationTime : decelerationTime;

            currentYaw   = Mathf.SmoothDamp(currentYaw,   targetYaw,   ref yawVelocity,   yawSmooth,   Mathf.Infinity, Time.unscaledDeltaTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, pitchSmooth, Mathf.Infinity, Time.unscaledDeltaTime);
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
        
        // Aplicar pitch y roll al punto de ojos (rotación vertical + inclinación lateral de la cámara)
        if (eyePoint != null)
        {
            eyePoint.localRotation = Quaternion.Euler(currentPitch, 0f, currentRoll);
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
        currentRoll = 0f;
        
        ApplyRotations();
    }
}
