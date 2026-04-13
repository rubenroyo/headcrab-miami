using UnityEngine;

/// <summary>
/// Cámara de tercera persona con sistema de órbitas estilo Cinemachine.
/// Usa 3 "rigs" (órbitas) a diferentes alturas que se interpolan según el pitch.
/// Incluye detección de colisiones, offset configurable y visualización con Gizmos.
/// </summary>
public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [System.Serializable]
    public class OrbitRig
    {
        [Tooltip("Altura del rig relativa al target")]
        public float height = 2f;
        
        [Tooltip("Radio del círculo de órbita")]
        public float radius = 4f;
        
        [Tooltip("Color del gizmo para este rig")]
        public Color gizmoColor = Color.yellow;
    }
    
    [Header("Target")]
    [SerializeField] private Transform target;
    [Tooltip("El transform del jugador que rotará en el eje Y (horizontal)")]
    [SerializeField] private Transform playerBody;
    
    [Header("Camera Orbits - Normal")]
    [Tooltip("Órbita superior (cuando miras hacia arriba)")]
    [SerializeField] private OrbitRig topRig = new OrbitRig { height = 4f, radius = 2f, gizmoColor = Color.cyan };
    
    [Tooltip("Órbita media (nivel de los ojos)")]
    [SerializeField] private OrbitRig middleRig = new OrbitRig { height = 1.5f, radius = 3f, gizmoColor = Color.yellow };
    
    [Tooltip("Órbita inferior (cuando miras hacia abajo)")]
    [SerializeField] private OrbitRig bottomRig = new OrbitRig { height = 0.2f, radius = 4f, gizmoColor = Color.green };
    
    [Header("Camera Orbits - Aim Mode")]
    [Tooltip("Órbita superior durante apuntado")]
    [SerializeField] private OrbitRig aimTopRig = new OrbitRig { height = 2.5f, radius = 1.5f, gizmoColor = new Color(1f, 0.5f, 0f) };
    
    [Tooltip("Órbita media durante apuntado")]
    [SerializeField] private OrbitRig aimMiddleRig = new OrbitRig { height = 1.2f, radius = 2f, gizmoColor = new Color(1f, 0.7f, 0f) };
    
    [Tooltip("Órbita inferior durante apuntado")]
    [SerializeField] private OrbitRig aimBottomRig = new OrbitRig { height = 0.3f, radius = 2.5f, gizmoColor = new Color(1f, 0.9f, 0f) };
    
    [Header("Offset - Normal")]
    [Tooltip("Desplazamiento horizontal de la cámara (positivo = derecha del personaje)")]
    [SerializeField] private float horizontalOffset = 0.4f;
    
    [Tooltip("Desplazamiento vertical del punto de mira (positivo = arriba)")]
    [SerializeField] private float verticalOffset = 0.4f;
    
    [Header("Offset - Aim Mode")]
    [SerializeField] private float aimHorizontalOffset = 0.6f;
    [SerializeField] private float aimVerticalOffset = 0.5f;
    
    [Header("Input Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float lookSmoothTime = 0.05f;
    
    [Header("Vertical Limits")]
    [Tooltip("Ángulo mínimo (mirando hacia arriba, valor negativo)")]
    [SerializeField] private float minPitch = -40f;
    
    [Tooltip("Ángulo máximo (mirando hacia abajo, valor positivo)")]
    [SerializeField] private float maxPitch = 70f;
    
    [Header("Collision")]
    [Tooltip("Activar detección de colisiones con paredes y suelo")]
    [SerializeField] private bool enableCollision = true;
    
    [Tooltip("Radio de la esfera de colisión de la cámara")]
    [SerializeField] private float collisionRadius = 0.2f;
    
    [Tooltip("Qué capas bloquean la cámara")]
    [SerializeField] private LayerMask collisionMask = ~0; // Todo por defecto
    
    [Tooltip("Distancia mínima al target cuando hay colisión")]
    [SerializeField] private float minDistanceFromTarget = 0.5f;
    
    [Tooltip("Velocidad de recuperación cuando ya no hay obstrucción")]
    [SerializeField] private float collisionRecoverySpeed = 8f;
    
    [Header("Smoothing")]
    [Tooltip("Suavizado del movimiento de la cámara")]
    [SerializeField] private float positionSmoothTime = 0.1f;
    
    [Header("Camera Settings")]
    [SerializeField] private float fieldOfView = 90f;
    [SerializeField] private float aimFieldOfView = 60f;
    [SerializeField] private float jumpFieldOfView = 110f;
    
    [Header("Jump Mode")]
    [Tooltip("Órbitas durante el salto - más cercanas y bajas para sensación de velocidad")]
    [SerializeField] private OrbitRig jumpTopRig = new OrbitRig { height = 2f, radius = 1.5f, gizmoColor = Color.red };
    [SerializeField] private OrbitRig jumpMiddleRig = new OrbitRig { height = 0.8f, radius = 2f, gizmoColor = Color.red };
    [SerializeField] private OrbitRig jumpBottomRig = new OrbitRig { height = 0.3f, radius = 2.5f, gizmoColor = Color.red };
    
    [Tooltip("Offset durante el salto")]
    [SerializeField] private float jumpHorizontalOffset = 0.3f;
    [SerializeField] private float jumpVerticalOffset = 0.3f;
    
    [Tooltip("Velocidad de transición al modo salto (para jumpBlend)")]
    [SerializeField] private float jumpTransitionSpeed = 12f;
    
    [Header("Jump Camera Timing")]
    [Tooltip("Instante A: Duración del cambio de FOV después de iniciar el salto (segundos)")]
    [SerializeField] private float jumpFOVDuration = 0.3f;
    
    [Tooltip("Instante B: Duración de la aceleración de la cámara desde 0 hasta velocidad V (segundos)")]
    [SerializeField] private float jumpCameraAccelDuration = 0.2f;
    
    [Tooltip("Velocidad V: Velocidad máxima de la cámara para alcanzar al jugador (unidades/segundo)")]
    [SerializeField] private float jumpCameraMaxSpeed = 25f;
    
    [Header("Screen Shake")]
    [SerializeField] private float landingShakeIntensity = 0.3f;
    [SerializeField] private float landingShakeDuration = 0.15f;
    
    [Header("Aim Transition")]
    [Tooltip("Velocidad de transición entre modo normal y aim")]
    [SerializeField] private float aimTransitionSpeed = 8f;
    
    [Header("Gizmos")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private int gizmoSegments = 32;
    
    // Estado interno
    private float pitch = 0f;
    private float yaw = 0f;
    private float targetPitch = 0f;
    private float targetYaw = 0f;
    
    // Velocidades de suavizado
    private float pitchVelocity;
    private float yawVelocity;
    private Vector3 positionVelocity;
    
    // Colisión
    private float currentDistance;
    private float targetDistance;
    
    // Cache
    private Camera cam;
    private bool cursorLocked = true;
    
    // Aim mode
    private bool isAiming = false;
    private float aimBlend = 0f; // 0 = normal, 1 = aim
    
    // Jump mode
    private bool isJumping = false;
    private float jumpBlend = 0f; // 0 = not jumping, 1 = jumping
    private float jumpProgress = 0f; // 0 a 1 durante el salto
    
    // Jump camera timing (instantes A, B, V)
    private float jumpStartTime = 0f; // Tiempo (unscaled) cuando empezó el salto
    private float jumpFOVStartValue = 0f; // FOV al inicio del salto
    private float jumpCameraVelocity = 0f; // Velocidad actual de la cámara durante el salto
    
    // Screen shake
    private float shakeTimer = 0f;
    private float shakeDuration = 0f;
    private float shakeIntensity = 0f;
    private Vector3 shakeOffset = Vector3.zero;
    
    // Propiedades públicas
    public float Pitch => pitch;
    public float Yaw => yaw;
    public Transform Target => target;
    public bool IsAiming => isAiming;
    public bool IsJumping => isJumping;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
        }
        
        LockCursor(true);
        
        // Inicializar con la rotación actual del player
        if (playerBody != null)
        {
            yaw = playerBody.eulerAngles.y;
            targetYaw = yaw;
        }
        
        // Calcular distancia inicial
        currentDistance = GetOrbitRadiusAtPitch(pitch);
        targetDistance = currentDistance;
    }
    
    void Update()
    {
        HandleCursorLock();
        
        if (!cursorLocked)
            return;
        
        HandleMouseInput();
    }
    
    void LateUpdate()
    {
        if (target == null)
            return;
        
        // Interpolar aim blend usando deltaTime sin escalar (para que funcione en slow motion)
        float targetAimBlend = isAiming ? 1f : 0f;
        aimBlend = Mathf.MoveTowards(aimBlend, targetAimBlend, aimTransitionSpeed * Time.unscaledDeltaTime);
        
        // Interpolar jump blend
        float targetJumpBlend = isJumping ? 1f : 0f;
        jumpBlend = Mathf.MoveTowards(jumpBlend, targetJumpBlend, jumpTransitionSpeed * Time.unscaledDeltaTime);
        
        // Interpolar FOV con sistema de instantes A/B/V durante el salto
        if (cam != null)
        {
            if (isJumping)
            {
                // Sistema de instante A: FOV cambia durante jumpFOVDuration segundos
                float timeSinceJumpStart = Time.unscaledTime - jumpStartTime;
                float fovT = jumpFOVDuration > 0f ? Mathf.Clamp01(timeSinceJumpStart / jumpFOVDuration) : 1f;
                
                // Suavizar con ease-out para que empiece rápido y termine suave
                float smoothFovT = 1f - (1f - fovT) * (1f - fovT);
                
                // FOV target durante el salto con boost sinusoidal
                float jumpTargetFOV = jumpFieldOfView + Mathf.Sin(jumpProgress * Mathf.PI) * 10f;
                
                cam.fieldOfView = Mathf.Lerp(jumpFOVStartValue, jumpTargetFOV, smoothFovT);
            }
            else
            {
                // Fuera del salto: interpolar normalmente
                float baseFOV = Mathf.Lerp(fieldOfView, aimFieldOfView, aimBlend);
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, baseFOV, aimTransitionSpeed * Time.unscaledDeltaTime);
            }
        }
        
        // Actualizar screen shake
        UpdateScreenShake();
        
        UpdateCameraPosition();
    }
    
    void UpdateScreenShake()
    {
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            float t = shakeTimer / shakeDuration;
            float intensity = shakeIntensity * t; // Decae linealmente
            
            shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * intensity,
                Random.Range(-1f, 1f) * intensity,
                0f
            );
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }
    
    void HandleCursorLock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LockCursor(!cursorLocked);
        }
        
        if (!cursorLocked && Input.GetMouseButtonDown(0))
        {
            LockCursor(true);
        }
    }
    
    void HandleMouseInput()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        targetYaw += mouseX;
        targetPitch += mouseY; // Positivo = mirar hacia abajo
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        
        // Suavizar input usando unscaledDeltaTime para que funcione en slow motion
        if (lookSmoothTime > 0f)
        {
            yaw = Mathf.SmoothDamp(yaw, targetYaw, ref yawVelocity, lookSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            pitch = Mathf.SmoothDamp(pitch, targetPitch, ref pitchVelocity, lookSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }
        else
        {
            yaw = targetYaw;
            pitch = targetPitch;
        }
        
        // NO rotamos el playerBody aquí - el jugador rota hacia donde se mueve
    }
    
    void UpdateCameraPosition()
    {
        // Interpolar offsets según aimBlend, luego jumpBlend
        float baseVerticalOffset = Mathf.Lerp(verticalOffset, aimVerticalOffset, aimBlend);
        float baseHorizontalOffset = Mathf.Lerp(horizontalOffset, aimHorizontalOffset, aimBlend);
        
        float currentVerticalOffset = Mathf.Lerp(baseVerticalOffset, jumpVerticalOffset, jumpBlend);
        float currentHorizontalOffset = Mathf.Lerp(baseHorizontalOffset, jumpHorizontalOffset, jumpBlend);
        
        // Punto de mira (target + offset vertical)
        Vector3 lookAtPoint = target.position + Vector3.up * currentVerticalOffset;
        
        // Calcular posición en la órbita
        Vector3 orbitPosition = CalculateOrbitPosition();
        
        // Aplicar offset horizontal (perpendicular a la dirección de la cámara)
        Vector3 rightDirection = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
        Vector3 desiredPosition = orbitPosition + rightDirection * currentHorizontalOffset;
        
        // Detectar colisiones
        if (enableCollision && !isJumping) // Deshabilitar colisión durante salto para evitar saltos de cámara
        {
            desiredPosition = HandleCollision(lookAtPoint, desiredPosition);
        }
        
        // Sistema de movimiento de cámara: durante el salto usa instantes B/V
        if (isJumping)
        {
            // Sistema de instante B: la cámara acelera durante jumpCameraAccelDuration segundos
            float timeSinceJumpStart = Time.unscaledTime - jumpStartTime;
            float accelT = jumpCameraAccelDuration > 0f ? Mathf.Clamp01(timeSinceJumpStart / jumpCameraAccelDuration) : 1f;
            
            // Suavizar aceleración con ease-in para que acelere gradualmente
            float smoothAccelT = accelT * accelT;
            
            // Velocidad actual de la cámara (aumenta de 0 a jumpCameraMaxSpeed)
            jumpCameraVelocity = jumpCameraMaxSpeed * smoothAccelT;
            
            // Mover hacia la posición deseada a velocidad jumpCameraVelocity
            Vector3 toDesired = desiredPosition - transform.position;
            float distance = toDesired.magnitude;
            
            if (distance > 0.001f)
            {
                float step = jumpCameraVelocity * Time.unscaledDeltaTime;
                if (step >= distance)
                {
                    transform.position = desiredPosition;
                }
                else
                {
                    transform.position += toDesired.normalized * step;
                }
            }
        }
        else
        {
            // Fuera del salto: suavizar posición normalmente
            if (positionSmoothTime > 0f)
            {
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            }
            else
            {
                transform.position = desiredPosition;
            }
        }
        
        // Aplicar screen shake
        transform.position += shakeOffset;
        
        // Mirar siempre al punto de mira
        transform.LookAt(lookAtPoint);
    }
    
    /// <summary>
    /// Calcula la posición de la cámara en la órbita según el pitch actual
    /// </summary>
    Vector3 CalculateOrbitPosition()
    {
        // Normalizar pitch a rango [0, 1] para interpolación
        // minPitch (-40) = 0 (top), 0 = 0.5 (middle), maxPitch (70) = 1 (bottom)
        float t = Mathf.InverseLerp(minPitch, maxPitch, pitch);
        
        // Obtener altura y radio interpolados
        float height = GetOrbitHeightAtT(t);
        float radius = GetOrbitRadiusAtT(t);
        
        // Guardar distancia objetivo para colisión
        targetDistance = radius;
        
        // Calcular posición en el círculo
        float yawRad = yaw * Mathf.Deg2Rad;
        
        // La cámara está detrás del personaje
        float x = -Mathf.Sin(yawRad) * radius;
        float z = -Mathf.Cos(yawRad) * radius;
        
        return target.position + new Vector3(x, height, z);
    }
    
    /// <summary>
    /// Interpola la altura entre los 3 rigs según t (0=top, 0.5=middle, 1=bottom)
    /// Tiene en cuenta aimBlend y jumpBlend para interpolar entre rigs
    /// </summary>
    float GetOrbitHeightAtT(float t)
    {
        // Obtener alturas de los rigs normales
        float normalHeight;
        if (t < 0.5f)
        {
            float localT = t * 2f;
            normalHeight = Mathf.Lerp(topRig.height, middleRig.height, localT);
        }
        else
        {
            float localT = (t - 0.5f) * 2f;
            normalHeight = Mathf.Lerp(middleRig.height, bottomRig.height, localT);
        }
        
        // Obtener alturas de los rigs aim
        float aimHeight;
        if (t < 0.5f)
        {
            float localT = t * 2f;
            aimHeight = Mathf.Lerp(aimTopRig.height, aimMiddleRig.height, localT);
        }
        else
        {
            float localT = (t - 0.5f) * 2f;
            aimHeight = Mathf.Lerp(aimMiddleRig.height, aimBottomRig.height, localT);
        }
        
        // Obtener alturas de los rigs jump
        float jumpHeight;
        if (t < 0.5f)
        {
            float localT = t * 2f;
            jumpHeight = Mathf.Lerp(jumpTopRig.height, jumpMiddleRig.height, localT);
        }
        else
        {
            float localT = (t - 0.5f) * 2f;
            jumpHeight = Mathf.Lerp(jumpMiddleRig.height, jumpBottomRig.height, localT);
        }
        
        // Interpolar entre normal y aim según aimBlend
        float baseHeight = Mathf.Lerp(normalHeight, aimHeight, aimBlend);
        // Luego interpolar con jump según jumpBlend
        return Mathf.Lerp(baseHeight, jumpHeight, jumpBlend);
    }
    
    /// <summary>
    /// Interpola el radio entre los 3 rigs según t (0=top, 0.5=middle, 1=bottom)
    /// Tiene en cuenta aimBlend y jumpBlend para interpolar entre rigs
    /// </summary>
    float GetOrbitRadiusAtT(float t)
    {
        // Obtener radios de los rigs normales
        float normalRadius;
        if (t < 0.5f)
        {
            float localT = t * 2f;
            normalRadius = Mathf.Lerp(topRig.radius, middleRig.radius, localT);
        }
        else
        {
            float localT = (t - 0.5f) * 2f;
            normalRadius = Mathf.Lerp(middleRig.radius, bottomRig.radius, localT);
        }
        
        // Obtener radios de los rigs aim
        float aimRadius;
        if (t < 0.5f)
        {
            float localT = t * 2f;
            aimRadius = Mathf.Lerp(aimTopRig.radius, aimMiddleRig.radius, localT);
        }
        else
        {
            float localT = (t - 0.5f) * 2f;
            aimRadius = Mathf.Lerp(aimMiddleRig.radius, aimBottomRig.radius, localT);
        }
        
        // Obtener radios de los rigs jump
        float jumpRadius;
        if (t < 0.5f)
        {
            float localT = t * 2f;
            jumpRadius = Mathf.Lerp(jumpTopRig.radius, jumpMiddleRig.radius, localT);
        }
        else
        {
            float localT = (t - 0.5f) * 2f;
            jumpRadius = Mathf.Lerp(jumpMiddleRig.radius, jumpBottomRig.radius, localT);
        }
        
        // Interpolar entre normal y aim según aimBlend
        float baseRadius = Mathf.Lerp(normalRadius, aimRadius, aimBlend);
        // Luego interpolar con jump según jumpBlend
        return Mathf.Lerp(baseRadius, jumpRadius, jumpBlend);
    }
    
    /// <summary>
    /// Obtiene el radio de órbita para un pitch dado
    /// </summary>
    float GetOrbitRadiusAtPitch(float p)
    {
        float t = Mathf.InverseLerp(minPitch, maxPitch, p);
        return GetOrbitRadiusAtT(t);
    }
    
    /// <summary>
    /// Maneja las colisiones de la cámara con el entorno
    /// </summary>
    Vector3 HandleCollision(Vector3 lookAtPoint, Vector3 desiredPosition)
    {
        Vector3 direction = desiredPosition - lookAtPoint;
        float distance = direction.magnitude;
        direction.Normalize();
        
        // SphereCast desde el punto de mira hacia la posición deseada
        if (Physics.SphereCast(lookAtPoint, collisionRadius, direction, out RaycastHit hit, distance, collisionMask))
        {
            // Hay obstrucción, acercar la cámara
            float hitDistance = Mathf.Max(hit.distance - collisionRadius, minDistanceFromTarget);
            currentDistance = hitDistance;
            return lookAtPoint + direction * currentDistance;
        }
        else
        {
            // No hay obstrucción, recuperar distancia gradualmente
            currentDistance = Mathf.MoveTowards(currentDistance, distance, collisionRecoverySpeed * Time.unscaledDeltaTime);
            
            // Si estamos muy cerca de la distancia objetivo, usar directamente
            if (Mathf.Abs(currentDistance - distance) < 0.01f)
            {
                currentDistance = distance;
            }
            
            return lookAtPoint + direction * currentDistance;
        }
    }
    
    /// <summary>
    /// Bloquea o libera el cursor
    /// </summary>
    public void LockCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
    
    /// <summary>
    /// Establece la sensibilidad del ratón
    /// </summary>
    public void SetSensitivity(float sensitivity)
    {
        mouseSensitivity = sensitivity;
    }
    
    /// <summary>
    /// Obtiene la dirección hacia donde mira la cámara
    /// </summary>
    public Vector3 GetLookDirection()
    {
        return transform.forward;
    }
    
    /// <summary>
    /// Establece un nuevo body de jugador
    /// </summary>
    public void SetPlayerBody(Transform newBody)
    {
        playerBody = newBody;
        if (newBody != null)
        {
            yaw = newBody.eulerAngles.y;
            targetYaw = yaw;
        }
    }
    
    /// <summary>
    /// Establece un nuevo target para seguir
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    /// <summary>
    /// Teletransporta la cámara a un nuevo objetivo
    /// </summary>
    public void SnapToTarget(Transform newTarget, float newYaw)
    {
        if (newTarget != null)
        {
            target = newTarget;
            yaw = newYaw;
            targetYaw = newYaw;
            
            // Forzar actualización de posición sin suavizado
            Vector3 lookAtPoint = target.position + Vector3.up * verticalOffset;
            Vector3 orbitPosition = CalculateOrbitPosition();
            Vector3 rightDirection = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            transform.position = orbitPosition + rightDirection * horizontalOffset;
            transform.LookAt(lookAtPoint);
        }
    }
    
    /// <summary>
    /// Activa el modo de apuntado (acerca la cámara, reduce FOV)
    /// </summary>
    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }
    
    /// <summary>
    /// Entra en modo aim
    /// </summary>
    public void EnterAimMode()
    {
        isAiming = true;
    }
    
    /// <summary>
    /// Sale del modo aim
    /// </summary>
    public void ExitAimMode()
    {
        isAiming = false;
    }
    
    /// <summary>
    /// Entra en modo salto (aleja la cámara, aumenta FOV)
    /// </summary>
    public void EnterJumpMode()
    {
        isJumping = true;
        isAiming = false; // Desactivar aim durante el salto
        jumpProgress = 0f;
        
        // Sistema de instantes A/B/V
        jumpStartTime = Time.unscaledTime;
        jumpFOVStartValue = cam != null ? cam.fieldOfView : fieldOfView;
        jumpCameraVelocity = 0f; // La cámara empieza quieta y acelera
    }
    
    /// <summary>
    /// Sale del modo salto
    /// </summary>
    public void ExitJumpMode()
    {
        isJumping = false;
        jumpProgress = 0f;
    }
    
    /// <summary>
    /// Actualiza el progreso del salto (0 = inicio, 1 = fin)
    /// </summary>
    public void UpdateJumpProgress(float progress)
    {
        jumpProgress = Mathf.Clamp01(progress);
    }
    
    /// <summary>
    /// Dispara el screen shake de aterrizaje
    /// </summary>
    public void TriggerLandingShake()
    {
        shakeTimer = landingShakeDuration;
        shakeDuration = landingShakeDuration;
        shakeIntensity = landingShakeIntensity;
    }
    
    /// <summary>
    /// Dispara un screen shake personalizado
    /// </summary>
    public void TriggerShake(float intensity, float duration)
    {
        shakeTimer = duration;
        shakeDuration = duration;
        shakeIntensity = intensity;
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;
        
        // Usar el target si está asignado, si no usar la posición del objeto
        Vector3 center = target != null ? target.position : transform.position;
        
        // Dibujar las 3 órbitas
        DrawOrbitGizmo(center, topRig, "Top");
        DrawOrbitGizmo(center, middleRig, "Middle");
        DrawOrbitGizmo(center, bottomRig, "Bottom");
        
        // Dibujar líneas conectando las órbitas
        DrawOrbitConnections(center);
        
        // Dibujar la posición actual de la cámara si está en play
        if (Application.isPlaying && target != null)
        {
            // Línea desde el target al punto de mira
            Vector3 lookAtPoint = target.position + Vector3.up * verticalOffset;
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lookAtPoint, 0.1f);
            Gizmos.DrawLine(target.position, lookAtPoint);
            
            // Línea desde el punto de mira a la cámara
            Gizmos.color = Color.white;
            Gizmos.DrawLine(lookAtPoint, transform.position);
        }
    }
    
    void DrawOrbitGizmo(Vector3 center, OrbitRig rig, string label)
    {
        Gizmos.color = rig.gizmoColor;
        
        Vector3 orbitCenter = center + Vector3.up * rig.height;
        
        // Dibujar círculo
        Vector3 prevPoint = orbitCenter + new Vector3(rig.radius, 0f, 0f);
        for (int i = 1; i <= gizmoSegments; i++)
        {
            float angle = (360f / gizmoSegments) * i * Mathf.Deg2Rad;
            Vector3 point = orbitCenter + new Vector3(Mathf.Cos(angle) * rig.radius, 0f, Mathf.Sin(angle) * rig.radius);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
        
        // Dibujar cruz en el centro del rig
        float crossSize = 0.2f;
        Gizmos.DrawLine(orbitCenter - Vector3.right * crossSize, orbitCenter + Vector3.right * crossSize);
        Gizmos.DrawLine(orbitCenter - Vector3.forward * crossSize, orbitCenter + Vector3.forward * crossSize);
        
        // Label
        #if UNITY_EDITOR
        UnityEditor.Handles.color = rig.gizmoColor;
        UnityEditor.Handles.Label(orbitCenter + Vector3.right * (rig.radius + 0.3f), label);
        #endif
    }
    
    void DrawOrbitConnections(Vector3 center)
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        
        // Dibujar 4 líneas verticales conectando las órbitas
        for (int i = 0; i < 4; i++)
        {
            float angle = (90f * i) * Mathf.Deg2Rad;
            
            Vector3 topPoint = center + new Vector3(
                Mathf.Cos(angle) * topRig.radius, 
                topRig.height, 
                Mathf.Sin(angle) * topRig.radius);
                
            Vector3 middlePoint = center + new Vector3(
                Mathf.Cos(angle) * middleRig.radius, 
                middleRig.height, 
                Mathf.Sin(angle) * middleRig.radius);
                
            Vector3 bottomPoint = center + new Vector3(
                Mathf.Cos(angle) * bottomRig.radius, 
                bottomRig.height, 
                Mathf.Sin(angle) * bottomRig.radius);
            
            Gizmos.DrawLine(topPoint, middlePoint);
            Gizmos.DrawLine(middlePoint, bottomPoint);
        }
    }
#endif
}
