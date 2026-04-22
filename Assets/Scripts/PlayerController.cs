using UnityEngine;

public enum PlayerState
{
    Normal,
    ChargingJump,  // Cargando salto (clic derecho mantenido)
    Jumping,       // En el aire con física
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
    
    [Header("Salto con Carga")]
    [Tooltip("El tiempo de carga viene del JumpTrajectoryVisualizer")]
    [SerializeField] private float jumpChargeTime = 1.5f;  // Se sincroniza con JumpTrajectoryVisualizer
    
    [Tooltip("Fuerza mínima de salto")]
    [SerializeField] private float minJumpForce = 5f;
    
    [Tooltip("Fuerza máxima de salto (al cargar completamente)")]
    [SerializeField] private float maxJumpForce = 25f;
    
    [Tooltip("Gravedad para el salto físico")]
    [SerializeField] private float jumpGravity = 30f;
    
    // Estado de carga
    private float chargeTimer = 0f;
    private float chargeProgress = 0f;  // 0-1
    
    // Estado de salto físico
    private Vector3 jumpVelocity;
    private bool isJumping = false;

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

    // Posesión
    private EnemyController possessedEnemy;
    private float lastDismountTime = -999f;
    private bool isSprinting = false;
    private bool isAimingADS = false;  // Apuntando con clic derecho durante posesión

    // Referencia al SettingsController
    private SettingsController settingsController;

    // Test-friendly public accessors
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float ChargeProgress => chargeProgress;  // Nuevo: expone el progreso de carga
    public EnemyController PossessedEnemy => possessedEnemy;

    /// <summary>
    /// Inicia un salto con una dirección y fuerza especificadas (útil para tests y dismount).
    /// </summary>
    public void StartJumpWithVelocity(Vector3 velocity)
    {
        jumpVelocity = velocity;
        isJumping = true;
        CurrentState = PlayerState.Jumping;
        
        Debug.Log($"[Player] StartJumpWithVelocity: {velocity}");
    }

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
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

            case PlayerState.ChargingJump:
                UpdateChargingJump();
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

        // Clic derecho = empezar a cargar salto
        if (Input.GetMouseButtonDown(1))
            EnterChargingJump();

