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
    [SerializeField] private TrajectoryUI trajectoryUI;
    [SerializeField] private CameraFollow cameraFollow;

    [Header("Salto")]
    [SerializeField] private float maxJumpDuration = 1f;
    [SerializeField] private float minJumpDuration = 0.2f;
    [SerializeField] private float maxJumpHeight = 5f;
    [SerializeField] private float minJumpHeight = 1f;
    
    // Valores calculados para el salto actual
    private float currentJumpDuration;
    private float currentJumpHeight;

    [Header("Posesión")]
    [SerializeField] private float possessionCooldown = 0.5f;
    [SerializeField] private float possessedEnemyMoveSpeed = 4f;

    public PlayerState CurrentState { get; private set; } = PlayerState.Normal;

    // Salto
    private float jumpElapsedTime;
    private Vector3[] jumpTrajectoryPoints;

    // Posesión
    private EnemyController possessedEnemy;
    private float lastDismountTime = -999f;

    // Referencia al SettingsController
    private SettingsController settingsController;

    // Test-friendly public accessors
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float MaxJumpHeight { get => maxJumpHeight; set => maxJumpHeight = value; }
    public float MaxJumpDuration { get => maxJumpDuration; set => maxJumpDuration = value; }
    public float CurrentJumpHeight => currentJumpHeight;
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

        // Calcular distancia y proporciones
        float jumpDistance = CalculateTotalTrajectoryDistance();
        float maxDistance = trajectoryUI != null ? trajectoryUI.MaxDistance : 10f;
        float distanceRatio = Mathf.Clamp01(jumpDistance / maxDistance);
        
        if (durationOverride > 0f)
            currentJumpDuration = durationOverride;
        else
            currentJumpDuration = Mathf.Lerp(minJumpDuration, maxJumpDuration, distanceRatio);
        
        currentJumpHeight = Mathf.Lerp(minJumpHeight, maxJumpHeight, distanceRatio);

        CurrentState = PlayerState.Jumping;
        jumpElapsedTime = 0f;

        trajectoryUI?.SetActive(false);
        cameraFollow?.SetJumping(true, currentJumpDuration);
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

        if (trajectoryUI == null)
            trajectoryUI = FindFirstObjectByType<TrajectoryUI>();

        if (cameraFollow == null)
            cameraFollow = FindFirstObjectByType<CameraFollow>();
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

        // El jugador sin poseer no puede disparar (es una seta)
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
        jumpElapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(jumpElapsedTime / currentJumpDuration);

        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length == 0)
        {
            EndJump();
            return;
        }

        float totalDistance = CalculateTotalTrajectoryDistance();
        float targetDistance = totalDistance * progress;

        Vector3 trajectoryPos = GetPositionAlongTrajectory(targetDistance);
        
        float startY = jumpTrajectoryPoints[0].y;
        float endY = jumpTrajectoryPoints[^1].y;
        float peakY = startY + currentJumpHeight;
        
        float currentY;
        if (progress < 0.5f)
        {
            float halfProgress = progress * 2f;
            currentY = Mathf.Lerp(startY, peakY, halfProgress);
        }
        else
        {
            float halfProgress = (progress - 0.5f) * 2f;
            currentY = Mathf.Lerp(peakY, endY, halfProgress);
        }

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

        if (Input.GetMouseButtonDown(0))
        {
            if (Input.GetMouseButton(1))
            {
                StartDismount();
            }
            else
            {
                TryFirePossessedWeapon();
            }
        }

        HandlePossessedMovement();

        Vector3 enemyPos = possessedEnemy.transform.position;
        transform.position = new Vector3(enemyPos.x, 1.625f, enemyPos.z);

        RotateTowardsMouse();
    }

    // ---------------- ACTIONS ----------------

    void EnterAiming() => CurrentState = PlayerState.Aiming;
    void ExitAiming() => CurrentState = PlayerState.Normal;

    void StartJump()
    {
        jumpTrajectoryPoints = trajectoryUI.GetTrajectoryPoints();
        if (jumpTrajectoryPoints.Length < 2)
            return;

        // Calcular distancia real del salto
        float jumpDistance = CalculateTotalTrajectoryDistance();
        float maxDistance = trajectoryUI.MaxDistance;
        
        // Calcular proporción (0 a 1) de la distancia máxima
        float distanceRatio = Mathf.Clamp01(jumpDistance / maxDistance);
        
        // Calcular duración y altura proporcionales
        currentJumpDuration = Mathf.Lerp(minJumpDuration, maxJumpDuration, distanceRatio);
        currentJumpHeight = Mathf.Lerp(minJumpHeight, maxJumpHeight, distanceRatio);

        CurrentState = PlayerState.Jumping;
        jumpElapsedTime = 0f;

        trajectoryUI.SetActive(false);
        cameraFollow.SetJumping(true, currentJumpDuration);
    }

    void EndJump()
    {
        if (CurrentState != PlayerState.Possessing)
        {
            if (jumpTrajectoryPoints != null && jumpTrajectoryPoints.Length > 0)
            {
                Vector3 finalPos = jumpTrajectoryPoints[^1];
                transform.position = new Vector3(finalPos.x, 0f, finalPos.z); // Forzar Y=0
            }

            if (trajectoryUI != null)
                trajectoryUI.SetActive(true);

            if (cameraFollow != null)
                cameraFollow.SetJumping(false, 0f);

            // Si el clic derecho sigue pulsado, reactivar modo apuntado
            if (Input.GetMouseButton(1))
            {
                CurrentState = PlayerState.Aiming;
                if (cameraFollow != null)
                    cameraFollow.ForceAimMode(true);
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
        
        // Forzar estado normal temporalmente para que EndJump no reactive aim
        CurrentState = PlayerState.Normal;
        
        if (jumpTrajectoryPoints != null && jumpTrajectoryPoints.Length > 0)
        {
            Vector3 finalPos = jumpTrajectoryPoints[^1];
            transform.position = new Vector3(finalPos.x, 0f, finalPos.z);
        }

        if (trajectoryUI != null)
            trajectoryUI.SetActive(true);

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
    }

    /// <summary>
    /// Libera al enemigo poseído y restaura el estado normal (sin salto).
    /// </summary>
    public void ReleaseEnemy()
    {
        ReleasePossessedEnemy();
        RestoreCameraToPlayer();
        CurrentState = PlayerState.Normal;
    }

    private void ReleasePossessedEnemy()
    {
        if (possessedEnemy != null)
        {
            possessedEnemy.OnReleased();
            possessedEnemy = null;
        }
    }

    private void RestoreCameraToPlayer()
    {
        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);
            cameraFollow.SetPossessedMode(false);
        }
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

    /// <summary>
    /// Intenta disparar con el arma del enemigo poseído
    /// </summary>
    void TryFirePossessedWeapon()
    {
        if (possessedEnemy == null) return;

        InventoryHolder inventory = possessedEnemy.GetComponent<InventoryHolder>();
        if (inventory == null) return;

        // Calcular dirección de disparo hacia el mouse
        Vector3 mouseWorld = settingsController.GetMouseWorldPosition(mainCamera, Input.mousePosition);
        Vector3 direction = mouseWorld - possessedEnemy.transform.position;
        direction.y = 0f;
        direction.Normalize();

        inventory.TryFire(direction);
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

        lastDismountTime = Time.time;
        ReleasePossessedEnemy();

        if (trajectoryUI != null)
        {
            jumpTrajectoryPoints = trajectoryUI.GetTrajectoryPoints();
        }
        
        if (jumpTrajectoryPoints == null || jumpTrajectoryPoints.Length == 0)
        {
            Vector3 currentPos = transform.position;
            Ray groundRay = new Ray(currentPos, Vector3.down);
            Vector3 groundPoint;
            
            if (Physics.Raycast(groundRay, out RaycastHit hit))
                groundPoint = new Vector3(hit.point.x, 0f, hit.point.z); // Y=0 (nivel del suelo)
            else
                groundPoint = new Vector3(currentPos.x, 0f, currentPos.z);
            
            jumpTrajectoryPoints = new Vector3[] { currentPos, groundPoint };
        }
        else
        {
            Vector3 lastPoint = jumpTrajectoryPoints[^1];
            // Forzar Y=0 en el punto final (nivel del suelo)
            jumpTrajectoryPoints[^1] = new Vector3(lastPoint.x, 0f, lastPoint.z);
        }

        // Calcular duración y altura para el dismount
        float jumpDistance = CalculateTotalTrajectoryDistance();
        float maxDistance = trajectoryUI != null ? trajectoryUI.MaxDistance : 10f;
        float distanceRatio = Mathf.Clamp01(jumpDistance / maxDistance);
        currentJumpDuration = Mathf.Lerp(minJumpDuration, maxJumpDuration, distanceRatio);
        currentJumpHeight = Mathf.Lerp(minJumpHeight, maxJumpHeight, distanceRatio);

        jumpElapsedTime = 0f;
        CurrentState = PlayerState.Jumping;

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(transform);
            cameraFollow.SetPossessedMode(false); // Restaurar altura de cámara a normal
            cameraFollow.SetJumping(true, currentJumpDuration);
        }
        
        if (trajectoryUI != null)
            trajectoryUI.SetActive(false);
    }
}