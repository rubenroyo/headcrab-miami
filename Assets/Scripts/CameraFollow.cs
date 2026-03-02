using UnityEngine;

public enum CameraMode
{
    TopDown,      // Perspectiva, encima del personaje
    Isometric     // Ortográfica isométrica, Y=5 Z=-5 del personaje
}

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Mode")]
    [SerializeField] private CameraMode currentMode = CameraMode.TopDown;
    [SerializeField] private KeyCode switchModeKey = KeyCode.I;
    
    [Header("Top-Down Settings (Perspective)")]
    [SerializeField] private Vector3 topDownOffset = new Vector3(0f, 5f, 0f);
    [SerializeField] private float topDownFOV = 60f;
    
    [Header("Isometric Settings (Orthographic)")]
    [SerializeField] private Vector3 isometricOffset = new Vector3(0f, 5f, -5f);
    [SerializeField] private float isometricOrthoSize = 8f;
    [SerializeField] private float isometricPitch = 45f; // Ángulo de inclinación (hacia abajo)
    [SerializeField] private float isometricRotationSpeed = 90f; // Grados por segundo
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.E;

    [Header("Mouse look-ahead")]
    [SerializeField] private float mouseOffsetDistance = 1f;
    [SerializeField] private float followSmoothTime = 0.2f;

    [Header("Aim Camera Mode")]
    [SerializeField] private float aimCameraHeight = 10f;
    [SerializeField] private float aimBlend = 0.5f;
    [SerializeField] private float aimSmoothTime = 0.15f;
    [SerializeField] private float maxAimDistance = 10f;

    [Header("Possessed Enemy Camera")]
    [SerializeField] private float possessedCameraHeight = 10f;
    [SerializeField] private float possessedAimCameraHeight = 20f;

    [Header("Time Control")]
    [SerializeField] private float slowTimeScale = 0.5f;
    
    [Header("Pixel Snapping")]
    [Tooltip("Snappea la cámara a la grid de pixeles para evitar flickering")]
    [SerializeField] private bool enablePixelSnapping = true;
    [SerializeField] private bool debugPixelSnapping = false;
    
    // Cache de la posición original del DisplayPlane
    private Vector3 displayPlaneOriginalPosition;
    private bool displayPlaneInitialized = false;

    [Header("Debug Camera Movement")]
    [Tooltip("Muestra cada segundo información detallada sobre el movimiento de la cámara")]
    [SerializeField] private bool debugCameraMovement = false;
    private float debugTimer = 0f;
    private const float DEBUG_INTERVAL = 1f;

    private Vector3 currentVelocity;
    private Camera mainCamera;

    private bool isAiming = false;
    private bool isPlayerJumping = false;

    private float defaultTopDownOffsetY;
    private float defaultAimCameraHeight;

    private float jumpDuration;
    private float jumpElapsed;
    private float jumpStartHeight;
    
    private Vector3 jumpVelocity;
    private Vector3 jumpStartPosition;

    private SettingsController settingsController;
    
    // Rotación actual de la cámara isométrica (grados alrededor del eje Y)
    private float isometricYaw = 0f;
    
    // Propiedad pública para saber el modo actual
    public CameraMode CurrentMode => currentMode;

    void Start()
    {
        settingsController = SettingsController.Instance;
        
        if (settingsController != null && settingsController.MainCamera != null)
            mainCamera = settingsController.MainCamera;
        else if (mainCamera == null)
            mainCamera = Camera.main;
        
        defaultTopDownOffsetY = topDownOffset.y;
        defaultAimCameraHeight = aimCameraHeight;
        
        // Aplicar modo inicial
        ApplyCameraMode();
    }

    void Update()
    {
        if (isPlayerJumping)
            return;

        // Cambiar modo con tecla I
        if (Input.GetKeyDown(switchModeKey))
        {
            SwitchCameraMode();
        }
        
        // Rotar cámara isométrica con Q y E (solo en modo isométrico)
        if (currentMode == CameraMode.Isometric)
        {
            if (Input.GetKey(rotateLeftKey))
            {
                isometricYaw -= isometricRotationSpeed * Time.deltaTime;
                ApplyIsometricRotation();
            }
            if (Input.GetKey(rotateRightKey))
            {
                isometricYaw += isometricRotationSpeed * Time.deltaTime;
                ApplyIsometricRotation();
            }
            
            // Normalizar el ángulo entre 0 y 360
            if (isometricYaw < 0f) isometricYaw += 360f;
            if (isometricYaw >= 360f) isometricYaw -= 360f;
        }

        if (Input.GetMouseButtonDown(1))
            EnterAimMode();

        if (Input.GetMouseButtonUp(1))
            ExitAimMode();
    }
    
    /// <summary>
    /// Cambia entre modo TopDown e Isometric
    /// </summary>
    public void SwitchCameraMode()
    {
        currentMode = (currentMode == CameraMode.TopDown) ? CameraMode.Isometric : CameraMode.TopDown;
        ApplyCameraMode();
    }
    
    /// <summary>
    /// Aplica la configuración del modo de cámara actual
    /// </summary>
    private void ApplyCameraMode()
    {
        if (mainCamera == null) return;
        
        switch (currentMode)
        {
            case CameraMode.TopDown:
                mainCamera.orthographic = false;
                mainCamera.fieldOfView = topDownFOV;
                transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Mirando hacia abajo
                break;
                
            case CameraMode.Isometric:
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = isometricOrthoSize;
                ApplyIsometricRotation();
                break;
        }
    }
    
    /// <summary>
    /// Aplica la rotación actual de la cámara isométrica
    /// </summary>
    private void ApplyIsometricRotation()
    {
        transform.rotation = Quaternion.Euler(isometricPitch, isometricYaw, 0f);
    }
    
    /// <summary>
    /// Obtiene el offset actual según el modo de cámara
    /// </summary>
    private Vector3 GetCurrentOffset()
    {
        if (currentMode == CameraMode.TopDown)
        {
            return topDownOffset;
        }
        else
        {
            // Rotar el offset isométrico alrededor del eje Y según isometricYaw
            float radians = isometricYaw * Mathf.Deg2Rad;
            float x = isometricOffset.z * Mathf.Sin(radians);
            float z = isometricOffset.z * Mathf.Cos(radians);
            return new Vector3(x, isometricOffset.y, z);
        }
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
        Vector3 offset = GetCurrentOffset();

        // Calcular posición base según el modo
        Vector3 basePos = target.position + offset;

        if (!isAiming)
        {
            Vector3 lookDir = mouseWorldPos - target.position;
            lookDir.y = 0f;

            Vector3 mouseOffset = Vector3.zero;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                // En modo isométrico, reducir el mouse offset para que no sea tan pronunciado
                float offsetMultiplier = (currentMode == CameraMode.Isometric) ? 0.5f : 1f;
                mouseOffset = lookDir.normalized * mouseOffsetDistance * offsetMultiplier;
            }

            Vector3 desiredPos = basePos + new Vector3(mouseOffset.x, 0f, mouseOffset.z);

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

            // En modo aim, usar altura específica pero mantener offset Z para isométrico
            Vector3 aimPosition;
            if (currentMode == CameraMode.TopDown)
            {
                aimPosition = new Vector3(midPoint.x, aimCameraHeight, midPoint.z);
            }
            else
            {
                aimPosition = new Vector3(midPoint.x, aimCameraHeight, midPoint.z + offset.z);
            }

            transform.position = Vector3.SmoothDamp(
                transform.position,
                aimPosition,
                ref currentVelocity,
                aimSmoothTime
            );
        }
        
        // Aplicar pixel snapping para evitar flickering
        ApplyPixelSnapping();
        
        // Debug del movimiento de cámara
        if (debugCameraMovement)
        {
            debugTimer += Time.deltaTime;
            if (debugTimer >= DEBUG_INTERVAL)
            {
                debugTimer = 0f;
                LogCameraDebugInfo(mouseWorldPos, offset, basePos);
            }
        }
    }
    
    /// <summary>
    /// Muestra información detallada del cálculo de posición de la cámara.
    /// </summary>
    private void LogCameraDebugInfo(Vector3 mouseWorldPos, Vector3 offset, Vector3 basePos)
    {
        Vector2 mouseScreenPos = Input.mousePosition;
        
        // Calcular información del raycast
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        groundPlane.Raycast(ray, out float rayDistance);
        
        string debugInfo = "\n" +
            "╔══════════════════════════════════════════════════════════════╗\n" +
            "║           DEBUG CÁMARA - EXPLICACIÓN DE POSICIÓN            ║\n" +
            "╠══════════════════════════════════════════════════════════════╣\n" +
            $"║ MODO: {currentMode} | Apuntando: {(isAiming ? "SÍ" : "NO")}\n" +
            "╠══════════════════════════════════════════════════════════════╣\n" +
            "║ PASO 1: POSICIÓN DEL RATÓN EN PANTALLA (2D pixels)          ║\n" +
            $"║   Mouse pantalla: ({mouseScreenPos.x:F0}, {mouseScreenPos.y:F0})\n" +
            "╠══════════════════════════════════════════════════════════════╣\n" +
            "║ PASO 2: RAYO DESDE LA CÁMARA (ScreenPointToRay)             ║\n" +
            $"║   Cámara posición: ({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})\n" +
            $"║   Rayo origen: ({ray.origin.x:F2}, {ray.origin.y:F2}, {ray.origin.z:F2})\n" +
            $"║   Rayo dirección: ({ray.direction.x:F2}, {ray.direction.y:F2}, {ray.direction.z:F2})\n" +
            "║   (El rayo sale de la cámara y atraviesa el pixel del mouse)\n" +
            "╠══════════════════════════════════════════════════════════════╣\n" +
            "║ PASO 3: INTERSECCIÓN CON SUELO Y=0 (Plane.Raycast)          ║\n" +
            $"║   Distancia del rayo al suelo: {rayDistance:F2} unidades\n" +
            $"║   Punto de intersección: ({mouseWorldPos.x:F2}, {mouseWorldPos.y:F2}, {mouseWorldPos.z:F2})\n" +
            $"║   Fórmula: ray.origin + ray.direction * {rayDistance:F2}\n" +
            "╠══════════════════════════════════════════════════════════════╣\n" +
            "║ PASO 4: CÁLCULO DE POSICIÓN DE CÁMARA                       ║\n" +
            $"║   Target (jugador): ({target.position.x:F2}, {target.position.y:F2}, {target.position.z:F2})\n" +
            $"║   Offset cámara: ({offset.x:F2}, {offset.y:F2}, {offset.z:F2})\n" +
            $"║   Posición base = target + offset = ({basePos.x:F2}, {basePos.y:F2}, {basePos.z:F2})\n";
        
        if (!isAiming)
        {
            Vector3 lookDir = mouseWorldPos - target.position;
            lookDir.y = 0f;
            float offsetMultiplier = (currentMode == CameraMode.Isometric) ? 0.5f : 1f;
            Vector3 mouseOffset = lookDir.normalized * mouseOffsetDistance * offsetMultiplier;
            Vector3 desiredPos = basePos + new Vector3(mouseOffset.x, 0f, mouseOffset.z);
            
            debugInfo +=
                "╠══════════════════════════════════════════════════════════════╣\n" +
                "║ PASO 5: OFFSET POR DIRECCIÓN DEL MOUSE                      ║\n" +
                $"║   Dir mouse - jugador: ({lookDir.x:F2}, 0, {lookDir.z:F2})\n" +
                $"║   Dir normalizada * distancia * mult: ({mouseOffset.x:F2}, 0, {mouseOffset.z:F2})\n" +
                $"║   Posición deseada: ({desiredPos.x:F2}, {desiredPos.y:F2}, {desiredPos.z:F2})\n";
        }
        else
        {
            Vector3 aimDir = mouseWorldPos - target.position;
            aimDir.y = 0f;
            if (aimDir.magnitude > maxAimDistance)
                aimDir = aimDir.normalized * maxAimDistance;
            Vector3 limitedAimPos = target.position + aimDir;
            Vector3 midPoint = Vector3.Lerp(target.position, limitedAimPos, aimBlend);
            
            debugInfo +=
                "╠══════════════════════════════════════════════════════════════╣\n" +
                "║ PASO 5: MODO APUNTANDO - PUNTO MEDIO                        ║\n" +
                $"║   Dir aim (limitada): ({aimDir.x:F2}, 0, {aimDir.z:F2})\n" +
                $"║   Punto aim limitado: ({limitedAimPos.x:F2}, {limitedAimPos.y:F2}, {limitedAimPos.z:F2})\n" +
                $"║   Punto medio (blend {aimBlend}): ({midPoint.x:F2}, {midPoint.y:F2}, {midPoint.z:F2})\n";
        }
        
        debugInfo +=
            "╠══════════════════════════════════════════════════════════════╣\n" +
            "║ RESULTADO FINAL                                             ║\n" +
            $"║   Cámara actual: ({transform.position.x:F2}, {transform.position.y:F2}, {transform.position.z:F2})\n" +
            $"║   (Suavizado con SmoothDamp, tiempo: {(isAiming ? aimSmoothTime : followSmoothTime):F2}s)\n" +
            "╚══════════════════════════════════════════════════════════════╝";
        
        Debug.Log(debugInfo);
    }
    
    /// <summary>
    /// Snappea la cámara a la grid de pixeles para evitar flickering.
    /// Opcionalmente aplica sub-pixel scrolling moviendo el DisplayPlane.
    /// </summary>
    private void ApplyPixelSnapping()
    {
        if (!enablePixelSnapping || settingsController == null)
            return;
            
        // Obtener el tamaño de un pixel de RT en unidades del mundo
        float pixelWorldSize = GetPixelWorldSize();
        if (pixelWorldSize <= 0f)
            return;
        
        // Inicializar la posición original del DisplayPlane si no se ha hecho
        Transform displayPlane = settingsController.DisplayPlane;
        if (!displayPlaneInitialized && displayPlane != null)
        {
            displayPlaneOriginalPosition = displayPlane.position;
            displayPlaneInitialized = true;
        }
        
        // La posición actual de la cámara
        Vector3 currentPos = transform.position;
        
        // Snappear a la grid de pixeles grandes
        float snappedX = Mathf.Round(currentPos.x / pixelWorldSize) * pixelWorldSize;
        float snappedZ = Mathf.Round(currentPos.z / pixelWorldSize) * pixelWorldSize;
        
        // Calcular el resto (la fracción de pixel que perdemos al snappear)
        float remainderX = currentPos.x - snappedX;
        float remainderZ = currentPos.z - snappedZ;
        
        // Sub-pixel scrolling: mover el DisplayPlane para compensar el resto
        int subPixelOffsetX = 0;
        int subPixelOffsetZ = 0;
        
        if (settingsController.EnableSubPixelScrolling && displayPlane != null)
        {
            int divisions = settingsController.SubPixelDivisions;
            float subPixelSize = pixelWorldSize / divisions;
            
            // Calcular a qué sub-pixel corresponde el resto (redondeando hacia el más cercano)
            // Usamos Round para mejor aproximación
            subPixelOffsetX = Mathf.RoundToInt(remainderX / subPixelSize);
            subPixelOffsetZ = Mathf.RoundToInt(remainderZ / subPixelSize);
            
            // Clamp para asegurarnos de que esté dentro del rango válido
            subPixelOffsetX = Mathf.Clamp(subPixelOffsetX, -(divisions - 1), divisions - 1);
            subPixelOffsetZ = Mathf.Clamp(subPixelOffsetZ, -(divisions - 1), divisions - 1);
            
            // Mover el DisplayPlane: cada sub-pixel = 1 unidad de movimiento del plane
            // El plane se mueve en dirección opuesta para compensar
            Vector3 planeOffset = new Vector3(-subPixelOffsetX, 0f, -subPixelOffsetZ);
            displayPlane.position = displayPlaneOriginalPosition + planeOffset;
        }
        
        if (debugPixelSnapping)
        {
            int renderHeight = settingsController.RenderHeight;
            float distance = transform.position.y;
            float fovRad = mainCamera.fieldOfView * Mathf.Deg2Rad;
            float visibleHeight = 2f * distance * Mathf.Tan(fovRad * 0.5f);
            int divisions = settingsController.SubPixelDivisions;
            float subPixelSize = pixelWorldSize / divisions;
            
            string debugInfo = "\n" +
                "╔══════════════════════════════════════════════════════════════╗\n" +
                "║              DEBUG PIXEL SNAPPING + SUB-PIXEL               ║\n" +
                "╠══════════════════════════════════════════════════════════════╣\n" +
                "║ PASO 1: CALCULAR TAMAÑO DE PIXEL                            ║\n" +
                $"║   Altura visible: {visibleHeight:F4} u, Resolución: {renderHeight} px\n" +
                $"║   Tamaño pixel grande: {pixelWorldSize:F6} unidades\n" +
                $"║   Tamaño sub-pixel (1/{divisions}): {subPixelSize:F6} unidades\n" +
                "╠══════════════════════════════════════════════════════════════╣\n" +
                "║ PASO 2: SNAPPEAR CÁMARA A PIXEL GRANDE                      ║\n" +
                $"║   Posición original: ({currentPos.x:F6}, {currentPos.z:F6})\n" +
                $"║   Posición snappeada: ({snappedX:F6}, {snappedZ:F6})\n" +
                "╠══════════════════════════════════════════════════════════════╣\n" +
                "║ PASO 3: CALCULAR RESTO (FRACCIÓN DE PIXEL)                  ║\n" +
                $"║   Resto X: {remainderX:F6} u = {remainderX / subPixelSize:F2} sub-pixels\n" +
                $"║   Resto Z: {remainderZ:F6} u = {remainderZ / subPixelSize:F2} sub-pixels\n" +
                "╠══════════════════════════════════════════════════════════════╣\n" +
                "║ PASO 4: MOVER DISPLAY PLANE                                 ║\n" +
                $"║   Sub-pixel offset: X={subPixelOffsetX}, Z={subPixelOffsetZ}\n" +
                $"║   Plane se mueve: ({-subPixelOffsetX}, 0, {-subPixelOffsetZ}) unidades\n" +
                $"║   SubPixel activo: {settingsController.EnableSubPixelScrolling}\n" +
                "╚══════════════════════════════════════════════════════════════╝";
            
            Debug.Log(debugInfo);
        }
        
        // Aplicar la posición snappeada a la cámara
        transform.position = new Vector3(snappedX, currentPos.y, snappedZ);
    }
    
    /// <summary>
    /// Calcula el tamaño de un pixel en unidades del mundo
    /// </summary>
    private float GetPixelWorldSize()
    {
        if (mainCamera == null || settingsController == null)
            return 0f;
            
        // Usar la resolución de la RenderTexture
        int renderHeight = settingsController.RenderHeight;
        
        if (mainCamera.orthographic)
        {
            // Para cámara ortográfica: tamaño visible = orthographicSize * 2
            // pixelSize = tamañoVisible / resoluciónVertical
            return (mainCamera.orthographicSize * 2f) / renderHeight;
        }
        else
        {
            // Para cámara perspectiva: depende de la distancia
            // A la distancia de la cámara al plano del suelo (altura de la cámara)
            float distance = transform.position.y;
            float fovRad = mainCamera.fieldOfView * Mathf.Deg2Rad;
            float visibleHeight = 2f * distance * Mathf.Tan(fovRad * 0.5f);
            return visibleHeight / renderHeight;
        }
    }

    void UpdateJumpingCamera()
    {
        jumpElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(jumpElapsed / jumpDuration);
        Vector3 offset = GetCurrentOffset();

        float height = Mathf.Lerp(jumpStartHeight, offset.y, t);

        Vector3 targetPos = new Vector3(
            target.position.x,
            height,
            target.position.z + offset.z
        );

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

    /// <summary>
    /// Fuerza el modo de apuntado desde fuera (para reactivar al aterrizar).
    /// </summary>
    public void ForceAimMode(bool aiming)
    {
        if (aiming)
            EnterAimMode();
        else
            ExitAimMode();
    }

    /// <summary>
    /// Obtiene la posición del ratón en el mundo usando SettingsController centralizado.
    /// </summary>
    Vector3 GetMouseWorldPosition()
    {
        if (settingsController != null)
            return settingsController.GetMouseWorldPosition(mainCamera, Input.mousePosition);

        // Fallback si no hay SettingsController
        if (mainCamera == null)
            return target.position;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return target.position;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetPossessedMode(bool isPossessed)
    {
        if (isPossessed)
        {
            topDownOffset = new Vector3(topDownOffset.x, possessedCameraHeight, topDownOffset.z);
            aimCameraHeight = possessedAimCameraHeight;
        }
        else
        {
            topDownOffset = new Vector3(topDownOffset.x, defaultTopDownOffsetY, topDownOffset.z);
            aimCameraHeight = defaultAimCameraHeight;
        }
    }
    
    /// <summary>
    /// Establece el modo de cámara directamente
    /// </summary>
    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
        ApplyCameraMode();
    }
}