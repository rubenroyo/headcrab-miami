using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Offset base")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 20f, 0f);

    [Header("Mouse look-ahead")]
    [SerializeField] private float mouseOffsetDistance = 1f;
    [SerializeField] private float followSmoothTime = 0.2f;

    [Header("Aim Camera Mode")]
    [SerializeField] private float aimCameraHeight = 30f;
    [SerializeField] private float aimBlend = 0.5f;
    [SerializeField] private float aimSmoothTime = 0.15f;
    [SerializeField] private float maxAimDistance = 10f;

    [Header("Time Control")]
    [SerializeField] private float slowTimeScale = 0.5f;

    private Vector3 currentVelocity;
    private Camera mainCamera;

    private bool isAiming = false;
    private bool isPlayerJumping = false;

    private float jumpDuration;
    private float jumpElapsed;
    private float jumpStartHeight;
    
    private Vector3 jumpVelocity;
    private Vector3 jumpStartPosition;


    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (isPlayerJumping)
            return;

        if (Input.GetMouseButtonDown(1))
            EnterAimMode();

        if (Input.GetMouseButtonUp(1))
            ExitAimMode();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        if (isPlayerJumping)
        {
            UpdateJumpingCamera();
            return;
        }

        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // ⬇️ Y FIJA, no dependiente del jugador
        Vector3 basePos = new Vector3(
            target.position.x,
            baseOffset.y,
            target.position.z
        );

        if (!isAiming)
        {
            Vector3 lookDir = mouseWorldPos - target.position;
            lookDir.y = 0f;

            Vector3 mouseOffset = Vector3.zero;
            if (lookDir.sqrMagnitude > 0.01f)
                mouseOffset = lookDir.normalized * mouseOffsetDistance;

            Vector3 desiredPos = basePos + mouseOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPos,
                ref currentVelocity,
                followSmoothTime
            );
        }
        else
        {
            Vector3 aimDir = mouseWorldPos - target.position;
            aimDir.y = 0f;

            if (aimDir.magnitude > maxAimDistance)
                aimDir = aimDir.normalized * maxAimDistance;

            Vector3 limitedAimPos = target.position + aimDir;

            Vector3 midPoint = Vector3.Lerp(
                target.position,
                limitedAimPos,
                aimBlend
            );

            Vector3 aimPosition = new Vector3(
                midPoint.x,
                aimCameraHeight,
                midPoint.z
            );

            transform.position = Vector3.SmoothDamp(
                transform.position,
                aimPosition,
                ref currentVelocity,
                aimSmoothTime
            );
        }
    }

    void UpdateJumpingCamera()
    {
        jumpElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(jumpElapsed / jumpDuration);

        // Altura interpolada
        float height = Mathf.Lerp(jumpStartHeight, baseOffset.y, t);

        // Posición objetivo REAL (donde debería acabar)
        Vector3 targetPos = new Vector3(
            target.position.x,
            height,
            target.position.z
        );

        // SmoothDamp completo XYZ (no teletransporte)
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref jumpVelocity,
            followSmoothTime
        );

        if (t >= 1f)
            isPlayerJumping = false;
    }


    public void SetJumping(bool jumping, float duration)
    {
        isPlayerJumping = jumping;
        jumpDuration = duration;
        jumpElapsed = 0f;

        if (jumping)
        {
            jumpStartPosition = transform.position;
            jumpStartHeight = transform.position.y;

            if (isAiming)
                ExitAimMode();
        }
    }


    void EnterAimMode()
    {
        isAiming = true;
        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    void ExitAimMode()
    {
        isAiming = false;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return target.position;
    }
}
