using UnityEngine;

public enum PlayerState
{
    Normal,
    Aiming,
    Jumping,
    Possessing
}

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // CharacterController para colisiones
    private CharacterController characterController;
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = 20f;
    
    // Velocidad vertical acumulada
    private float verticalVelocity = 0f;

    [Header("Referencias")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private FirstPersonCamera firstPersonCamera;
    [SerializeField] private ThirdPersonOrbitCamera thirdPersonCamera;
    [SerializeField] private CinemachineCameraController cinemachineCamera;
    [SerializeField] private JumpTrajectoryVisualizer jumpTrajectoryVisualizer;
    
    [Header("Rotación del jugador")]
    [Tooltip("Velocidad de rotación del jugador hacia la dirección de movimiento")]
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Salto Cinemático")]
    [Tooltip("Duración del 'wind-up' al inicio del salto (slow motion dramático)")]
    [SerializeField] private float jumpWindUpDuration = 0.08f;
    
    [Tooltip("Escala de tiempo durante el wind-up")]
    [SerializeField] private float jumpWindUpTimeScale = 0.1f;
    
    [Tooltip("Escala de tiempo durante el vuelo (1 = normal, >1 = más rápido)")]
    [SerializeField] private float jumpFlightTimeScale = 1.2f;
    
    [Tooltip("Curva de velocidad del salto (X = progreso 0-1, Y = multiplicador de velocidad)")]
    [SerializeField] private AnimationCurve jumpSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    // Duración del salto actual (viene del JumpTrajectoryVisualizer)
    private float currentJumpDuration;

    [Header("Posesión")]
    [SerializeField] private float possessionCooldown = 0.5f;
    [SerializeField] private float possessedEnemyMoveSpeed = 4f;
    
    [Header("Sprint (durante posesión)")]
    [SerializeField] private float sprintSpeedMultiplier = 1.6f;
    [SerializeField] private float sprintFOV = 100f;
    [SerializeField] private float normalPossessionFOV = 90f;
    [SerializeField] private float fovTransitionSpeed = 8f;
    
    [Header("Apuntado ADS (durante posesión)")]
    [SerializeField] private float aimFOV = 70f;
    [SerializeField] private float aimSpeedMultiplier = 0.5f;
    
    [Header("Slow Motion (Aim)")]
    [Tooltip("Escala de tiempo durante el apuntado (0.5 = mitad de velocidad)")]
    [SerializeField] private float aimTimeScale = 0.3f;
    
    [Tooltip("Velocidad de transición del tiempo")]
    [SerializeField] private float timeTransitionSpeed = 10f;

    public PlayerState CurrentState { get; private set; } = PlayerState.Normal;

    // Salto físico
    private float jumpElapsedTime;
    private Vector3[] jumpTrajectoryPoints;
    private Vector3 jumpStartPosition;
    private bool isInWindUp = false;
    private float windUpTimer = 0f;

    // Posesión
    private EnemyController possessedEnemy;
    private float lastDismountTime = -999f;
    private bool isSprinting = false;
    private bool isAimingADS = false;  // Apuntando con clic derecho durante posesión

    // Referencia al SettingsController
    private SettingsController settingsController;

    // Test-friendly public accessors
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float CurrentJumpDuration => currentJumpDuration;
    public EnemyController PossessedEnemy => possessedEnemy;

    /// <summary>
    /// Inicia un salto con puntos de trayectoria personalizados (útil para tests y dismount).
    /// </summary>
    public void StartJumpWithPoints(Vector3[] points, float durationOverride = -1f)
    {
        jumpTrajectoryPoints = points;

        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length < 2)
            return;

        // Calcular duración basada en la distancia de la trayectoria
        if (durationOverride > 0f)
        {
            currentJumpDuration = durationOverride;
        }
        else
        {
            // Calcular distancia total y estimar duración (similar a física de parábola)
            float totalDistance = 0f;
            for (int i = 0; i < jumpTrajectoryPoints.Length - 1; i++)
                totalDistance += Vector3.Distance(jumpTrajectoryPoints[i], jumpTrajectoryPoints[i + 1]);
            
            // Duración proporcional: ~0.5s para trayectorias cortas, ~1s para largas
            currentJumpDuration = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(totalDistance / 15f));
        }

        CurrentState = PlayerState.Jumping;
        jumpElapsedTime = 0f;
        
        // Cachear distancias para interpolación
        CacheTrajectoryDistances();

        cameraFollow?.SetJumping(true, currentJumpDuration);
    }

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        // Forzar la curva a empezar en 0 (por si el valor serializado está mal)
        jumpSpeedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    void Start()
    {
        // Obtener SettingsController
        settingsController = SettingsController.Instance;

        // Usar la cámara del SettingsController si está disponible
        if (settingsController != null && settingsController.MainCamera != null)
            mainCamera = settingsController.MainCamera;
        else if (mainCamera == null)
            mainCamera = Camera.main;

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();
        
        if (thirdPersonCamera == null)
            thirdPersonCamera = FindFirstObjectByType<ThirdPersonOrbitCamera>();
        
        if (cinemachineCamera == null)
            cinemachineCamera = FindFirstObjectByType<CinemachineCameraController>();
        
        if (jumpTrajectoryVisualizer == null)
            jumpTrajectoryVisualizer = FindFirstObjectByType<JumpTrajectoryVisualizer>();
        
        // Bloquear y ocultar cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Toggle cursor con Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isLocked;
        }
        
        // Re-bloquear cursor al hacer clic
        if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        switch (CurrentState)
        {
            case PlayerState.Normal:
                UpdateNormal();
                break;

            case PlayerState.Aiming:
                UpdateAiming();
                break;

            case PlayerState.Jumping:
                UpdateJump();
                break;

            case PlayerState.Possessing:
                UpdatePossessing();
                break;
        }
    }

    // ---------------- STATES ----------------

    void UpdateNormal()
    {
        HandleMovement();
        // En FPS, la rotación la maneja FirstPersonCamera

        if (Input.GetMouseButtonDown(1))
            EnterAiming();

        // El jugador sin poseer no puede disparar (es una seta)
    }

    void UpdateAiming()
    {
        HandleMovement();
        // En FPS, la rotación la maneja FirstPersonCamera

        // Clic izquierdo mientras apuntas = saltar
        if (Input.GetMouseButtonDown(0))
            StartJump();

        if (Input.GetMouseButtonUp(1))
            ExitAiming();
    }

    void UpdateJump()
    {
        // Verificar que tenemos puntos de trayectoria
        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length < 2)
        {
            Debug.LogWarning("[Jump] No trajectory points!");
            EndJump();
            return;
        }
        
        // Verificar estado de los arrays
        if (trajectoryDistances == null)
        {
            Debug.LogError("[Jump] trajectoryDistances is NULL!");
            EndJump();
            return;
        }
        
        // Manejar wind-up (slow motion inicial)
        if (isInWindUp)
        {
            windUpTimer += Time.unscaledDeltaTime;
            
            if (windUpTimer >= jumpWindUpDuration)
            {
                // Terminar wind-up, acelerar al vuelo
                isInWindUp = false;
                Time.timeScale = jumpFlightTimeScale;
                Time.fixedDeltaTime = 0.02f * jumpFlightTimeScale;
            }
            else
            {
                // Todavía en wind-up, no mover
                return;
            }
        }
        
        // Actualizar tiempo del salto
        jumpElapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(jumpElapsedTime / currentJumpDuration);
        
        // Aplicar curva de velocidad para sensación más satisfactoria
        float curvedProgress = jumpSpeedCurve.Evaluate(progress);
        
        // Seguir los puntos de la trayectoria (interpolación por distancia)
        Vector3 position = GetPositionAlongTrajectoryByDistance(curvedProgress);
        
        transform.position = position;
        
        // Rotar hacia la dirección de movimiento (suave)
        Vector3 moveDir = GetTrajectoryDirectionAt(curvedProgress);
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);
        }
        
        // Actualizar progreso en la cámara
        if (cinemachineCamera != null)
            cinemachineCamera.UpdateJumpProgress(progress);
        else if (thirdPersonCamera != null)
            thirdPersonCamera.UpdateJumpProgress(progress);
        
        // Verificar colisión con enemigos para posesión
        CheckPossessionCollision();
        
        // Verificar si hemos llegado al final
        if (progress >= 1f && CurrentState == PlayerState.Jumping)
        {
            EndJump();
        }
    }
    
    // Distancia total de la trayectoria (cacheada)
    private float trajectoryTotalDistance = 0f;
    private float[] trajectoryDistances = null;
    
    /// <summary>
    /// Calcula las distancias acumuladas de cada punto de la trayectoria
    /// </summary>
    void CacheTrajectoryDistances()
    {
        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length < 2)
        {
            trajectoryTotalDistance = 0f;
            trajectoryDistances = null;
            return;
        }
        
        trajectoryDistances = new float[jumpTrajectoryPoints.Length];
        trajectoryDistances[0] = 0f;
        
        for (int i = 1; i < jumpTrajectoryPoints.Length; i++)
        {
            trajectoryDistances[i] = trajectoryDistances[i - 1] + 
                Vector3.Distance(jumpTrajectoryPoints[i - 1], jumpTrajectoryPoints[i]);
        }
        
        trajectoryTotalDistance = trajectoryDistances[jumpTrajectoryPoints.Length - 1];
    }
    
    /// <summary>
    /// Obtiene la posición en la trayectoria basándose en distancia recorrida (no índice)
    /// </summary>
    Vector3 GetPositionAlongTrajectoryByDistance(float t)
    {
        if (trajectoryDistances == null || trajectoryTotalDistance <= 0f)
            return transform.position;
        
        float targetDistance = t * trajectoryTotalDistance;
        
        // Buscar el segmento donde estamos
        for (int i = 0; i < trajectoryDistances.Length - 1; i++)
        {
            if (targetDistance <= trajectoryDistances[i + 1])
            {
                float segmentStart = trajectoryDistances[i];
                float segmentEnd = trajectoryDistances[i + 1];
                float segmentLength = segmentEnd - segmentStart;
                
                if (segmentLength < 0.001f)
                    return jumpTrajectoryPoints[i];
                
                float localT = (targetDistance - segmentStart) / segmentLength;
                return Vector3.Lerp(jumpTrajectoryPoints[i], jumpTrajectoryPoints[i + 1], localT);
            }
        }
        
        // Al final de la trayectoria
        return jumpTrajectoryPoints[jumpTrajectoryPoints.Length - 1];
    }
    
    /// <summary>
    /// Obtiene la dirección de movimiento en un punto de la trayectoria
    /// </summary>
    Vector3 GetTrajectoryDirectionAt(float t)
    {
        if (trajectoryDistances == null || trajectoryTotalDistance <= 0f || jumpTrajectoryPoints.Length < 2)
            return transform.forward;
        
        float targetDistance = t * trajectoryTotalDistance;
        
        // Buscar el segmento donde estamos
        for (int i = 0; i < trajectoryDistances.Length - 1; i++)
        {
            if (targetDistance <= trajectoryDistances[i + 1])
            {
                return (jumpTrajectoryPoints[i + 1] - jumpTrajectoryPoints[i]).normalized;
            }
        }
        
        // Al final
        int last = jumpTrajectoryPoints.Length - 1;
        return (jumpTrajectoryPoints[last] - jumpTrajectoryPoints[last - 1]).normalized;
    }

    void UpdatePossessing()
    {
        if (possessedEnemy == null)
        {
            CurrentState = PlayerState.Normal;
            return;
        }

        // Sprint con Shift (no puedes sprintar si apuntas)
        isSprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !isAimingADS;
        
        // Apuntar con clic derecho (no puedes apuntar si corres)
        bool wantsToAim = Input.GetMouseButton(1);
        isAimingADS = wantsToAim && !isSprinting;
        
        // Actualizar estado de apuntado en el arma FPS
        if (cinemachineCamera != null)
        {
            cinemachineCamera.SetADSState(isAimingADS);
        }
        
        // Actualizar estado del crosshair
        bool isMovingNow = cinemachineCamera != null && 
                          cinemachineCamera.FirstPersonController != null &&
                          cinemachineCamera.FirstPersonController.GetMovementDirection().sqrMagnitude > 0.01f;
        if (CrosshairController.Instance != null)
        {
            CrosshairController.Instance.UpdateState(isAimingADS, isMovingNow, isSprinting);
        }

        // Disparar con click izquierdo (ahora también mientras apuntas)
        if (Input.GetMouseButtonDown(0))
        {
            TryFirePossessedWeapon();
        }
        
        // DESACTIVADO TEMPORALMENTE: Desmontar con espacio + click derecho
        // if (Input.GetKeyDown(KeyCode.Space) && Input.GetMouseButton(1))
        // {
        //     StartDismount();
        // }
        
        // Actualizar FOV de la cámara según sprint/apuntado
        UpdateSprintFOV();

        HandlePossessedMovement();

        Vector3 enemyPos = possessedEnemy.transform.position;
        transform.position = new Vector3(enemyPos.x, 1.625f, enemyPos.z);

        // En FPS, la rotación la maneja FirstPersonPossessionController
    }
    
    /// <summary>
    /// Actualiza el FOV de la cámara de posesión según sprint/apuntado
    /// </summary>
    private void UpdateSprintFOV()
    {
        if (cinemachineCamera == null) return;
        
        // Acceder directamente a vcamPossession para cambiar FOV
        var vcam = cinemachineCamera.GetActivePossessionCamera();
        if (vcam != null)
        {
            float targetFOV;
            if (isAimingADS)
                targetFOV = aimFOV;
            else if (isSprinting)
                targetFOV = sprintFOV;
            else
                targetFOV = normalPossessionFOV;
                
            vcam.Lens.FieldOfView = Mathf.Lerp(vcam.Lens.FieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
        }
    }

    // ---------------- ACTIONS ----------------

    void EnterAiming()
    {
        Debug.Log("[Player] EnterAiming()");
        CurrentState = PlayerState.Aiming;
        
        // Activar slow motion
        Time.timeScale = aimTimeScale;
        Time.fixedDeltaTime = 0.02f * aimTimeScale;
        
        // Activar modo aim en la cámara
        if (cinemachineCamera != null)
            cinemachineCamera.EnterAimMode();
        else if (thirdPersonCamera != null)
            thirdPersonCamera.EnterAimMode();
        
        // Activar visualizador de trayectoria
        if (jumpTrajectoryVisualizer != null)
        {
            Debug.Log("[Player] Activating JumpTrajectoryVisualizer");
            jumpTrajectoryVisualizer.SetActive(true);
        }
        else
        {
            Debug.LogError("[Player] jumpTrajectoryVisualizer is NULL! Make sure to add JumpTrajectoryVisualizer component to a GameObject in the scene.");
        }
    }
    
    void ExitAiming()
    {
        Debug.Log("[Player] ExitAiming()");
        CurrentState = PlayerState.Normal;
        
        // Restaurar tiempo normal
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        // Desactivar modo aim en la cámara
        if (cinemachineCamera != null)
            cinemachineCamera.ExitAimMode();
        else if (thirdPersonCamera != null)
            thirdPersonCamera.ExitAimMode();
        
        // Desactivar visualizador de trayectoria
        if (jumpTrajectoryVisualizer != null)
            jumpTrajectoryVisualizer.SetActive(false);
    }

    void StartJump()
    {
        // Verificar que tenemos un punto de aterrizaje válido
        if (jumpTrajectoryVisualizer == null || !jumpTrajectoryVisualizer.HasValidLanding)
        {
            Debug.Log("[Jump] No valid landing point - cannot jump");
            return;
        }
        
        // Copiar los puntos de trayectoria
        Vector3[] sourcePoints = jumpTrajectoryVisualizer.TrajectoryPoints;
        if (sourcePoints == null || sourcePoints.Length < 2)
        {
            Debug.Log("[Jump] Not enough trajectory points");
            return;
        }
        
        jumpTrajectoryPoints = new Vector3[sourcePoints.Length];
        System.Array.Copy(sourcePoints, jumpTrajectoryPoints, sourcePoints.Length);
        
        // Guardar posición de inicio
        jumpStartPosition = jumpTrajectoryVisualizer.StartPosition;
        
        // La duración viene directamente del visualizador (basada en la física de la parábola)
        currentJumpDuration = jumpTrajectoryVisualizer.FlightDuration;
        
        // Cachear distancias para interpolación suave
        CacheTrajectoryDistances();
        
        Debug.Log($"[JUMP] Start: {jumpStartPosition}, End: {jumpTrajectoryPoints[jumpTrajectoryPoints.Length - 1]}, Duration: {currentJumpDuration:F2}s");
        
        // Cambiar estado
        CurrentState = PlayerState.Jumping;
        jumpElapsedTime = 0f;
        
        // Deshabilitar CharacterController durante el salto
        if (characterController != null)
            characterController.enabled = false;
        
        // Iniciar wind-up (slow motion dramático al despegar)
        isInWindUp = true;
        windUpTimer = 0f;
        Time.timeScale = jumpWindUpTimeScale;
        Time.fixedDeltaTime = 0.02f * jumpWindUpTimeScale;
        
        // Desactivar visualizador de trayectoria
        if (jumpTrajectoryVisualizer != null)
            jumpTrajectoryVisualizer.SetActive(false);
        
        // Activar modo salto en la cámara
        if (cinemachineCamera != null)
            cinemachineCamera.EnterJumpMode();
        else if (thirdPersonCamera != null)
            thirdPersonCamera.EnterJumpMode();
        
        // Rotar jugador hacia la dirección inicial
        if (jumpTrajectoryPoints.Length >= 2)
        {
            Vector3 jumpDir = (jumpTrajectoryPoints[1] - jumpTrajectoryPoints[0]);
            jumpDir.y = 0f;
            if (jumpDir.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(jumpDir.normalized);
            }
        }
    }

    void EndJump()
    {
        // Restaurar tiempo normal
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isInWindUp = false;
        
        // Reactivar CharacterController
        if (characterController != null)
            characterController.enabled = true;
        
        // Salir del modo salto de la cámara y disparar shake
        if (cinemachineCamera != null)
        {
            cinemachineCamera.ExitJumpMode();
            cinemachineCamera.TriggerLandingShake();
        }
        else if (thirdPersonCamera != null)
        {
            thirdPersonCamera.ExitJumpMode();
            thirdPersonCamera.TriggerLandingShake();
        }
        
        if (CurrentState != PlayerState.Possessing)
        {
            // Ajustar posición final al último punto de la trayectoria
            if (jumpTrajectoryPoints != null && jumpTrajectoryPoints.Length > 0)
            {
                transform.position = jumpTrajectoryPoints[jumpTrajectoryPoints.Length - 1];
            }

            if (cameraFollow != null)
                cameraFollow.SetJumping(false, 0f);

            // Si el clic derecho sigue pulsado, reactivar modo apuntado
            if (Input.GetMouseButton(1))
            {
                EnterAiming();
            }
            else
            {
                CurrentState = PlayerState.Normal;
            }
        }
    }

    // ---------------- POSSESSION ----------------

    void CheckPossessionCollision()
    {
        if (Time.time - lastDismountTime < possessionCooldown)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, 1f);
        foreach (Collider col in hits)
        {
            EnemyController enemy = col.GetComponent<EnemyController>();
            if (enemy != null && enemy.CanBePossessed && !enemy.IsPossessed)
            {
                PossessEnemy(enemy);
                break;
            }
        }
    }

    void PossessEnemy(EnemyController enemy)
    {
        // Guardar si tenemos que reactivar el aim después
        bool wasHoldingAim = Input.GetMouseButton(1);
        
        // Restaurar tiempo normal (por si estábamos en wind-up o vuelo)
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        isInWindUp = false;
        
        // Reactivar CharacterController
        if (characterController != null)
            characterController.enabled = true;
        
        // Forzar estado normal temporalmente para que EndJump no reactive aim
        CurrentState = PlayerState.Normal;
        
        if (jumpTrajectoryPoints != null && jumpTrajectoryPoints.Length > 0)
        {
            Vector3 finalPos = jumpTrajectoryPoints[^1];
            transform.position = new Vector3(finalPos.x, 0f, finalPos.z);
        }

        if (cameraFollow != null)
            cameraFollow.SetJumping(false, 0f);

        // Ahora entrar en modo posesión
        CurrentState = PlayerState.Possessing;

        possessedEnemy = enemy;
        possessedEnemy.OnPossessed();

        transform.position = new Vector3(enemy.transform.position.x, 1.625f, enemy.transform.position.z);

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(enemy.transform);
            cameraFollow.SetPossessedMode(true);
            
            // Reactivar slow-motion si seguía pulsando clic derecho
            if (wasHoldingAim)
                cameraFollow.ForceAimMode(true);
        }
        
        // Ocultar modelo del jugador para no interferir con vista FPS
        SetModelVisible(false);
        
        // Actualizar target de Cinemachine
        if (cinemachineCamera != null)
            cinemachineCamera.EnterPossessionMode(enemy.transform);
        
        // Mostrar crosshair
        if (CrosshairController.Instance != null)
            CrosshairController.Instance.SetVisible(true);
    }

    /// <summary>
    /// Libera al enemigo poseído y restaura el estado normal (sin salto).
    /// </summary>
    public void ReleaseEnemy()
    {
        ReleasePossessedEnemy();
        RestoreCameraToPlayer();
        
        // Mostrar modelo del jugador de nuevo
        SetModelVisible(true);
        
        // Ocultar crosshair
        if (CrosshairController.Instance != null)
            CrosshairController.Instance.SetVisible(false);
        
        CurrentState = PlayerState.Normal;
    }

    private void ReleasePossessedEnemy()
    {
        if (possessedEnemy != null)
        {
            possessedEnemy.OnReleased();
            possessedEnemy = null;
        }
        
        // Resetear estado de sprint
        isSprinting = false;
    }

    private void RestoreCameraToPlayer()
    {
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);
            cameraFollow.SetPossessedMode(false);
        }
        
        // Restaurar target de Cinemachine
        if (cinemachineCamera != null)
            cinemachineCamera.ExitPossessionMode(transform);
    }

    // ---------------- HELPERS ----------------

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Aplicar gravedad
        if (characterController.isGrounded)
        {
            // Pequeña fuerza hacia abajo para mantenerlo pegado al suelo
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // Obtener dirección de la cámara (proyectada en el plano horizontal)
        Vector3 forward;
        Vector3 right;
        
        // Usar la cámara principal (funciona tanto con Cinemachine como con el sistema antiguo)
        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam != null)
        {
            // Tercera persona: movimiento relativo a la cámara
            forward = cam.transform.forward;
            right = cam.transform.right;
        }
        else
        {
            // Fallback: movimiento relativo al jugador
            forward = transform.forward;
            right = transform.right;
        }
        
        // Mantener movimiento horizontal (sin componente Y)
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        Vector3 inputDirection = (forward * v + right * h);
        
        // Rotar el jugador (en tercera persona)
        bool isThirdPerson = cinemachineCamera != null || thirdPersonCamera != null;
        if (isThirdPerson)
        {
            if (CurrentState == PlayerState.Aiming)
            {
                // Durante aim: siempre mirar hacia donde mira la cámara
                // Usar unscaledDeltaTime para que sea fluido en slow motion
                Quaternion targetRotation = Quaternion.LookRotation(forward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.unscaledDeltaTime);
            }
            else if (inputDirection.sqrMagnitude > 0.01f)
            {
                // Normal: rotar hacia la dirección de movimiento
                Quaternion targetRotation = Quaternion.LookRotation(inputDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        
        Vector3 move = inputDirection.normalized * moveSpeed;
        move.y = verticalVelocity;
        
        characterController.Move(move * Time.deltaTime);
    }

    void HandlePossessedMovement()
    {
        if (possessedEnemy == null) return;

        Vector3 moveDirection;
        
        // Si tenemos el controlador FPS activo, usar direcciones relativas al enemigo
        if (cinemachineCamera != null && 
            cinemachineCamera.FirstPersonController != null && 
            cinemachineCamera.FirstPersonController.IsActive)
        {
            moveDirection = cinemachineCamera.FirstPersonController.GetMovementDirection();
        }
        else
        {
            // Fallback: movimiento en ejes mundiales (para compatibilidad)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            moveDirection = new Vector3(h, 0f, v).normalized;
        }
        
        // Aplicar multiplicador de sprint o apuntado
        float currentSpeed = possessedEnemyMoveSpeed;
        if (isAimingADS)
        {
            currentSpeed *= aimSpeedMultiplier;
        }
        else if (isSprinting)
        {
            currentSpeed *= sprintSpeedMultiplier;
        }
        
        // Actualizar animación del arma FPS (bob)
        bool isMoving = moveDirection.sqrMagnitude > 0.01f;
        if (cinemachineCamera != null)
        {
            cinemachineCamera.UpdateFPSWeaponMovement(isMoving, isSprinting);
        }
        
        possessedEnemy.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Intenta disparar con el arma del enemigo poseído
    /// </summary>
    void TryFirePossessedWeapon()
    {
        if (possessedEnemy == null) return;

        InventoryHolder inventory = possessedEnemy.GetComponent<InventoryHolder>();
        if (inventory == null || !inventory.HasWeapon) return;

        Vector3 direction;
        Vector3 origin = Vector3.zero;
        
        // Si tenemos el controlador FPS activo, usar la dirección de la cámara
        if (cinemachineCamera != null && 
            cinemachineCamera.FirstPersonController != null && 
            cinemachineCamera.FirstPersonController.IsActive)
        {
            // Obtener la cámara principal para hacer raycast
            Camera cam = Camera.main;
            if (cam != null)
            {
                // Raycast desde el centro de la pantalla para encontrar el punto de mira
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                
                // Buscar punto de impacto o usar punto lejano
                Vector3 aimPoint;
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 1000f))
                {
                    aimPoint = hit.point;
                }
                else
                {
                    aimPoint = ray.GetPoint(100f);
                }
                
                // Calcular dirección desde el arma hacia el punto de mira
                origin = inventory.GetMuzzlePosition();
                direction = (aimPoint - origin).normalized;
                
                // Aplicar dispersión
                direction = ApplyDispersion(direction);
            }
            else
            {
                // Fallback: usar dirección del eyePoint
                direction = cinemachineCamera.FirstPersonController.GetAimDirection();
                direction = ApplyDispersion(direction);
            }
        }
        else
        {
            // Fallback: disparar hacia el mouse (para compatibilidad top-down)
            Vector3 mouseWorld = settingsController.GetMouseWorldPosition(mainCamera, Input.mousePosition);
            direction = mouseWorld - possessedEnemy.transform.position;
            direction.y = 0f;
            direction.Normalize();
        }

        bool fired = inventory.TryFire(direction);
        
        // Si disparó, aplicar efectos de recoil y camera shake
        if (fired && cinemachineCamera != null)
        {
            // Animación de recoil del arma FPS
            cinemachineCamera.TriggerWeaponRecoil();
            
            // Camera shake usando intensidad del WeaponType
            float shakeIntensity = inventory.EquippedWeapon.weaponType.fireShakeIntensity;
            if (shakeIntensity > 0f)
            {
                cinemachineCamera.TriggerShake(shakeIntensity);
            }
        }
    }
    
    /// <summary>
    /// Aplica dispersión aleatoria a una dirección de disparo
    /// </summary>
    private Vector3 ApplyDispersion(Vector3 direction)
    {
        if (CrosshairController.Instance == null) return direction;
        
        float dispersionAngle = CrosshairController.Instance.CurrentDispersion;
        if (dispersionAngle <= 0f) return direction;
        
        // Generar desviación aleatoria dentro de un cono
        float randomAngleX = Random.Range(-dispersionAngle, dispersionAngle);
        float randomAngleY = Random.Range(-dispersionAngle, dispersionAngle);
        
        // Aplicar rotación a la dirección
        Quaternion dispersionRotation = Quaternion.Euler(randomAngleX, randomAngleY, 0f);
        return dispersionRotation * direction;
    }

    void RotateTowardsMouse()
    {
        if (mainCamera == null || settingsController == null) return;

        Vector3 mouseWorld = settingsController.GetMouseWorldPosition(mainCamera, Input.mousePosition);
        Vector3 dir = mouseWorld - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.forward = dir.normalized;

            if (CurrentState == PlayerState.Possessing && possessedEnemy != null)
            {
                Vector3 enemyDir = mouseWorld - possessedEnemy.transform.position;
                enemyDir.y = 0f;
                if (enemyDir.sqrMagnitude > 0.001f)
                    possessedEnemy.transform.forward = enemyDir.normalized;
            }
        }
    }

    Vector3 GetPositionAlongTrajectory(float distance)
    {
        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length == 0)
            return transform.position;

        float current = 0f;

        for (int i = 0; i < jumpTrajectoryPoints.Length - 1; i++)
        {
            float segLen = Vector3.Distance(jumpTrajectoryPoints[i], jumpTrajectoryPoints[i + 1]);
            if (current + segLen >= distance)
                return Vector3.Lerp(jumpTrajectoryPoints[i], jumpTrajectoryPoints[i + 1],
                                    (distance - current) / segLen);
            current += segLen;
        }

        return jumpTrajectoryPoints[^1];
    }

    void StartDismount()
    {
        if (possessedEnemy == null) return;

        lastDismountTime = Time.time;
        ReleasePossessedEnemy();
        
        // Mostrar el modelo del jugador de nuevo
        SetModelVisible(true);
        
        // Restaurar cámara a tercera persona
        if (cinemachineCamera != null)
            cinemachineCamera.ExitPossessionMode(transform);
        
        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length == 0)
        {
            Vector3 currentPos = transform.position;
            Ray groundRay = new Ray(currentPos, Vector3.down);
            Vector3 groundPoint;
            
            if (Physics.Raycast(groundRay, out RaycastHit hit))
                groundPoint = new Vector3(hit.point.x, 0f, hit.point.z);
            else
                groundPoint = new Vector3(currentPos.x, 0f, currentPos.z);
            
            jumpTrajectoryPoints = new Vector3[] { currentPos, groundPoint };
        }
        else
        {
            Vector3 lastPoint = jumpTrajectoryPoints[^1];
            jumpTrajectoryPoints[^1] = new Vector3(lastPoint.x, 0f, lastPoint.z);
        }

        // Calcular duración basada en la distancia
        float totalDistance = 0f;
        for (int i = 0; i < jumpTrajectoryPoints.Length - 1; i++)
            totalDistance += Vector3.Distance(jumpTrajectoryPoints[i], jumpTrajectoryPoints[i + 1]);
        currentJumpDuration = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(totalDistance / 15f));
        
        // Cachear distancias
        CacheTrajectoryDistances();

        jumpElapsedTime = 0f;
        CurrentState = PlayerState.Jumping;

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);
            cameraFollow.SetPossessedMode(false);
            cameraFollow.SetJumping(true, currentJumpDuration);
        }
    }
    
    /// <summary>
    /// Muestra u oculta todos los renderers del modelo del jugador (para primera persona)
    /// </summary>
    private void SetModelVisible(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }
}