using UnityEngine;

public enum PlayerState
{
    Normal,
    Crouching,       // Agachado en el suelo (deslizando hasta parar)
    Jumping,         // Salto normal en el aire
    Backflip,        // Salto hacia atrás desde agachado sin dirección
    LongJump,        // Salto largo desde agachado con dirección
    AirRoll,         // Voltereta en el aire: 1s suspendido, decide dive o ground pound
    Dive,            // Arco bajo hacia donde se mueve el personaje
    GroundPound,     // Caída en picado
    GroundPoundLand, // Stun al aterrizar del ground pound
    Possessing
}

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController characterController;

    // ─────────────────────────────────────────────
    //  MOVIMIENTO
    // ─────────────────────────────────────────────

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 20f;
    [SerializeField] private float directionChangeSpeed = 25f;

    private Vector3 currentVelocity = Vector3.zero;
    private float verticalVelocity = 0f;

    // ─────────────────────────────────────────────
    //  GRAVEDAD
    // ─────────────────────────────────────────────

    [Header("Gravedad")]
    [SerializeField] private float riseGravity = 25f;
    [SerializeField] private float fallGravity = 40f;
    [SerializeField] private float maxFallSpeed = 40f;

    // ─────────────────────────────────────────────
    //  SALTO NORMAL
    // ─────────────────────────────────────────────

    [Header("Salto Normal")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float jumpCutMultiplier = 3f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.15f;

    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;
    private bool isJumpCut = false;
    private bool wasGroundedLastFrame = false;

    // ─────────────────────────────────────────────
    //  AGACHARSE
    // ─────────────────────────────────────────────

    [Header("Agacharse")]
    [SerializeField] private float crouchSlideDeceleration = 8f;
    [SerializeField] private float longJumpMinInputThreshold = 0.1f;

    private bool hasCrouchDirectionInput = false;

    // ─────────────────────────────────────────────
    //  BACKFLIP
    // ─────────────────────────────────────────────

    [Header("Backflip")]
    [SerializeField] private float backflipVerticalForce = 16f;
    [SerializeField] private float backflipHorizontalForce = 5f;

    // ─────────────────────────────────────────────
    //  LONG JUMP
    // ─────────────────────────────────────────────

    [Header("Long Jump")]
    [SerializeField] private float longJumpVerticalForce = 8f;
    [SerializeField] private float longJumpHorizontalForce = 16f;
    [SerializeField] private float longJumpGravity = 15f;

    private Vector3 longJumpDirection = Vector3.zero;

    // ─────────────────────────────────────────────
    //  AIR ROLL
    // ─────────────────────────────────────────────

    [Header("Air Roll")]
    [Tooltip("Segundos que el personaje queda suspendido decidiendo")]
    [SerializeField] private float airRollDuration = 1f;

    private float airRollTimer = 0f;
    private Vector3 airRollDiveDirection = Vector3.zero;

    // ─────────────────────────────────────────────
    //  DIVE
    // ─────────────────────────────────────────────

    [Header("Dive")]
    [SerializeField] private float diveHorizontalForce = 18f;
    [Tooltip("Pequeño impulso vertical al inicio del dive antes de caer")]
    [SerializeField] private float diveVerticalForce = 4f;
    [Tooltip("Gravedad agresiva para mantener arco muy bajo")]
    [SerializeField] private float diveGravity = 50f;

    // ─────────────────────────────────────────────
    //  GROUND POUND
    // ─────────────────────────────────────────────

    [Header("Ground Pound")]
    [SerializeField] private float groundPoundSpeed = 35f;
    [SerializeField] private float groundPoundLandDuration = 0.5f;

    private float groundPoundLandTimer = 0f;

    // ─────────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────────

    [Header("Referencias")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private ThirdPersonOrbitCamera thirdPersonCamera;
    [SerializeField] private CinemachineCameraController cinemachineCamera;

    [Header("Rotación")]
    [SerializeField] private float rotationSpeedGround = 20f;
    [SerializeField] private float rotationSpeedAir = 8f;

    // ─────────────────────────────────────────────
    //  POSESIÓN
    // ─────────────────────────────────────────────

    [Header("Posesión")]
    [SerializeField] private float possessionCooldown = 0.5f;
    [SerializeField] private float possessionRange = 3f;

    [Header("Sprint (posesión)")]
    [SerializeField] private float sprintFOV = 100f;
    [SerializeField] private float normalPossessionFOV = 90f;
    [SerializeField] private float fovTransitionSpeed = 8f;

    [Header("ADS (posesión)")]
    [SerializeField] private float aimFOV = 70f;

    private EnemyCombatActions possessedCombatActions;

    // ─────────────────────────────────────────────
    //  ESTADO GENERAL
    // ─────────────────────────────────────────────

    public PlayerState CurrentState { get; private set; } = PlayerState.Normal;

    private EnemyController possessedEnemy;
    private float lastDismountTime = -999f;
    private bool isSprinting = false;
    private bool isAimingADS = false;

    private SettingsController settingsController;

    public float MoveSpeed           { get => moveSpeed; set => moveSpeed = value; }
    public EnemyController PossessedEnemy => possessedEnemy;
    public float CurrentHorizontalSpeed   => new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
    public bool  IsGrounded               => characterController.isGrounded;
    public bool  IsCrouching              => CurrentState == PlayerState.Crouching;
    public float VerticalVelocity         => verticalVelocity;
    public float AirRollDuration          => airRollDuration;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Start()
    {
        settingsController = SettingsController.Instance;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isLocked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = isLocked;
        }
        if (Cursor.lockState == CursorLockMode.None && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if ((CurrentState == PlayerState.Normal || CurrentState == PlayerState.Crouching)
            && (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)))
        {
            EnterCrouch();
        }
        if (CurrentState == PlayerState.Crouching && (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift)))
        {
            ExitCrouch();
        }

        switch (CurrentState)
        {
            case PlayerState.Normal:          UpdateNormal();          break;
            case PlayerState.Crouching:       UpdateCrouching();       break;
            case PlayerState.Jumping:         UpdateJumping();         break;
            case PlayerState.Backflip:        UpdateBackflip();        break;
            case PlayerState.LongJump:        UpdateLongJump();        break;
            case PlayerState.AirRoll:         UpdateAirRoll();         break;
            case PlayerState.Dive:            UpdateDive();            break;
            case PlayerState.GroundPound:     UpdateGroundPound();     break;
            case PlayerState.GroundPoundLand: UpdateGroundPoundLand(); break;
            case PlayerState.Possessing:      UpdatePossessing();      break;
        }
    }

    // ─────────────────────────────────────────────
    //  STATES — SUELO
    // ─────────────────────────────────────────────

    void UpdateNormal()
    {
        HandleMovementAndGravity();
        UpdateCoyoteAndBuffer();
        HandleJumpInput();

        if (Input.GetKeyDown(KeyCode.E))
            TryPossessNearbyEnemy();
    }

    void UpdateCrouching()
    {
        ApplyGravity();
        ApplyCrouchSlide();

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        hasCrouchDirectionInput = new Vector2(h, v).sqrMagnitude > longJumpMinInputThreshold * longJumpMinInputThreshold;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (hasCrouchDirectionInput)
                PerformLongJump();
            else
                PerformBackflip();
        }
    }

    // ─────────────────────────────────────────────
    //  STATES — AIRE ESTÁNDAR
    // ─────────────────────────────────────────────

    void UpdateJumping()
    {
        HandleMovementAndGravity();
        UpdateCoyoteAndBuffer();
        HandleJumpInput();

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            EnterAirRoll();

        if (characterController.isGrounded && verticalVelocity <= 0f)
            LandOnGround();
    }

    void UpdateBackflip()
    {
        ApplyGravityInAir();
        ApplyAirControl(0.4f);
        RotateTowardsVelocity(rotationSpeedAir);

        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            EnterAirRoll();

        Vector3 move = currentVelocity;
        move.y = verticalVelocity;
        characterController.Move(move * Time.deltaTime);

        if (characterController.isGrounded && verticalVelocity <= 0f)
            LandOnGround();
    }

    void UpdateLongJump()
    {
        ApplyLongJumpGravity();
        ApplyLongJumpMovement();
        RotateTowardsVelocity(rotationSpeedAir);

        if (characterController.isGrounded && verticalVelocity <= 0f)
            LandOnGround();
    }

    // ─────────────────────────────────────────────
    //  AIR ROLL
    // ─────────────────────────────────────────────

    void EnterAirRoll()
    {
        CurrentState = PlayerState.AirRoll;
        airRollTimer = airRollDuration;
        airRollDiveDirection = Vector3.zero;

        verticalVelocity = 0f;
        currentVelocity = Vector3.zero;

        if (playerAnimatorController == null)
            playerAnimatorController = GetComponent<PlayerAnimatorController>();
        if (playerAnimatorController != null)
            playerAnimatorController.TriggerAirRoll();

        Debug.Log("[Player] AirRoll start");
    }

    void UpdateAirRoll()
    {
        airRollTimer -= Time.deltaTime;

        Vector3 inputDir = GetCameraRelativeInput();
        if (inputDir.sqrMagnitude > 0.01f)
            airRollDiveDirection = inputDir.normalized;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (airRollDiveDirection.sqrMagnitude < 0.01f)
                airRollDiveDirection = transform.forward;

            PerformDive(airRollDiveDirection);
            return;
        }

        if (airRollTimer <= 0f)
            PerformGroundPound();
    }

    // ─────────────────────────────────────────────
    //  DIVE
    // ─────────────────────────────────────────────

    void PerformDive(Vector3 direction)
    {
        currentVelocity = direction * diveHorizontalForce;
        verticalVelocity = diveVerticalForce;
        isJumpCut = false;
        CurrentState = PlayerState.Dive;
        transform.rotation = Quaternion.LookRotation(direction);

        if (playerAnimatorController == null)
            playerAnimatorController = GetComponent<PlayerAnimatorController>();
        if (playerAnimatorController != null)
            playerAnimatorController.TriggerDive();

        Debug.Log($"[Player] Dive! dir={direction}");
    }

    void UpdateDive()
    {
        verticalVelocity -= diveGravity * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);

        RotateTowardsVelocity(rotationSpeedAir);

        Vector3 move = currentVelocity;
        move.y = verticalVelocity;
        characterController.Move(move * Time.deltaTime);

        if (characterController.isGrounded && verticalVelocity <= 0f)
            LandOnGround();
    }

    // ─────────────────────────────────────────────
    //  GROUND POUND
    // ─────────────────────────────────────────────

    void PerformGroundPound()
    {
        currentVelocity = Vector3.zero;
        verticalVelocity = -groundPoundSpeed;
        CurrentState = PlayerState.GroundPound;
        Debug.Log("[Player] GroundPound!");
    }

    void UpdateGroundPound()
    {
        verticalVelocity = -groundPoundSpeed;
        characterController.Move(Vector3.down * groundPoundSpeed * Time.deltaTime);

        if (characterController.isGrounded)
            EnterGroundPoundLand();
    }

    void EnterGroundPoundLand()
    {
        CurrentState = PlayerState.GroundPoundLand;
        groundPoundLandTimer = groundPoundLandDuration;
        currentVelocity = Vector3.zero;
        verticalVelocity = -2f;

        if (cinemachineCamera != null)
            cinemachineCamera.TriggerLandingShake();
        else if (thirdPersonCamera != null)
            thirdPersonCamera.TriggerLandingShake();

        if (playerAnimatorController == null)
            playerAnimatorController = GetComponent<PlayerAnimatorController>();
        if (playerAnimatorController != null)
            playerAnimatorController.TriggerGroundPoundSmoke();

        Debug.Log("[Player] GroundPound landed — stun");
    }

    void UpdateGroundPoundLand()
    {
        characterController.Move(Vector3.down * 2f * Time.deltaTime);

        groundPoundLandTimer -= Time.deltaTime;
        if (groundPoundLandTimer <= 0f)
        {
            CurrentState = PlayerState.Normal;
            Debug.Log("[Player] GroundPound stun ended");
        }
    }

    // ─────────────────────────────────────────────
    //  AGACHARSE
    // ─────────────────────────────────────────────

    void EnterCrouch()
    {
        CurrentState = PlayerState.Crouching;
        hasCrouchDirectionInput = false;
        Debug.Log("[Player] EnterCrouch");
    }

    void ExitCrouch()
    {
        CurrentState = PlayerState.Normal;
        Debug.Log("[Player] ExitCrouch");
    }

    void ApplyCrouchSlide()
    {
        currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, crouchSlideDeceleration * Time.deltaTime);

        Vector3 move = currentVelocity;
        move.y = verticalVelocity;
        characterController.Move(move * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  BACKFLIP
    // ─────────────────────────────────────────────

    void PerformBackflip()
    {
        verticalVelocity = backflipVerticalForce;
        isJumpCut = false;
        currentVelocity = -transform.forward * backflipHorizontalForce;
        CurrentState = PlayerState.Backflip;
        Debug.Log("[Player] Backflip!");
    }

    // ─────────────────────────────────────────────
    //  LONG JUMP
    // ─────────────────────────────────────────────

    void PerformLongJump()
    {
        Vector3 inputDir = GetCameraRelativeInput().normalized;
        if (inputDir.sqrMagnitude < 0.01f)
            inputDir = currentVelocity.sqrMagnitude > 0.01f ? currentVelocity.normalized : transform.forward;

        longJumpDirection = inputDir;
        verticalVelocity = longJumpVerticalForce;
        currentVelocity = longJumpDirection * longJumpHorizontalForce;
        isJumpCut = false;
        CurrentState = PlayerState.LongJump;
        transform.rotation = Quaternion.LookRotation(longJumpDirection);

        if (playerAnimatorController == null)
            playerAnimatorController = GetComponent<PlayerAnimatorController>();
        if (playerAnimatorController != null)
            playerAnimatorController.TriggerLongJump();

        Debug.Log($"[Player] LongJump! dir={longJumpDirection}");
    }

    void ApplyLongJumpGravity()
    {
        float gravity = verticalVelocity > 0f ? longJumpGravity : longJumpGravity * 1.8f;
        verticalVelocity -= gravity * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
    }

    void ApplyLongJumpMovement()
    {
        Vector3 inputDir = GetCameraRelativeInput();
        Vector3 lateralAdjust = Vector3.zero;

        if (inputDir.sqrMagnitude > 0.01f)
        {
            Vector3 perpendicular = Vector3.Cross(longJumpDirection, Vector3.up);
            float lateralInput = Vector3.Dot(inputDir.normalized, perpendicular);
            lateralAdjust = perpendicular * lateralInput * 3f;
        }

        Vector3 targetHorizontal = longJumpDirection * longJumpHorizontalForce + lateralAdjust;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetHorizontal, 5f * Time.deltaTime);

        Vector3 move = currentVelocity;
        move.y = verticalVelocity;
        characterController.Move(move * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  SALTO NORMAL
    // ─────────────────────────────────────────────

    void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (characterController.isGrounded || coyoteTimer > 0f)
                PerformJump(jumpForce);
            else
                jumpBufferTimer = jumpBufferTime;
        }

        if (Input.GetKeyUp(KeyCode.Space) && verticalVelocity > 0f && !isJumpCut)
            isJumpCut = true;
    }

    void UpdateCoyoteAndBuffer()
    {
        bool grounded = characterController.isGrounded;

        bool isAirborne = CurrentState == PlayerState.Jumping   ||
                          CurrentState == PlayerState.Backflip  ||
                          CurrentState == PlayerState.LongJump  ||
                          CurrentState == PlayerState.AirRoll   ||
                          CurrentState == PlayerState.Dive      ||
                          CurrentState == PlayerState.GroundPound;

        if (wasGroundedLastFrame && !grounded && !isAirborne)
            coyoteTimer = coyoteTime;

        if (coyoteTimer > 0f) coyoteTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (grounded)
            {
                PerformJump(jumpForce);
                jumpBufferTimer = 0f;
            }
        }

        wasGroundedLastFrame = grounded;
    }

    // Referencia cacheada al AnimatorController para no buscarlo en cada llamada
    private PlayerAnimatorController playerAnimatorController;

    void PerformJump(float force)
    {
        verticalVelocity = force;
        coyoteTimer = 0f;
        isJumpCut = false;
        CurrentState = PlayerState.Jumping;

        // Notificar al AnimatorController para disparar el trigger
        if (playerAnimatorController == null)
            playerAnimatorController = GetComponent<PlayerAnimatorController>();
        if (playerAnimatorController != null)
            playerAnimatorController.TriggerJump();

        Debug.Log($"[Player] Jump! force={force}");
    }

    void LandOnGround()
    {
        CurrentState = PlayerState.Normal;
        isJumpCut = false;
        coyoteTimer = 0f;
        longJumpDirection = Vector3.zero;
        airRollDiveDirection = Vector3.zero;

        if (currentVelocity.magnitude > moveSpeed)
            currentVelocity = currentVelocity.normalized * moveSpeed;

        if (cinemachineCamera != null)
            cinemachineCamera.TriggerLandingShake();
        else if (thirdPersonCamera != null)
            thirdPersonCamera.TriggerLandingShake();

        if (cameraFollow != null)
            cameraFollow.SetJumping(false, 0f);

        if (playerAnimatorController == null)
            playerAnimatorController = GetComponent<PlayerAnimatorController>();
        if (playerAnimatorController != null)
            playerAnimatorController.TriggerFallSmoke();

        Debug.Log("[Player] Landed.");
    }

    // ─────────────────────────────────────────────
    //  MOVIMIENTO Y GRAVEDAD (Normal / Jumping)
    // ─────────────────────────────────────────────

    void HandleMovementAndGravity()
    {
        bool grounded = characterController.isGrounded;
        ApplyGravity();

        Vector3 inputDir = GetCameraRelativeInput();
        bool hasInput = inputDir.sqrMagnitude > 0.01f;

        if (hasInput)
        {
            Vector3 targetVelocity = inputDir.normalized * moveSpeed;
            float dot = Vector3.Dot(currentVelocity.normalized, inputDir.normalized);
            float accel = dot < -0.2f ? directionChangeSpeed : acceleration;
            if (!grounded) accel *= 0.7f;
            currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, accel * Time.deltaTime);
        }
        else
        {
            float decel = grounded ? deceleration : deceleration * 0.4f;
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, decel * Time.deltaTime);
        }

        RotateTowardsVelocity(grounded ? rotationSpeedGround : rotationSpeedAir);

        Vector3 move = currentVelocity;
        move.y = verticalVelocity;
        characterController.Move(move * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  HELPERS DE FÍSICA
    // ─────────────────────────────────────────────

    void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity <= 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            float cutMultiplier = (isJumpCut && verticalVelocity > 0f) ? jumpCutMultiplier : 1f;
            float gravity = verticalVelocity > 0f ? riseGravity : fallGravity;
            verticalVelocity -= gravity * cutMultiplier * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
        }
    }

    void ApplyGravityInAir()
    {
        float gravity = verticalVelocity > 0f ? riseGravity : fallGravity;
        verticalVelocity -= gravity * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
    }

    void ApplyAirControl(float controlFactor)
    {
        Vector3 inputDir = GetCameraRelativeInput();
        if (inputDir.sqrMagnitude > 0.01f)
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity,
                inputDir.normalized * moveSpeed,
                acceleration * controlFactor * Time.deltaTime);
        }
        else
        {
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero,
                deceleration * 0.3f * Time.deltaTime);
        }
    }

    void RotateTowardsVelocity(float speed)
    {
        bool isThirdPerson = cinemachineCamera != null || thirdPersonCamera != null;
        if (!isThirdPerson || currentVelocity.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, speed * Time.deltaTime);
    }

    Vector3 GetCameraRelativeInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Camera cam = mainCamera != null ? mainCamera : Camera.main;
        Vector3 forward = cam != null ? cam.transform.forward : transform.forward;
        Vector3 right   = cam != null ? cam.transform.right   : transform.right;
        forward.y = 0f; forward.Normalize();
        right.y   = 0f; right.Normalize();

        return forward * v + right * h;
    }

    // ─────────────────────────────────────────────
    //  POSESIÓN
    // ─────────────────────────────────────────────

    void TryPossessNearbyEnemy()
    {
        if (Time.time - lastDismountTime < possessionCooldown) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, possessionRange);
        EnemyController closest = null;
        float closestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            EnemyController enemy = col.GetComponent<EnemyController>();
            if (enemy == null || !enemy.CanBePossessed || enemy.IsPossessed) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        if (closest != null)
            PossessEnemy(closest);
    }

    void PossessEnemy(EnemyController enemy)
    {
        verticalVelocity = 0f;
        currentVelocity  = Vector3.zero;

        if (characterController != null)
            characterController.enabled = true;

        if (cameraFollow != null)
            cameraFollow.SetJumping(false, 0f);

        CurrentState  = PlayerState.Possessing;
        possessedEnemy = enemy;
        possessedEnemy.OnPossessed();

        possessedCombatActions = enemy.GetComponent<EnemyCombatActions>(); // (+)

        transform.position = new Vector3(enemy.transform.position.x, 1.625f, enemy.transform.position.z);

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(enemy.transform);
            cameraFollow.SetPossessedMode(true);
        }

        SetModelVisible(false);

        if (cinemachineCamera != null)
            cinemachineCamera.EnterPossessionMode(enemy.transform);

        if (CrosshairController.Instance != null)
            CrosshairController.Instance.SetVisible(true);
    }

    public void ReleaseEnemy()
    {
        ReleasePossessedEnemy();
        RestoreCameraToPlayer();
        SetModelVisible(true);

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

        possessedCombatActions = null; // (+)
        isSprinting = false;
    }

    private void RestoreCameraToPlayer()
    {
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);
            cameraFollow.SetPossessedMode(false);
        }
        if (cinemachineCamera != null)
            cinemachineCamera.ExitPossessionMode(transform);
    }

    private void UpdateSprintFOV()
    {
        if (cinemachineCamera == null) return;
        var vcam = cinemachineCamera.GetActivePossessionCamera();
        if (vcam == null) return;

        float targetFOV = isAimingADS ? aimFOV : (isSprinting ? sprintFOV : normalPossessionFOV);
        vcam.Lens.FieldOfView = Mathf.Lerp(vcam.Lens.FieldOfView, targetFOV, fovTransitionSpeed * Time.deltaTime);
    }

    void UpdatePossessing()
    {
        if (possessedEnemy == null || possessedCombatActions == null)
        {
            CurrentState = PlayerState.Normal;
            return;
        }

        EnemyStats stats = possessedEnemy.Stats;

        isSprinting = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && !isAimingADS;
        isAimingADS = Input.GetMouseButton(1) && !isSprinting;

        // Sincroniza ADS con EnemyLocomotion a través de EnemyCombatActions
        possessedCombatActions.SetAiming(isAimingADS);

        if (cinemachineCamera != null)
            cinemachineCamera.SetADSState(isAimingADS);

        // Crosshair: lee la dispersión desde EnemyLocomotion (fuente única)
        if (CrosshairController.Instance != null && possessedEnemy.Inventory.HasWeapon)
        {
            float tremor     = stats != null ? stats.handTremor : 0f;
            WeaponState wState = possessedCombatActions.LocomotionState switch
            {
                EnemyLocomotionState.Running => WeaponState.Sprinting,
                EnemyLocomotionState.Walking => WeaponState.Moving,
                EnemyLocomotionState.Aiming  => WeaponState.Aiming,
                _                            => WeaponState.Idle
            };
            float dispersion = possessedEnemy.Inventory.EquippedWeapon.weaponType
                                .GetDispersion(wState, tremor);
            CrosshairController.Instance.SetDispersion(dispersion);
        }

        bool fireInput = possessedEnemy.Inventory.HasWeapon &&
                         possessedEnemy.Inventory.EquippedWeapon.weaponType.isAutomatic
                             ? Input.GetMouseButton(0)
                             : Input.GetMouseButtonDown(0);

        if (fireInput)
            TryFirePossessedWeapon();

        UpdateSprintFOV();
        HandlePossessedMovement(stats);

        Vector3 enemyPos = possessedEnemy.transform.position;
        transform.position = new Vector3(enemyPos.x, 1.625f, enemyPos.z);
    }

    void HandlePossessedMovement(EnemyStats stats)
    {
        if (possessedEnemy == null || possessedCombatActions == null) return;

        Vector3 moveDirection;
        if (cinemachineCamera != null &&
            cinemachineCamera.FirstPersonController != null &&
            cinemachineCamera.FirstPersonController.IsActive)
        {
            moveDirection = cinemachineCamera.FirstPersonController.GetMovementDirection();
        }
        else
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            moveDirection = new Vector3(h, 0f, v).normalized;
        }

        bool hasInput = moveDirection.sqrMagnitude > 0.01f;

        if (cinemachineCamera != null)
            cinemachineCamera.UpdateFPSWeaponMovement(hasInput, isSprinting);

        if (!hasInput)
            possessedCombatActions.StopMoving();
        else if (isSprinting && !isAimingADS)
            possessedCombatActions.Run(moveDirection);
        else
            possessedCombatActions.Walk(moveDirection);   // sin aimMult
    }

    void TryFirePossessedWeapon()
    {
        if (possessedEnemy == null || possessedCombatActions == null) return;
        InventoryHolder inventory = possessedEnemy.Inventory;
        if (inventory == null || !inventory.HasWeapon) return;

        EnemyStats stats = possessedEnemy.Stats;
        float tremor = stats != null ? stats.handTremor : 0f;

        Camera shooterCam = mainCamera != null ? mainCamera : Camera.main;
        bool fired = possessedCombatActions.FireWithCamera(shooterCam, tremor);

        if (fired && cinemachineCamera != null)
        {
            cinemachineCamera.TriggerWeaponRecoil();

            float shakeIntensity = inventory.EquippedWeapon.weaponType.fireShakeIntensity;
            if (shakeIntensity > 0f)
                cinemachineCamera.TriggerShake(shakeIntensity);

            CrosshairController.Instance?.AddRecoilDispersion(
                inventory.EquippedWeapon.weaponType.baseDispersion * 0.5f);
        }
    }

    void StartDismount()
    {
        if (possessedEnemy == null) return;

        lastDismountTime = Time.time;
        ReleasePossessedEnemy();
        SetModelVisible(true);

        if (cinemachineCamera != null)
            cinemachineCamera.ExitPossessionMode(transform);

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10f))
            transform.position = hit.point + Vector3.up * 0.1f;

        verticalVelocity = jumpForce * 0.6f;
        currentVelocity = Vector3.zero;
        CurrentState = PlayerState.Jumping;

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);
            cameraFollow.SetPossessedMode(false);
            cameraFollow.SetJumping(true, 0.5f);
        }
    }

    private void SetModelVisible(bool visible)
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = visible;
    }

    private WeaponState CalculateWeaponState()
    {
        if (isAimingADS)  return WeaponState.Aiming;
        if (isSprinting)  return WeaponState.Sprinting;

        bool isMoving = cinemachineCamera != null &&
                        cinemachineCamera.FirstPersonController != null &&
                        cinemachineCamera.FirstPersonController.GetMovementDirection().sqrMagnitude > 0.01f;

        return isMoving ? WeaponState.Moving : WeaponState.Idle;
    }
}
