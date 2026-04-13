using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Controlador de cámaras Cinemachine para diferentes estados del jugador.
/// Maneja transiciones entre Normal, Aim, Jump y Possession cambiando prioridades.
/// Replica la interfaz de ThirdPersonOrbitCamera para compatibilidad con PlayerController.
/// </summary>
public class CinemachineCameraController : MonoBehaviour
{
    [Header("Virtual Cameras")]
    [Tooltip("Cámara principal de tercera persona")]
    [SerializeField] private CinemachineCamera vcamThirdPerson;
    
    [Tooltip("Cámara para modo apuntar (shoulder cam)")]
    [SerializeField] private CinemachineCamera vcamAim;
    
    [Tooltip("Cámara durante el salto")]
    [SerializeField] private CinemachineCamera vcamJump;
    
    [Tooltip("Cámara siguiendo al enemigo poseído")]
    [SerializeField] private CinemachineCamera vcamPossession;
    
    [Header("Priority Settings")]
    [Tooltip("Prioridad de la cámara activa")]
    [SerializeField] private int activePriority = 20;
    
    [Tooltip("Prioridad de las cámaras inactivas")]
    [SerializeField] private int inactivePriority = 10;
    
    [Header("Target")]
    [SerializeField] private Transform playerTarget;
    
    [Header("FOV Settings")]
    [SerializeField] private float normalFOV = 90f;
    [SerializeField] private float aimFOV = 60f;
    [SerializeField] private float jumpFOV = 110f;
    
    [Header("Jump Camera Timing")]
    [Tooltip("Duración del cambio de FOV después de iniciar el salto")]
    [SerializeField] private float jumpFOVDuration = 0.3f;
    
    [Tooltip("Duración de la aceleración de la cámara")]
    [SerializeField] private float jumpCameraAccelDuration = 0.2f;
    
