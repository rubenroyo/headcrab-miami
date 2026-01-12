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


    public PlayerState CurrentState { get; private set; } = PlayerState.Normal;

    // Salto
    private float jumpElapsedTime;
    private Vector3[] jumpTrajectoryPoints;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (trajectoryPreview == null)
            trajectoryPreview = FindFirstObjectByType<TrajectoryPreview>();

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
        }
    }

    // ---------------- STATES ----------------

    void UpdateNormal()
    {
        HandleMovement();
        RotateTowardsMouse();

        // Entrar en apuntar
        if (Input.GetMouseButtonDown(1))
            EnterAiming();

        // 🚫 No se puede disparar aquí (bloqueado por diseño)
    }

    void UpdateAiming()
    {
        RotateTowardsMouse();

        // Iniciar salto
        if (Input.GetMouseButtonDown(0))
            StartJump();

        // Salir de apuntar
        if (Input.GetMouseButtonUp(1))
            ExitAiming();
    }

    void UpdateJump()
    {
        jumpElapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(jumpElapsedTime / jumpDuration);

        float totalDistance = CalculateTotalTrajectoryDistance();
        float targetDistance = totalDistance * progress;

        Vector3 trajectoryPos = GetPositionAlongTrajectory(targetDistance);

        float parabola = 4f * progress * (1f - progress);
        float verticalOffset = parabola * jumpHeight;

        transform.position = trajectoryPos + Vector3.up * verticalOffset;

        if (progress >= 1f)
            EndJump();
    }

    // ---------------- ACTIONS ----------------

    void EnterAiming()
    {
        CurrentState = PlayerState.Aiming;
    }

    void ExitAiming()
    {
        CurrentState = PlayerState.Normal;
    }

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
        CurrentState = PlayerState.Normal;

        transform.position = jumpTrajectoryPoints[^1];

        trajectoryPreview.SetActive(true);
        cameraFollow.SetJumping(false, 0f);
    }

    // ---------------- HELPERS ----------------

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, 0f, v).normalized;
        transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);
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
                transform.forward = dir.normalized;
        }
    }

    float CalculateTotalTrajectoryDistance()
    {
        float total = 0f;
        for (int i = 0; i < jumpTrajectoryPoints.Length - 1; i++)
            total += Vector3.Distance(jumpTrajectoryPoints[i], jumpTrajectoryPoints[i + 1]);
        return total;
    }

    Vector3 GetPositionAlongTrajectory(float distance)
    {
        float current = 0f;

        for (int i = 0; i < jumpTrajectoryPoints.Length - 1; i++)
        {
            float segLen = Vector3.Distance(jumpTrajectoryPoints[i], jumpTrajectoryPoints[i + 1]);
            if (current + segLen >= distance)
                return Vector3.Lerp(
                    jumpTrajectoryPoints[i],
                    jumpTrajectoryPoints[i + 1],
                    (distance - current) / segLen
                );

            current += segLen;
        }

        return jumpTrajectoryPoints[^1];
    }
}
