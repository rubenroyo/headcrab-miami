using UnityEngine;

/// <summary>
/// Cámara de primera persona estándar.
/// Se adjunta a un objeto hijo del jugador que actúa como "cabeza".
/// </summary>
public class FirstPersonCamera : MonoBehaviour
{
    [Header("Sensibilidad")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float lookSmoothTime = 0.03f;
    
    [Header("Límites verticales")]
    [SerializeField] private float minPitch = -89f;
    [SerializeField] private float maxPitch = 89f;
    
    [Header("Referencias")]
    [Tooltip("El transform del jugador que rotará en el eje Y (horizontal)")]
    [SerializeField] private Transform playerBody;
    
    // Rotación actual
    private float pitch = 0f; // Rotación vertical (arriba/abajo)
    private float yaw = 0f;   // Rotación horizontal (izquierda/derecha)
    
    // Suavizado
    private float pitchVelocity;
    private float yawVelocity;
    private float targetPitch;
    private float targetYaw;
    
    // Cache
    private bool cursorLocked = true;

    void Start()
    {
        // Bloquear cursor en el centro de la pantalla
        LockCursor(true);
        
        // Inicializar rotación con la actual del jugador
        if (playerBody != null)
        {
            yaw = playerBody.eulerAngles.y;
            targetYaw = yaw;
        }
        
        pitch = transform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f; // Convertir de 0-360 a -180-180
        targetPitch = pitch;
    }

    void Update()
    {
        HandleCursorLock();
        
        if (!cursorLocked)
            return;
            
        HandleMouseLook();
    }
    
    void HandleCursorLock()
    {
        // Escape para liberar/bloquear cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(!cursorLocked);
        }
        
        // Click para volver a bloquear
        if (!cursorLocked && Input.GetMouseButtonDown(0))
        {
            LockCursor(true);
        }
    }
    
    void HandleMouseLook()
    {
        // Obtener input del ratón
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        // Acumular rotación objetivo
        targetYaw += mouseX;
        targetPitch -= mouseY; // Invertido: mover ratón arriba = mirar arriba
        
        // Clamp pitch para no girar completamente
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        
        // Suavizar rotación (opcional, para sensación más suave)
        if (lookSmoothTime > 0f)
        {
            yaw = Mathf.SmoothDamp(yaw, targetYaw, ref yawVelocity, lookSmoothTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, lookSmoothTime);
        }
        else
        {
            yaw = targetYaw;
            pitch = targetPitch;
        }
        
        // Aplicar rotación vertical a la cámara (pitch)
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        
        // Aplicar rotación horizontal al cuerpo del jugador (yaw)
        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
    
    /// <summary>
    /// Bloquea o libera el cursor
    /// </summary>
    public void LockCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
    
    /// <summary>
    /// Establece la sensibilidad del ratón
    /// </summary>
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    /// <summary>
    /// Obtiene la dirección hacia donde mira la cámara (para disparar)
    /// </summary>
    public Vector3 GetLookDirection()
    {
        return transform.forward;
    }
    
    /// <summary>
    /// Establece un nuevo body de jugador (para posesión)
    /// </summary>
    public void SetPlayerBody(Transform newBody)
    {
        playerBody = newBody;
        if (newBody != null)
        {
            yaw = newBody.eulerAngles.y;
            targetYaw = yaw;
        }
    }
    
    /// <summary>
    /// Teletransporta la cámara a un nuevo objetivo manteniendo el pitch actual
    /// </summary>
    public void SnapToTarget(Transform target, float newYaw)
    {
        if (target != null)
        {
            transform.position = target.position;
            yaw = newYaw;
            targetYaw = newYaw;
        }
    }
}
