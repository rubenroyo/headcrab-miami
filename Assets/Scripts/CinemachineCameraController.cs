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
    
    [Header("Possession Settings (First Person)")]
    [Tooltip("Altura de los ojos relativa al pivot del enemigo (para primera persona)")]
    [SerializeField] private float eyeHeightOffset = 1.6f;
    
    [Tooltip("Sensibilidad del ratón en primera persona")]
    [SerializeField] private float fpMouseSensitivity = 2f;
    
    [Tooltip("Sensibilidad vertical del ratón")]
    [SerializeField] private float fpVerticalSensitivity = 2f;
    
    // Referencia al controlador de lag (se busca automáticamente en vcamJump)
    private JumpCameraLagController jumpLagController;
    
    // Controlador de primera persona para posesión
    private FirstPersonPossessionController fpController;
    
    // Vista del arma en primera persona
    private FPSWeaponView fpsWeaponView;
    
    // Transform temporal para los ojos del enemigo poseído
    private Transform possessionEyePoint;
    private Transform currentPossessedTarget;
    private InventoryHolder currentPossessedInventory;
    
    // Estado interno
    private CameraState currentState = CameraState.Normal;
    private float jumpProgress = 0f;
    private float jumpStartTime = 0f;
    private float jumpFOVStartValue = 0f;
    
    // Estado de carga del salto (transición suave)
    private bool isCharging = false;
    private float chargeFOVStart;
    private float chargeFOVTarget;
    private Vector3 chargeFollowOffsetStart;
    private Vector3 chargeFollowOffsetTarget;
    private float chargeOrbitRadiusStart;
    private float chargeOrbitRadiusTarget;
    
    // Componentes cacheados para la transición
    private CinemachineOrbitalFollow thirdPersonOrbital;
    private CinemachineOrbitalFollow aimOrbital;
    
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
    public bool IsPossessing => currentState == CameraState.Possessing;
    
    /// <summary>
    /// Referencia al controlador de primera persona (para que PlayerController pueda obtener direcciones de movimiento)
    /// </summary>
    public FirstPersonPossessionController FirstPersonController => fpController;
    
    /// <summary>
    /// Devuelve la cámara virtual de posesión (para modificar FOV desde PlayerController)
    /// </summary>
    public CinemachineCamera GetActivePossessionCamera()
    {
        return currentState == CameraState.Possessing ? vcamPossession : null;
    }
    
    /// <summary>
    /// Actualiza el estado de movimiento del arma FPS (para animación de bob)
    /// </summary>
    public void UpdateFPSWeaponMovement(bool isMoving, bool isSprinting)
    {
        if (fpsWeaponView != null)
        {
            fpsWeaponView.SetMovementState(isMoving, isSprinting);
        }
    }
    
    /// <summary>
    /// Actualiza el estado de apuntado ADS del arma FPS
    /// </summary>
    public void SetADSState(bool isAiming)
    {
        if (fpsWeaponView != null)
        {
            fpsWeaponView.SetADSState(isAiming);
        }
    }
    
    /// <summary>
    /// Dispara la animación de recoil del arma FPS
    /// </summary>
    public void TriggerWeaponRecoil()
    {
        if (fpsWeaponView != null)
        {
            fpsWeaponView.PlayRecoil();
        }
    }
    
    void Awake()
    {
        // Buscar impulse source si no está asignado
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
        
        // Asegurar que las cámaras tengan CinemachineImpulseListener para el screen shake
        EnsureImpulseListener(vcamThirdPerson);
        EnsureImpulseListener(vcamAim);
        EnsureImpulseListener(vcamJump);
        EnsureImpulseListener(vcamPossession);
        
        // Buscar controller de lag en vcamJump
        if (vcamJump != null)
            jumpLagController = vcamJump.GetComponent<JumpCameraLagController>();
        
        // Crear o buscar el controlador de primera persona
        fpController = GetComponent<FirstPersonPossessionController>();
        if (fpController == null)
            fpController = gameObject.AddComponent<FirstPersonPossessionController>();
        
        // Inicializar prioridades
        SetAllInactive();
        if (vcamThirdPerson != null)
        {
            vcamThirdPerson.Priority = activePriority;
            activeCamera = vcamThirdPerson;
        }
        
        // Cachear componentes OrbitalFollow para transiciones
        if (vcamThirdPerson != null)
            thirdPersonOrbital = vcamThirdPerson.GetComponent<CinemachineOrbitalFollow>();
        if (vcamAim != null)
            aimOrbital = vcamAim.GetComponent<CinemachineOrbitalFollow>();
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
    
    void OnDestroy()
    {
        // Limpiar el punto de ojos temporal
        CleanupPossessionEyePoint();
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
    /// Entrar en modo de carga de salto (transición progresiva ThirdPerson → Aim)
    /// </summary>
    public void EnterChargingMode()
    {
        Debug.Log("[CinemachineCamera] EnterChargingMode - starting smooth transition");
        isCharging = true;
        
        // Mantener ThirdPerson activa - vamos a interpolar sus valores
        SwitchToCamera(vcamThirdPerson);
        
        // Cachear valores iniciales de ThirdPerson
        if (vcamThirdPerson != null)
        {
            chargeFOVStart = vcamThirdPerson.Lens.FieldOfView;
            
            if (thirdPersonOrbital != null)
            {
                chargeFollowOffsetStart = thirdPersonOrbital.TargetOffset;
                chargeOrbitRadiusStart = thirdPersonOrbital.OrbitStyle == CinemachineOrbitalFollow.OrbitStyles.Sphere 
                    ? thirdPersonOrbital.Radius 
                    : 5f; // Default
            }
        }
        
        // Cachear valores target de Aim
        if (vcamAim != null)
        {
            chargeFOVTarget = vcamAim.Lens.FieldOfView;
            
            if (aimOrbital != null)
            {
                chargeFollowOffsetTarget = aimOrbital.TargetOffset;
                chargeOrbitRadiusTarget = aimOrbital.OrbitStyle == CinemachineOrbitalFollow.OrbitStyles.Sphere
                    ? aimOrbital.Radius
                    : 3f; // Default más cercano para aim
            }
        }
        else
        {
            // Fallback si no hay cámara Aim
            chargeFOVTarget = aimFOV;
            chargeFollowOffsetTarget = chargeFollowOffsetStart + new Vector3(0.5f, 0, -1f);
            chargeOrbitRadiusTarget = chargeOrbitRadiusStart * 0.6f;
        }
        
        Debug.Log($"[CinemachineCamera] Charge transition: FOV {chargeFOVStart} → {chargeFOVTarget}, Radius {chargeOrbitRadiusStart} → {chargeOrbitRadiusTarget}");
    }
    
    /// <summary>
    /// Salir del modo de carga de salto (cancelar)
    /// </summary>
    public void ExitChargingMode()
    {
        Debug.Log("[CinemachineCamera] ExitChargingMode - returning to ThirdPerson");
        isCharging = false;
        
        // Restaurar valores originales de ThirdPerson
        if (vcamThirdPerson != null)
        {
            vcamThirdPerson.Lens.FieldOfView = chargeFOVStart;
            
            if (thirdPersonOrbital != null)
            {
                thirdPersonOrbital.TargetOffset = chargeFollowOffsetStart;
                if (thirdPersonOrbital.OrbitStyle == CinemachineOrbitalFollow.OrbitStyles.Sphere)
                    thirdPersonOrbital.Radius = chargeOrbitRadiusStart;
            }
        }
        
        SwitchToCamera(vcamThirdPerson);
        currentState = CameraState.Normal;
    }
    
    /// <summary>
    /// Actualiza el progreso de carga del salto (0-1).
    /// La cámara transiciona gradualmente de ThirdPerson a Aim basándose en este valor.
    /// Interpola FOV y posición suavemente.
    /// </summary>
    public void SetChargeProgress(float progress)
    {
        if (!isCharging || vcamThirdPerson == null) return;
        
        progress = Mathf.Clamp01(progress);
        
        // Curva de easing para transición más natural
        float easedProgress = EaseOutQuad(progress);
        
        // Interpolar FOV
        float newFOV = Mathf.Lerp(chargeFOVStart, chargeFOVTarget, easedProgress);
        vcamThirdPerson.Lens.FieldOfView = newFOV;
        
        // Interpolar posición de la cámara (OrbitalFollow)
        if (thirdPersonOrbital != null)
        {
            // Interpolar TargetOffset (shoulder offset)
            thirdPersonOrbital.TargetOffset = Vector3.Lerp(chargeFollowOffsetStart, chargeFollowOffsetTarget, easedProgress);
            
            // Interpolar radio de órbita (distancia a jugador)
            if (thirdPersonOrbital.OrbitStyle == CinemachineOrbitalFollow.OrbitStyles.Sphere)
            {
                thirdPersonOrbital.Radius = Mathf.Lerp(chargeOrbitRadiusStart, chargeOrbitRadiusTarget, easedProgress);
            }
        }
        
        // Marcar como Aiming cuando está cargando
        if (progress > 0.1f)
            currentState = CameraState.Aiming;
        
        // Debug.Log($"[CinemachineCamera] SetChargeProgress({progress:F2})");
    }
    
    /// <summary>
    /// Entrar en modo salto
    /// </summary>
    public void EnterJumpMode()
    {
        // Terminar modo carga si estaba activo
        if (isCharging)
        {
            isCharging = false;
            
            // Restaurar valores originales de ThirdPerson para que la próxima vez empiece bien
            if (vcamThirdPerson != null)
            {
                vcamThirdPerson.Lens.FieldOfView = chargeFOVStart;
                
                if (thirdPersonOrbital != null)
                {
                    thirdPersonOrbital.TargetOffset = chargeFollowOffsetStart;
                    if (thirdPersonOrbital.OrbitStyle == CinemachineOrbitalFollow.OrbitStyles.Sphere)
                        thirdPersonOrbital.Radius = chargeOrbitRadiusStart;
                }
            }
        }
        
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
    /// Actualizar progreso del salto (0-1) para efectos de FOV y lag
    /// </summary>
    public void UpdateJumpProgress(float progress)
    {
        jumpProgress = Mathf.Clamp01(progress);
        
        // Comunicar progreso al controlador de lag
        if (jumpLagController != null)
            jumpLagController.UpdateJumpProgress(jumpProgress);
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
    /// Entrar en modo posesión - PRIMERA PERSONA instantánea a la altura de los ojos
    /// </summary>
    public void EnterPossessionMode(Transform enemyTarget)
    {
        currentState = CameraState.Possessing;
        currentPossessedTarget = enemyTarget;
        
        // Crear/actualizar el punto de ojos para primera persona
        CreateOrUpdateEyePoint(enemyTarget);
        
        // Configurar cámara de posesión para primera persona
        if (vcamPossession != null)
        {
            // El Follow es el punto de ojos (posición de la cámara)
            vcamPossession.Follow = possessionEyePoint;
            // LookAt null para primera persona pura (la orientación viene del Follow)
            vcamPossession.LookAt = null;
            
            // TRANSICIÓN INSTANTÁNEA: resetear el estado de la cámara
            vcamPossession.PreviousStateIsValid = false;
            
            // Desactivar cualquier CinemachineOrbitalFollow si existe
            var orbitalFollow = vcamPossession.GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow != null)
            {
                orbitalFollow.enabled = false;
            }
            
            // Asegurar que CinemachineFollow (si existe) esté configurado para primera persona
            var follow = vcamPossession.GetComponent<CinemachineFollow>();
            if (follow != null)
            {
                follow.FollowOffset = Vector3.zero; // Sin offset - cámara exactamente en los ojos
                var trackerSettings = follow.TrackerSettings;
                trackerSettings.PositionDamping = Vector3.zero; // Sin suavizado - instantáneo
                follow.TrackerSettings = trackerSettings;
            }
            
            // Asegurar que CinemachineRotateWithFollowTarget esté presente y activo
            // Este componente hace que la cámara rote igual que el eye point
            var rotateWithFollow = vcamPossession.GetComponent<CinemachineRotateWithFollowTarget>();
            if (rotateWithFollow == null)
            {
                rotateWithFollow = vcamPossession.gameObject.AddComponent<CinemachineRotateWithFollowTarget>();
            }
            rotateWithFollow.enabled = true;
            rotateWithFollow.Damping = 0f; // Sin suavizado para respuesta inmediata
        }
        
        // Activar control de primera persona (mouse look)
        if (fpController != null)
        {
            fpController.MouseSensitivity = fpMouseSensitivity;
            fpController.VerticalSensitivity = fpVerticalSensitivity;
            fpController.Activate(enemyTarget, possessionEyePoint);
        }
        
        // Activar vista de arma FPS
        ActivateFPSWeaponView(enemyTarget);
        
        SwitchToCameraInstant(vcamPossession);
        Debug.Log($"[CinemachineCamera] EnterPossessionMode - First Person at eye height {eyeHeightOffset}m");
    }
    
    /// <summary>
    /// Activa la vista del arma en primera persona
    /// </summary>
    private void ActivateFPSWeaponView(Transform enemyTarget)
    {
        // Obtener el inventario del enemigo
        currentPossessedInventory = enemyTarget.GetComponent<InventoryHolder>();
        
        if (currentPossessedInventory == null) return;
        
        // Ocultar el arma de tercera persona
        currentPossessedInventory.SetWeaponVisualVisible(false);
        
        // Crear o obtener el FPSWeaponView
        if (fpsWeaponView == null)
        {
            fpsWeaponView = gameObject.AddComponent<FPSWeaponView>();
        }
        
        // Activar con el inventario y el punto de ojos
        fpsWeaponView.Activate(currentPossessedInventory, possessionEyePoint);
    }
    
    /// <summary>
    /// Crea o actualiza el punto de ojos para la posesión
    /// </summary>
    private void CreateOrUpdateEyePoint(Transform enemyTarget)
    {
        // Buscar si el enemigo ya tiene un punto de ojos definido
        Transform existingEyePoint = enemyTarget.Find("EyePoint");
        
        if (existingEyePoint != null)
        {
            // Usar el punto de ojos existente
            possessionEyePoint = existingEyePoint;
        }
        else
        {
            // Crear un GameObject temporal como hijo del enemigo
            if (possessionEyePoint == null || possessionEyePoint.parent != enemyTarget)
            {
                // Limpiar el anterior si existe
                if (possessionEyePoint != null)
                    Destroy(possessionEyePoint.gameObject);
                
                GameObject eyePointObj = new GameObject("_PossessionEyePoint");
                eyePointObj.transform.SetParent(enemyTarget);
                possessionEyePoint = eyePointObj.transform;
            }
            
            // Posicionar a la altura de los ojos
            possessionEyePoint.localPosition = new Vector3(0f, eyeHeightOffset, 0f);
            possessionEyePoint.localRotation = Quaternion.identity;
        }
    }
    
    /// <summary>
    /// Salir del modo posesión (volver a seguir jugador)
    /// </summary>
    public void ExitPossessionMode(Transform playerTransform)
    {
        currentState = CameraState.Normal;
        
        // Desactivar control de primera persona
        if (fpController != null)
        {
            fpController.Deactivate();
        }
        
        // Restaurar targets al jugador
        SetAllTargets(playerTransform);
        
        // Re-habilitar orbital follow en vcamPossession si estaba deshabilitado
        if (vcamPossession != null)
        {
            var orbitalFollow = vcamPossession.GetComponent<CinemachineOrbitalFollow>();
            if (orbitalFollow != null)
            {
                orbitalFollow.enabled = true;
            }
            
            // Desactivar CinemachineRotateWithFollowTarget (ya no necesario fuera de posesión)
            var rotateWithFollow = vcamPossession.GetComponent<CinemachineRotateWithFollowTarget>();
            if (rotateWithFollow != null)
            {
                rotateWithFollow.enabled = false;
            }
        }
        
        // Desactivar vista de arma FPS y restaurar arma de tercera persona
        DeactivateFPSWeaponView();
        
        // Limpiar el punto de ojos temporal
        CleanupPossessionEyePoint();
        currentPossessedTarget = null;
        
        SwitchToCamera(vcamThirdPerson);
        Debug.Log("[CinemachineCamera] ExitPossessionMode");
    }
    
    /// <summary>
    /// Desactiva la vista del arma en primera persona
    /// </summary>
    private void DeactivateFPSWeaponView()
    {
        // Desactivar vista FPS
        if (fpsWeaponView != null)
        {
            fpsWeaponView.Deactivate();
        }
        
        // Restaurar arma de tercera persona
        if (currentPossessedInventory != null)
        {
            currentPossessedInventory.SetWeaponVisualVisible(true);
            currentPossessedInventory = null;
        }
    }
    
    /// <summary>
    /// Limpia el punto de ojos temporal de la posesión
    /// </summary>
    private void CleanupPossessionEyePoint()
    {
        if (possessionEyePoint != null && possessionEyePoint.name == "_PossessionEyePoint")
        {
            Destroy(possessionEyePoint.gameObject);
            possessionEyePoint = null;
        }
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
    
    /// <summary>
    /// Cambiar a una cámara con transición INSTANTÁNEA (sin blend)
    /// </summary>
    private void SwitchToCameraInstant(CinemachineCamera newCamera)
    {
        if (newCamera == null) return;
        
        SetAllInactive();
        newCamera.Priority = activePriority;
        activeCamera = newCamera;
        
        // Forzar actualización instantánea sin blend
        newCamera.PreviousStateIsValid = false;
        
        // Buscar el CinemachineBrain y forzar corte instantáneo
        var brain = CinemachineCore.FindPotentialTargetBrain(newCamera);
        if (brain != null)
        {
            // Forzar actualización inmediata del brain
            brain.ManualUpdate();
        }
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
    
    /// <summary>
    /// Asegura que una cámara tenga CinemachineImpulseListener para recibir screen shake
    /// </summary>
    private void EnsureImpulseListener(CinemachineCamera vcam)
    {
        if (vcam == null) return;
        
        var listener = vcam.GetComponent<CinemachineImpulseListener>();
        if (listener == null)
        {
            listener = vcam.gameObject.AddComponent<CinemachineImpulseListener>();
            listener.Gain = 1f;
            Debug.Log($"[CinemachineCamera] Added ImpulseListener to {vcam.name}");
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
    
    /// <summary>
    /// Curva de easing para transiciones más naturales (desacelera al final)
    /// </summary>
    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
    
    /// <summary>
    /// Curva de easing que acelera al principio y desacelera al final
    /// </summary>
    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
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
