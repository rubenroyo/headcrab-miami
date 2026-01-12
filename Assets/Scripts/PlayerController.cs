using UnityEngine;

public enum PlayerState
{
    Normal,
    Aiming,
    Jumping,
    Possessing
}

public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Referencias")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TrajectoryPreview trajectoryPreview;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Salto")]
    [SerializeField] private float jumpDuration = 1f;
    [SerializeField] private float jumpHeight = 5f;

    [Header("Posesión")]
    [SerializeField] private float possessionCooldown = 0.5f; // Tiempo de inmunidad tras desmontar
    [SerializeField] private float possessedEnemyMoveSpeed = 4f; // Velocidad del enemigo cuando se le posee

    public PlayerState CurrentState { get; private set; } = PlayerState.Normal;

    // Salto
    private float jumpElapsedTime;
    private Vector3[] jumpTrajectoryPoints;

    // Posesión
    private EnemyController possessedEnemy;
    private float lastDismountTime = -999f; // Tiempo del último desmontaje

    // Armas
    private WeaponController weaponController;

    // Test-friendly public accessors
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float JumpHeight { get => jumpHeight; set => jumpHeight = value; }
    public float JumpDuration { get => jumpDuration; set => jumpDuration = value; }

    /// <summary>
    /// Inicia el salto usando los puntos ya calculados en TrajectoryPreview.
    /// Método público para permitir tests que invoquen el salto.
    /// </summary>
    public void StartJumpPublic()
    {
        StartJump();
    }

    /// <summary>
    /// Inicia un salto usando unos puntos explícitos (útil en tests).
    /// </summary>
    public void StartJumpWithPoints(Vector3[] points, float durationOverride = -1f)
    {
        jumpTrajectoryPoints = points;

        if (durationOverride > 0f)
            jumpDuration = durationOverride;

        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length < 2)
            return;

        CurrentState = PlayerState.Jumping;
        jumpElapsedTime = 0f;

        trajectoryPreview?.SetActive(false);
        cameraFollow?.SetJumping(true, jumpDuration);
    }


    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (trajectoryPreview == null)
            trajectoryPreview = FindFirstObjectByType<TrajectoryPreview>();

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();

            // Inicializar WeaponController
            weaponController = GetComponent<WeaponController>();
            if (weaponController == null)
                weaponController = gameObject.AddComponent<WeaponController>();
    }

    void Update()
    {
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
        RotateTowardsMouse();

        if (Input.GetMouseButtonDown(1))
            EnterAiming();

        // Inputs de armas
        if (Input.GetMouseButtonDown(0) && weaponController?.EquippedWeapon != null)
        {
            weaponController.TryFire();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            weaponController?.TryReload();
        }
    }

    void UpdateAiming()
    {
        HandleMovement();
        RotateTowardsMouse();

        if (Input.GetMouseButtonDown(0))
            StartJump();

        if (Input.GetMouseButtonUp(1))
            ExitAiming();
    }

    void UpdateJump()
    {
        // Salto unificado (tanto normal como desmontaje)
        jumpElapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(jumpElapsedTime / jumpDuration);

        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length == 0)
        {
            EndJump();
            return;
        }

        float totalDistance = CalculateTotalTrajectoryDistance();
        float targetDistance = totalDistance * progress;

        Vector3 trajectoryPos = GetPositionAlongTrajectory(targetDistance);
        
        // Interpolar altura desde punto inicial al final
        float startY = jumpTrajectoryPoints[0].y;
        float endY = jumpTrajectoryPoints[^1].y;
        
        // La altura del pico es la altura inicial + jumpHeight (como offset relativo)
        float peakY = startY + jumpHeight;
        
        // Calcular altura con dos fases
        float currentY;
        if (progress < 0.5f)
        {
            // Primera mitad: subida lineal de startY a peakY
            float halfProgress = progress * 2f; // 0 a 1 durante primera mitad
            currentY = Mathf.Lerp(startY, peakY, halfProgress);
        }
        else
        {
            // Segunda mitad: bajada lineal de peakY a endY
            float halfProgress = (progress - 0.5f) * 2f; // 0 a 1 durante segunda mitad
            currentY = Mathf.Lerp(peakY, endY, halfProgress);
        }

        // Posición final: XZ de trayectoria + Y calculada
        transform.position = new Vector3(trajectoryPos.x, currentY, trajectoryPos.z);

        CheckPossessionCollision();

        if (progress >= 1f && CurrentState == PlayerState.Jumping)
            EndJump();
    }


    void UpdatePossessing()
    {
        if (possessedEnemy == null)
        {
            CurrentState = PlayerState.Normal;
            return;
        }

        // Clic izquierdo: si además mantiene clic derecho, desmonta; si no, dispara
        if (Input.GetMouseButtonDown(0))
        {
            if (Input.GetMouseButton(1))
            {
                StartDismount();
            }
            else
            {
                weaponController?.TryFire();
            }
        }

        // Mover al enemigo poseído
        HandlePossessedMovement();

        // Player se "sube" sobre el enemigo
        Vector3 pos = possessedEnemy.transform.position + Vector3.up * 3f;
        transform.position = pos;

        // Rotar hacia el mouse (igual que el jugador normal)
        RotateTowardsMouse();
    }


    // ---------------- ACTIONS ----------------

    void EnterAiming() => CurrentState = PlayerState.Aiming;
    void ExitAiming() => CurrentState = PlayerState.Normal;

    void StartJump()
    {
        jumpTrajectoryPoints = trajectoryPreview.GetTrajectoryPoints();
        if (jumpTrajectoryPoints.Length < 2)
            return;

        CurrentState = PlayerState.Jumping;
        jumpElapsedTime = 0f;

        trajectoryPreview.SetActive(false);
        cameraFollow.SetJumping(true, jumpDuration);
    }

    void EndJump()
    {
        if (CurrentState != PlayerState.Possessing)
        {
            CurrentState = PlayerState.Normal;

            // Protección: si no hay puntos de trayectoria, no intentar acceder
            if (jumpTrajectoryPoints != null && jumpTrajectoryPoints.Length > 0)
                transform.position = jumpTrajectoryPoints[^1];

            if (trajectoryPreview != null)
                trajectoryPreview.SetActive(true);

            if (cameraFollow != null)
                cameraFollow.SetJumping(false, 0f);
        }
    }

    // ---------------- POSSESSION ----------------

    void CheckPossessionCollision()
    {
        // Verificar cooldown de posesión (inmunidad tras desmontar)
        if (Time.time - lastDismountTime < possessionCooldown)
            return;

        Collider[] hits = Physics.OverlapSphere(transform.position, 1f); // radio de colisión
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
        // Cancelar salto
        EndJump();

        // Cambiar estado
        CurrentState = PlayerState.Possessing;

        // Guardar referencia
        possessedEnemy = enemy;
        possessedEnemy.OnPossessed();

        // Mover Player sobre el enemigo
        transform.position = enemy.transform.position + Vector3.up * 3f;

        // Cambiar cámara
        if (cameraFollow != null)
        {
            cameraFollow.SetJumping(false, 0f); // asegura cámara no esté en modo salto
            cameraFollow.SetTarget(enemy.transform);
            cameraFollow.SetPossessedMode(true); // Activar alturas de posesión
        }
    }

    public void ReleaseEnemy()
    {
        if (possessedEnemy != null)
        {
            possessedEnemy.OnReleased();
            possessedEnemy = null;
        }
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);
            cameraFollow.SetPossessedMode(false); // Restaurar alturas normales
        }
        CurrentState = PlayerState.Normal;
    }

    // ---------------- HELPERS ----------------

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0f, v).normalized;
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
    }

    void HandlePossessedMovement()
    {
        if (possessedEnemy == null) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0f, v).normalized;
        possessedEnemy.transform.Translate(move * possessedEnemyMoveSpeed * Time.deltaTime, Space.World);
    }

    void RotateTowardsMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);
            Vector3 dir = hitPoint - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                transform.forward = dir.normalized;

                // Si estamos poseyendo, hacer que el enemigo también mire al ratón
                if (CurrentState == PlayerState.Possessing && possessedEnemy != null)
                {
                    Vector3 enemyDir = hitPoint - possessedEnemy.transform.position;
                    enemyDir.y = 0f;
                    if (enemyDir.sqrMagnitude > 0.001f)
                        possessedEnemy.transform.forward = enemyDir.normalized;
                }
            }
        }
    }

    float CalculateTotalTrajectoryDistance()
    {
        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length < 2)
            return 0f;

        float total = 0f;
        for (int i = 0; i < jumpTrajectoryPoints.Length - 1; i++)
            total += Vector3.Distance(jumpTrajectoryPoints[i], jumpTrajectoryPoints[i + 1]);
        return total;
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

        // Marcar tiempo de desmontaje para cooldown
        lastDismountTime = Time.time;

        // Liberar enemigo inmediatamente
        possessedEnemy.OnReleased();
        possessedEnemy = null;

        // Usar la trayectoria calculada por TrajectoryPreview (igual que salto normal)
        if (trajectoryPreview != null)
        {
            jumpTrajectoryPoints = trajectoryPreview.GetTrajectoryPoints();
        }
        
        // Si no hay trayectoria, calcular punto directamente abajo como fallback
        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length == 0)
        {
            Vector3 currentPos = transform.position;
            Ray groundRay = new Ray(currentPos, Vector3.down);
            Vector3 groundPoint;
            
            if (Physics.Raycast(groundRay, out RaycastHit hit))
                groundPoint = hit.point;
            else
                groundPoint = new Vector3(currentPos.x, 0f, currentPos.z);
            
            jumpTrajectoryPoints = new Vector3[] { currentPos, groundPoint };
        }
        else
        {
            // Ajustar el último punto de la trayectoria al suelo
            // (TrajectoryPreview lo calcula desde altura del enemigo)
            Vector3 lastPoint = jumpTrajectoryPoints[^1];
            Ray groundRay = new Ray(lastPoint + Vector3.up * 10f, Vector3.down);
            
            if (Physics.Raycast(groundRay, out RaycastHit hit, 20f))
            {
                // Actualizar Y del último punto al suelo
                jumpTrajectoryPoints[^1] = new Vector3(lastPoint.x, hit.point.y, lastPoint.z);
            }
            else
            {
                // Fallback: asumir Y = 0
                jumpTrajectoryPoints[^1] = new Vector3(lastPoint.x, 0f, lastPoint.z);
            }
        }

        // Iniciar salto con la trayectoria
        jumpElapsedTime = 0f;
        CurrentState = PlayerState.Jumping;

        // Asignar cámara de nuevo al Player
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(this.transform);
            cameraFollow.SetJumping(true, jumpDuration);
        }
        
        // Desactivar preview de trayectoria
        if (trajectoryPreview != null)
            trajectoryPreview.SetActive(false);
    }
}