        // El jugador sin poseer no puede disparar (es una seta)
    }

    void UpdateChargingJump()
    {
        // El personaje se DETIENE mientras carga (no HandleMovement)
        
        // Actualizar carga
        chargeTimer += Time.deltaTime;
        chargeProgress = Mathf.Clamp01(chargeTimer / jumpChargeTime);
        
        // Actualizar visualizador con el progreso de carga
        if (jumpTrajectoryVisualizer != null)
        {
            jumpTrajectoryVisualizer.SetChargeProgress(chargeProgress);
        }
        
        // Sincronizar cámara con el progreso de carga (transición ThirdPerson → Aim)
        if (cinemachineCamera != null)
        {
            cinemachineCamera.SetChargeProgress(chargeProgress);
        }
        
        // Al soltar clic derecho = ejecutar salto
        if (Input.GetMouseButtonUp(1))
        {
            ExecuteJump();
        }
    }

    void UpdateJump()
    {
        if (!isJumping)
        {
            EndJump();
            return;
        }
        
        // Aplicar gravedad a la velocidad
        jumpVelocity.y -= jumpGravity * Time.deltaTime;
        
        // Mover con CharacterController
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(jumpVelocity * Time.deltaTime);
        }
        else
        {
            // Fallback si CharacterController está deshabilitado
            transform.position += jumpVelocity * Time.deltaTime;
        }
        
        // Rotar hacia la dirección de movimiento horizontal
        Vector3 horizontalVel = new Vector3(jumpVelocity.x, 0, jumpVelocity.z);
        if (horizontalVel.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(horizontalVel.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }
        
        // Verificar colisión con enemigos para posesión
        CheckPossessionCollision();
        
        // Verificar aterrizaje (tocando suelo)
        if (characterController != null && characterController.isGrounded && jumpVelocity.y <= 0)
        {
            EndJump();
        }
        // Fallback: raycast hacia abajo si no usamos CharacterController en este frame
        else if (characterController == null || !characterController.enabled)
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.3f))
            {
                if (jumpVelocity.y <= 0)
                    EndJump();
            }
        }
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

    void EnterChargingJump()
    {
        Debug.Log("[Player] EnterChargingJump()");
        CurrentState = PlayerState.ChargingJump;
        
        // Resetear carga
        chargeTimer = 0f;
        chargeProgress = 0f;
        
        // Activar slow motion mientras carga
        Time.timeScale = aimTimeScale;
        Time.fixedDeltaTime = 0.02f * aimTimeScale;
        
        // Iniciar transición de cámara (empezará desde ThirdPerson)
        if (cinemachineCamera != null)
        {
            cinemachineCamera.EnterChargingMode();
        }
        
        // Activar visualizador de trayectoria (solo visible en editor)
        if (jumpTrajectoryVisualizer != null)
        {
            jumpTrajectoryVisualizer.SetActive(true);
            // Sincronizar tiempo de carga con el visualizador
            jumpChargeTime = jumpTrajectoryVisualizer.ChargeTime;
            minJumpForce = jumpTrajectoryVisualizer.MinJumpForce;
            maxJumpForce = jumpTrajectoryVisualizer.MaxJumpForce;
        }
    }
    
    void CancelChargingJump()
    {
        Debug.Log("[Player] CancelChargingJump()");
        CurrentState = PlayerState.Normal;
        
        // Restaurar tiempo normal
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        // Resetear carga
        chargeTimer = 0f;
        chargeProgress = 0f;
        
        // Cancelar transición de cámara
        if (cinemachineCamera != null)
        {
            cinemachineCamera.ExitChargingMode();
        }
        
        // Desactivar visualizador
        if (jumpTrajectoryVisualizer != null)
            jumpTrajectoryVisualizer.SetActive(false);
    }

    void ExecuteJump()
    {
        Debug.Log($"[Player] ExecuteJump() - ChargeProgress: {chargeProgress:F2}");
        
        // Obtener velocidad de lanzamiento desde el visualizador
        if (jumpTrajectoryVisualizer != null)
        {
            jumpVelocity = jumpTrajectoryVisualizer.GetLaunchVelocity(chargeProgress);
            Debug.Log($"[Jump] Launch velocity: {jumpVelocity}, magnitude: {jumpVelocity.magnitude:F2}");
        }
        else
        {
            // Fallback si no hay visualizador
            Vector3 cameraForward = Camera.main.transform.forward;
            float force = Mathf.Lerp(minJumpForce, maxJumpForce, chargeProgress);
            jumpVelocity = (cameraForward + Vector3.up * 0.5f).normalized * force;
            Debug.LogWarning("[Jump] Using fallback velocity - no visualizer");
        }
        
        // Restaurar tiempo normal para el salto
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        // Cambiar estado
        CurrentState = PlayerState.Jumping;
        isJumping = true;
        
        // Activar modo salto en la cámara
        if (cinemachineCamera != null)
            cinemachineCamera.EnterJumpMode();
        else if (thirdPersonCamera != null)
            thirdPersonCamera.EnterJumpMode();
        
        // Desactivar visualizador de trayectoria
        if (jumpTrajectoryVisualizer != null)
            jumpTrajectoryVisualizer.SetActive(false);
        
        // Rotar jugador hacia la dirección horizontal del salto
        Vector3 horizontalDir = jumpVelocity;
        horizontalDir.y = 0f;
        if (horizontalDir.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(horizontalDir.normalized);
        }
    }

    void EndJump()
    {
        Debug.Log("[Player] EndJump()");
        
        // Restaurar tiempo normal (por si acaso)
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        // Reset estado de salto
        isJumping = false;
        jumpVelocity = Vector3.zero;
        
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
            if (cameraFollow != null)
                cameraFollow.SetJumping(false, 0f);

            // Si el clic derecho sigue pulsado, reactivar modo de carga
            if (Input.GetMouseButton(1))
            {
                EnterChargingJump();
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
        
        // Restaurar tiempo normal (por si estábamos cargando salto)
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        // Detener salto si estábamos saltando
        isJumping = false;
        jumpVelocity = Vector3.zero;
        
        // Reactivar CharacterController
        if (characterController != null)
            characterController.enabled = true;
        
        // Forzar estado normal temporalmente
        CurrentState = PlayerState.Normal;

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
            if (CurrentState == PlayerState.ChargingJump)
            {
                // Durante carga de salto: siempre mirar hacia donde mira la cámara
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
        
        // Calcular velocidad de salida para el desmonte (salto pequeño hacia abajo)
        Vector3 currentPos = transform.position;
        Vector3 targetPos = currentPos + Vector3.down * 2f; // Apuntar hacia abajo
        
        // Buscar punto de suelo
        if (Physics.Raycast(currentPos, Vector3.down, out RaycastHit hit, 10f))
        {
            targetPos = hit.point;
        }
        
        // Crear velocidad inicial pequeña (el jugador cae principalmente por gravedad)
        Vector3 direction = (targetPos - currentPos).normalized;
        float dismountForce = 5f;
        jumpVelocity = direction * dismountForce + Vector3.up * 2f; // Pequeño impulso hacia arriba
        
        // Entrar en estado de salto con física
        isJumping = true;
        CurrentState = PlayerState.Jumping;

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);
            cameraFollow.SetPossessedMode(false);
            cameraFollow.SetJumping(true, 0.5f);
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