    [Header("Screen Shake - Impulse Source")]
    [Tooltip("Impulse Source para landing shake (añadir CinemachineImpulseSource al mismo objeto)")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    
    [SerializeField] private float landingShakeIntensity = 0.3f;
    
    // Estado interno
    private CameraState currentState = CameraState.Normal;
    private float jumpProgress = 0f;
    private float jumpStartTime = 0f;
    private float jumpFOVStartValue = 0f;
    
    // Cache
    private CinemachineCamera activeCamera;
    
    public enum CameraState
    {
        Normal,
        Aiming,
        Jumping,
        Possessing
    }
    
    // Propiedades públicas
    public CameraState CurrentState => currentState;
    public bool IsAiming => currentState == CameraState.Aiming;
    public bool IsJumping => currentState == CameraState.Jumping;
    
    void Awake()
    {
        // Buscar impulse source si no está asignado
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
        
        // Inicializar prioridades
        SetAllInactive();
        if (vcamThirdPerson != null)
        {
            vcamThirdPerson.Priority = activePriority;
            activeCamera = vcamThirdPerson;
        }
    }
    
    void Start()
    {
        // Auto-find target si no está asignado
        if (playerTarget == null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                playerTarget = player.transform;
        }
        
        // Configurar targets en todas las cámaras
        SetAllTargets(playerTarget);
    }
    
    void Update()
    {
        // Sistema de FOV dinámico durante el salto
        if (currentState == CameraState.Jumping && vcamJump != null)
        {
            float timeSinceJumpStart = Time.unscaledTime - jumpStartTime;
            float fovT = jumpFOVDuration > 0f ? Mathf.Clamp01(timeSinceJumpStart / jumpFOVDuration) : 1f;
            
            // Ease-out para que empiece rápido y termine suave
            float smoothFovT = 1f - (1f - fovT) * (1f - fovT);
            
            // FOV target con boost sinusoidal basado en el progreso del salto
            float jumpTargetFOV = jumpFOV + Mathf.Sin(jumpProgress * Mathf.PI) * 10f;
            
            float newFOV = Mathf.Lerp(jumpFOVStartValue, jumpTargetFOV, smoothFovT);
            vcamJump.Lens.FieldOfView = newFOV;
        }
    }
    
    #region Public API (Compatible con ThirdPersonOrbitCamera)
    
    /// <summary>
    /// Entrar en modo apuntar
    /// </summary>
    public void EnterAimMode()
    {
        if (currentState == CameraState.Aiming) return;
        
        currentState = CameraState.Aiming;
        SwitchToCamera(vcamAim);
        Debug.Log("[CinemachineCamera] EnterAimMode");
    }
    
    /// <summary>
    /// Salir del modo apuntar
    /// </summary>
    public void ExitAimMode()
    {
        if (currentState != CameraState.Aiming) return;
        
        currentState = CameraState.Normal;
        SwitchToCamera(vcamThirdPerson);
        Debug.Log("[CinemachineCamera] ExitAimMode");
    }
    
    /// <summary>
    /// Entrar en modo salto
    /// </summary>
    public void EnterJumpMode()
    {
        currentState = CameraState.Jumping;
        jumpStartTime = Time.unscaledTime;
        jumpProgress = 0f;
        
        // Guardar FOV actual para interpolar desde ahí
        if (activeCamera != null)
            jumpFOVStartValue = activeCamera.Lens.FieldOfView;
        else
            jumpFOVStartValue = normalFOV;
        
        SwitchToCamera(vcamJump);
        Debug.Log("[CinemachineCamera] EnterJumpMode");
    }
    
    /// <summary>
    /// Salir del modo salto
    /// </summary>
    public void ExitJumpMode()
    {
        if (currentState != CameraState.Jumping) return;
        
        currentState = CameraState.Normal;
        SwitchToCamera(vcamThirdPerson);
        Debug.Log("[CinemachineCamera] ExitJumpMode");
    }
    
    /// <summary>
    /// Actualizar progreso del salto (0-1) para efectos de FOV
    /// </summary>
    public void UpdateJumpProgress(float progress)
    {
        jumpProgress = Mathf.Clamp01(progress);
    }
    
    /// <summary>
    /// Disparar screen shake al aterrizar
    /// </summary>
    public void TriggerLandingShake()
    {
        TriggerShake(landingShakeIntensity);
    }
    
    /// <summary>
    /// Disparar screen shake con intensidad personalizada
    /// </summary>
    public void TriggerShake(float intensity)
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(Vector3.down * intensity);
            Debug.Log($"[CinemachineCamera] TriggerShake intensity={intensity}");
        }
    }
    
    /// <summary>
    /// Entrar en modo posesión (seguir enemigo)
    /// </summary>
    public void EnterPossessionMode(Transform enemyTarget)
    {
        currentState = CameraState.Possessing;
        
        // Configurar cámara de posesión para seguir al enemigo
        if (vcamPossession != null)
        {
            vcamPossession.Follow = enemyTarget;
            vcamPossession.LookAt = enemyTarget;
        }
        
        SwitchToCamera(vcamPossession);
        Debug.Log("[CinemachineCamera] EnterPossessionMode");
    }
    
    /// <summary>
    /// Salir del modo posesión (volver a seguir jugador)
    /// </summary>
    public void ExitPossessionMode(Transform playerTransform)
    {
        currentState = CameraState.Normal;
        
        // Restaurar targets al jugador
        SetAllTargets(playerTransform);
        
        SwitchToCamera(vcamThirdPerson);
        Debug.Log("[CinemachineCamera] ExitPossessionMode");
    }
    
    /// <summary>
    /// Cambiar el target de todas las cámaras
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        playerTarget = newTarget;
        SetAllTargets(newTarget);
    }
    
    /// <summary>
    /// Snap inmediato al nuevo target (útil para cambios de posesión)
    /// </summary>
    public void SnapToTarget(Transform newTarget, float newYaw)
    {
        SetTarget(newTarget);
        
        // Forzar posición inmediata en todas las cámaras
        foreach (var vcam in new[] { vcamThirdPerson, vcamAim, vcamJump, vcamPossession })
        {
            if (vcam != null)
            {
                // Resetear el estado de la cámara para snap inmediato
                vcam.PreviousStateIsValid = false;
            }
        }
    }
    
    #endregion
    
    #region Private Helpers
    
    private void SwitchToCamera(CinemachineCamera newCamera)
    {
        if (newCamera == null) return;
        
        // Sincronizar solo el eje horizontal (rotación alrededor del personaje)
        SyncHorizontalAxis(activeCamera, newCamera);
        
        SetAllInactive();
        newCamera.Priority = activePriority;
        activeCamera = newCamera;
    }
    
    private void SyncHorizontalAxis(CinemachineCamera fromCamera, CinemachineCamera toCamera)
    {
        if (fromCamera == null || toCamera == null) return;
        
        var fromOrbital = fromCamera.GetComponent<CinemachineOrbitalFollow>();
        var toOrbital = toCamera.GetComponent<CinemachineOrbitalFollow>();
        
        if (fromOrbital != null && toOrbital != null)
        {
            // Solo copiar el eje horizontal (yaw - rotación alrededor del personaje)
            // El eje vertical (pitch) lo maneja Cinemachine con el blend
            toOrbital.HorizontalAxis.Value = fromOrbital.HorizontalAxis.Value;
        }
    }
    
    private void SetAllInactive()
    {
        if (vcamThirdPerson != null) vcamThirdPerson.Priority = inactivePriority;
        if (vcamAim != null) vcamAim.Priority = inactivePriority;
        if (vcamJump != null) vcamJump.Priority = inactivePriority;
        if (vcamPossession != null) vcamPossession.Priority = inactivePriority;
    }
    
    private void SetAllTargets(Transform target)
    {
        if (target == null) return;
        
        // Configurar Follow y LookAt en todas las cámaras (excepto possession que tiene su propio target)
        if (vcamThirdPerson != null)
        {
            vcamThirdPerson.Follow = target;
            vcamThirdPerson.LookAt = target;
        }
        if (vcamAim != null)
        {
            vcamAim.Follow = target;
            vcamAim.LookAt = target;
        }
        if (vcamJump != null)
        {
            vcamJump.Follow = target;
            vcamJump.LookAt = target;
        }
    }
    
    #endregion
    
    #region Editor Helpers
    
#if UNITY_EDITOR
    [ContextMenu("Setup Default Cameras")]
    private void SetupDefaultCameras()
    {
        Debug.Log("Use el menú GameObject > Cinemachine > Cinemachine Camera para crear las cámaras virtuales.");
    }
#endif
    
    #endregion
}
