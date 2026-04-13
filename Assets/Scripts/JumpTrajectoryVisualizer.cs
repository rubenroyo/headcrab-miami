using UnityEngine;

/// <summary>
/// Visualiza la trayectoria parabólica del salto del jugador.
/// Dibuja la parábola tanto en el editor (Gizmos) como en el juego (LineRenderer).
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class JumpTrajectoryVisualizer : MonoBehaviour
{
    [Header("Trajectory Settings")]
    [Tooltip("Potencia del salto - controla la longitud total de la parábola")]
    [SerializeField] private float jumpPower = 15f;
    
    [Tooltip("Gravedad aplicada al salto")]
    [SerializeField] private float gravity = 20f;
    
    [Tooltip("Multiplicador de velocidad para recorrer la trayectoria (1 = tiempo físico real)")]
    [SerializeField] private float jumpSpeedMultiplier = 1f;
    
    [Tooltip("Tiempo máximo de simulación")]
    [SerializeField] private float maxSimulationTime = 3f;
    
    [Tooltip("Número de puntos en la trayectoria")]
    [SerializeField] private int trajectoryResolution = 50;
    
    [Tooltip("Altura desde donde sale la línea (relativa al jugador)")]
    [SerializeField] private float startHeightOffset = 0.5f;
    
    [Tooltip("Ángulo mínimo de lanzamiento en grados (evita saltos completamente horizontales)")]
    [SerializeField] private float minLaunchAngle = 15f;
    
    [Tooltip("Ángulo máximo de lanzamiento en grados")]
    [SerializeField] private float maxLaunchAngle = 75f;
    
    [Header("Visual Settings")]
    [SerializeField] private Color trajectoryColor = Color.green;
    [SerializeField] private float lineWidth = 0.05f;
    
    [Header("Collision")]
    [Tooltip("Radio de la esfera de colisión para detectar impactos")]
    [SerializeField] private float collisionRadius = 0.3f;
    
    [Tooltip("Capa de paredes para rebote")]
    [SerializeField] private LayerMask wallMask;
    
    [Tooltip("Capa del suelo para aterrizaje")]
    [SerializeField] private LayerMask groundMask;
    
    [Tooltip("Número máximo de rebotes en paredes")]
    [SerializeField] private int maxBounces = 3;
    
    [Tooltip("Factor de conservación de velocidad al rebotar (0-1)")]
    [SerializeField] private float bounceVelocityFactor = 0.7f;
    
    [Header("Landing Indicator")]
    [Tooltip("Radio del círculo de aterrizaje")]
    [SerializeField] private float landingCircleRadius = 0.5f;
    
    [Tooltip("Número de segmentos del círculo")]
    [SerializeField] private int circleSegments = 32;
    
    [Tooltip("Color del indicador de aterrizaje")]
    [SerializeField] private Color landingCircleColor = Color.yellow;
    
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private ThirdPersonOrbitCamera orbitCamera;
    
    [Tooltip("Punto de origen de la parábola (pivote del jugador). Si está vacío, usa player.position + startHeightOffset")]
    [SerializeField] private Transform playerPivot;
    
    // Componentes
    private LineRenderer lineRenderer;
    private LineRenderer landingCircleRenderer;
    
    // Estado
    private bool isActive = false;
    private Vector3 landingNormal;
    private Vector3[] trajectoryPoints;
    private Vector3 launchDirection;
    private Vector3 landingPoint;
    private bool hasValidLanding = false;
    private Vector3 calculatedVelocity; // Velocidad calculada para el salto actual
    private Vector3 trajectoryStartPosition; // Posición exacta de inicio de la trayectoria
    private float physicsFlightTime = 0f; // Tiempo de vuelo según la física de la simulación
    
    // Propiedades públicas
    public Vector3[] TrajectoryPoints => trajectoryPoints;
    public Vector3 StartPosition => trajectoryStartPosition;
    public Vector3 LaunchDirection => launchDirection;
    public Vector3 LandingPoint => landingPoint;
    public bool HasValidLanding => hasValidLanding;
    public float JumpPower => jumpPower;
    public float Gravity => gravity;
    public bool IsActive => isActive;
    
    /// <summary>
    /// Duración del salto en segundos (tiempo físico ajustado por el multiplicador de velocidad)
    /// </summary>
    public float FlightDuration => physicsFlightTime / Mathf.Max(jumpSpeedMultiplier, 0.1f);
    
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        SetupLineRenderer();
        SetupLandingCircle();
    }
    
    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>()?.transform;
            
        if (orbitCamera == null)
            orbitCamera = FindFirstObjectByType<ThirdPersonOrbitCamera>();
            
        // Inicializar array
        trajectoryPoints = new Vector3[trajectoryResolution];
        
        // Debug
        Debug.Log($"[JumpTrajectory] Start - Player: {(player != null ? player.name : "NULL")}, OrbitCamera: {(orbitCamera != null ? orbitCamera.name : "NULL")}, LineRenderer: {(lineRenderer != null ? "OK" : "NULL")}");
        
        // Ocultar al inicio
        SetActive(false);
    }
    
    void SetupLineRenderer()
    {
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = trajectoryColor;
        lineRenderer.endColor = trajectoryColor;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
    }
    
    void SetupLandingCircle()
    {
        // Crear GameObject hijo para el círculo de aterrizaje
        GameObject circleObj = new GameObject("LandingCircle");
        circleObj.transform.SetParent(transform);
        circleObj.transform.localPosition = Vector3.zero;
        
        landingCircleRenderer = circleObj.AddComponent<LineRenderer>();
        landingCircleRenderer.startWidth = lineWidth * 1.5f;
        landingCircleRenderer.endWidth = lineWidth * 1.5f;
        landingCircleRenderer.material = new Material(Shader.Find("Sprites/Default"));
        landingCircleRenderer.startColor = landingCircleColor;
        landingCircleRenderer.endColor = landingCircleColor;
        landingCircleRenderer.positionCount = 0;
        landingCircleRenderer.useWorldSpace = true;
        landingCircleRenderer.loop = true; // Cerrar el círculo
        landingCircleRenderer.enabled = false;
    }
    
    void Update()
    {
        if (!isActive || player == null)
            return;
            
        CalculateTrajectory();
        UpdateLineRenderer();
        UpdateLandingCircle();
    }
    
    /// <summary>
    /// Calcula la trayectoria parabólica desde la posición del jugador.
    /// - jumpPower controla la "longitud" total de la parábola
    /// - La dirección de la cámara determina cómo se distribuye entre altura y distancia
    /// - Si apunta al suelo: el destino es ese punto (si está en alcance) o el máximo alcance
    /// </summary>
    void CalculateTrajectory()
    {
        // Posición inicial: usar playerPivot si está asignado, si no usar player + offset
        Vector3 startPos;
        if (playerPivot != null)
        {
            startPos = playerPivot.position;
        }
        else
        {
            startPos = player.position + Vector3.up * startHeightOffset;
        }
        
        // Guardar la posición de inicio para que el PlayerController la use
        trajectoryStartPosition = startPos;
        
        // Obtener dirección de la cámara
        Vector3 cameraForward = orbitCamera != null ? orbitCamera.transform.forward : player.forward;
        Vector3 cameraPosition = orbitCamera != null ? orbitCamera.transform.position : player.position + Vector3.up * 2f;
        
        // Dirección horizontal normalizada
        launchDirection = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;
        if (launchDirection.magnitude < 0.01f)
            launchDirection = player.forward;
        
        // Calcular el pitch de la cámara (negativo = mirando abajo)
        float cameraPitch = Mathf.Asin(Mathf.Clamp(cameraForward.y, -1f, 1f)) * Mathf.Rad2Deg;
        
        Vector3 velocity;
        
        // Si la cámara apunta hacia abajo, intentar alcanzar el punto del suelo
        if (cameraPitch < 0f)
        {
            velocity = CalculateVelocityForGroundTarget(startPos, cameraPosition, cameraForward);
        }
        else
        {
            // Apuntando hacia arriba o horizontal
            // Mapear el pitch de la cámara al ángulo de lanzamiento
            // Pitch 0° (horizontal) → ángulo mínimo (salto largo y bajo)
            // Pitch 90° (arriba) → ángulo máximo (salto alto y corto)
            float t = cameraPitch / 90f; // 0 a 1
            float launchAngle = Mathf.Lerp(minLaunchAngle, maxLaunchAngle, t);
            
            velocity = CalculateVelocityFromAngle(launchAngle);
        }
        
        calculatedVelocity = velocity;
        
        // Simular la trayectoria
        SimulateTrajectory(startPos, velocity);
    }
    
    /// <summary>
    /// Calcula la velocidad para alcanzar un punto del suelo donde apunta la cámara
    /// </summary>
    Vector3 CalculateVelocityForGroundTarget(Vector3 startPos, Vector3 cameraPos, Vector3 cameraDir)
    {
        // Hacer raycast desde la cámara hacia donde apunta
        if (Physics.Raycast(cameraPos, cameraDir, out RaycastHit hit, 100f, groundMask))
        {
            Vector3 targetPoint = hit.point;
            
            // Calcular distancia horizontal y diferencia de altura
            Vector3 toTarget = targetPoint - startPos;
            float horizontalDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
            float heightDiff = toTarget.y; // Negativo si el objetivo está más abajo
            
            // Calcular el alcance máximo con jumpPower a 45°
            // R_max = v² / g (alcance máximo teórico en terreno plano)
            float maxRange = (jumpPower * jumpPower) / gravity;
            
            // Verificar si el punto está en alcance
            // Para simplificar, comparamos distancia horizontal con alcance máximo
            if (horizontalDist <= maxRange * 0.95f) // 0.95 para dar margen
            {
                // El punto está en alcance - calcular velocidad para llegar exactamente ahí
                return CalculateVelocityToReachPoint(horizontalDist, heightDiff);
            }
        }
        
        // El punto está fuera de alcance o no hay suelo
        // Usar el alcance máximo en la dirección horizontal de la cámara
        // Ángulo óptimo para máximo alcance = 45°
        float optimalAngle = 45f;
        
        // Si hay diferencia de altura (bajando), ajustar ángulo para mayor alcance
        return CalculateVelocityFromAngle(optimalAngle);
    }
    
    /// <summary>
    /// Calcula la velocidad necesaria para alcanzar un punto específico
    /// </summary>
    Vector3 CalculateVelocityToReachPoint(float horizontalDist, float heightDiff)
    {
        // Usamos la fórmula de proyectil para calcular el ángulo necesario
        // Para alcanzar (d, h) con velocidad v:
        // h = d*tan(θ) - (g*d²)/(2*v²*cos²(θ))
        
        // Hay dos soluciones (ángulo alto y bajo). Preferimos el más bajo para mayor control.
        // Simplificación: calcular ángulo que maximiza la probabilidad de alcanzar el punto
        
        float v2 = jumpPower * jumpPower;
        float g = gravity;
        float d = horizontalDist;
        float h = heightDiff;
        
        // Discriminante para verificar si es alcanzable
        float discriminant = v2 * v2 - g * (g * d * d + 2f * h * v2);
        
        if (discriminant < 0f)
        {
            // No alcanzable con esta velocidad, usar ángulo de 45° (máximo alcance)
            return CalculateVelocityFromAngle(45f);
        }
        
        // Calcular los dos ángulos posibles
        float sqrtDisc = Mathf.Sqrt(discriminant);
        float angle1 = Mathf.Atan2(v2 + sqrtDisc, g * d) * Mathf.Rad2Deg;
        float angle2 = Mathf.Atan2(v2 - sqrtDisc, g * d) * Mathf.Rad2Deg;
        
        // Elegir el ángulo más bajo (trayectoria más directa) pero dentro de límites
        float chosenAngle = Mathf.Min(angle1, angle2);
        chosenAngle = Mathf.Clamp(chosenAngle, minLaunchAngle, maxLaunchAngle);
        
        return CalculateVelocityFromAngle(chosenAngle);
    }
    
    /// <summary>
    /// Calcula el vector de velocidad dado un ángulo de lanzamiento
    /// </summary>
    Vector3 CalculateVelocityFromAngle(float angleDegrees)
    {
        float angleRad = angleDegrees * Mathf.Deg2Rad;
        float vx = jumpPower * Mathf.Cos(angleRad);
        float vy = jumpPower * Mathf.Sin(angleRad);
        
        return launchDirection * vx + Vector3.up * vy;
    }
    
    /// <summary>
    /// Simula la trayectoria paso a paso con colisiones
    /// </summary>
    void SimulateTrajectory(Vector3 startPos, Vector3 velocity)
    {
        float timeStep = maxSimulationTime / trajectoryResolution;
        hasValidLanding = false;
        landingNormal = Vector3.up;
        int bounceCount = 0;
        
        System.Collections.Generic.List<Vector3> pointsList = new System.Collections.Generic.List<Vector3>();
        
        Vector3 currentPos = startPos;
        float currentTime = 0f;
        
        pointsList.Add(currentPos);
        
        while (currentTime < maxSimulationTime && pointsList.Count < trajectoryResolution * 2)
        {
            float dt = timeStep;
            if (currentTime + dt > maxSimulationTime)
                dt = maxSimulationTime - currentTime;
            
            // Calcular siguiente posición
            Vector3 nextPos = currentPos + velocity * dt + 0.5f * gravity * Vector3.down * dt * dt;
            
            // Actualizar velocidad
            velocity += gravity * Vector3.down * dt;
            
            Vector3 direction = nextPos - currentPos;
            float distance = direction.magnitude;
            
            if (distance > 0.001f)
            {
                // Verificar colisión con suelo
                if (Physics.SphereCast(currentPos, collisionRadius * 0.5f, direction.normalized, out RaycastHit groundHit, distance, groundMask))
                {
                    landingPoint = groundHit.point;
                    landingNormal = groundHit.normal;
                    pointsList.Add(landingPoint);
                    hasValidLanding = true;
                    break;
                }
                
                // Verificar colisión con paredes
                if (bounceCount < maxBounces && Physics.SphereCast(currentPos, collisionRadius * 0.5f, direction.normalized, out RaycastHit wallHit, distance, wallMask))
                {
                    Vector3 hitPoint = wallHit.point + wallHit.normal * collisionRadius * 0.5f;
                    pointsList.Add(hitPoint);
                    
                    // Reflejar velocidad horizontal
                    Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);
                    Vector3 reflectedHorizontal = Vector3.Reflect(horizontalVel, wallHit.normal) * bounceVelocityFactor;
                    velocity = new Vector3(reflectedHorizontal.x, velocity.y, reflectedHorizontal.z);
                    
                    currentPos = hitPoint;
                    bounceCount++;
                    currentTime += dt * (wallHit.distance / distance);
                    continue;
                }
            }
            
            if (nextPos.y < -10f)
                break;
            
            currentPos = nextPos;
            pointsList.Add(currentPos);
            currentTime += dt;
        }
        
        // Guardar el tiempo de vuelo físico
        physicsFlightTime = currentTime;
        trajectoryPoints = pointsList.ToArray();
    }
    
    /// <summary>
    /// Actualiza el círculo indicador de aterrizaje
    /// </summary>
    void UpdateLandingCircle()
    {
        if (!hasValidLanding || landingCircleRenderer == null)
        {
            if (landingCircleRenderer != null)
            {
                landingCircleRenderer.enabled = false;
                landingCircleRenderer.positionCount = 0;
            }
            return;
        }
        
        landingCircleRenderer.enabled = true;
        landingCircleRenderer.positionCount = circleSegments;
        
        // Calcular orientación del círculo basada en la normal del suelo
        Vector3 right = Vector3.Cross(landingNormal, Vector3.forward).normalized;
        if (right.magnitude < 0.01f)
            right = Vector3.Cross(landingNormal, Vector3.right).normalized;
        Vector3 forward = Vector3.Cross(right, landingNormal).normalized;
        
        // Crear puntos del círculo
        Vector3[] circlePoints = new Vector3[circleSegments];
        float angleStep = 360f / circleSegments;
        
        // Elevar ligeramente el círculo para evitar z-fighting con el suelo
        Vector3 circleCenter = landingPoint + landingNormal * 0.02f;
        
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * landingCircleRadius;
            circlePoints[i] = circleCenter + offset;
        }
        
        landingCircleRenderer.SetPositions(circlePoints);
        
        // Actualizar color del círculo
        landingCircleRenderer.startColor = landingCircleColor;
        landingCircleRenderer.endColor = landingCircleColor;
    }
    
    /// <summary>
    /// Actualiza el LineRenderer con los puntos calculados
    /// </summary>
    void UpdateLineRenderer()
    {
        if (trajectoryPoints == null || trajectoryPoints.Length == 0)
        {
            lineRenderer.positionCount = 0;
            Debug.LogWarning("[JumpTrajectory] No trajectory points!");
            return;
        }
        
        lineRenderer.positionCount = trajectoryPoints.Length;
        lineRenderer.SetPositions(trajectoryPoints);
        
        // Cambiar color según si hay aterrizaje válido
        Color color = hasValidLanding ? trajectoryColor : Color.red;
        lineRenderer.startColor = color;
        lineRenderer.endColor = new Color(color.r, color.g, color.b, 0.3f);
        
        // Debug cada segundo aproximadamente
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[JumpTrajectory] Points: {trajectoryPoints.Length}, HasLanding: {hasValidLanding}, Start: {trajectoryPoints[0]}, LineRenderer enabled: {lineRenderer.enabled}");
        }
    }
    
    /// <summary>
    /// Activa o desactiva la visualización de la trayectoria
    /// </summary>
    public void SetActive(bool active)
    {
        Debug.Log($"[JumpTrajectory] SetActive({active}) - Player: {(player != null ? player.name : "NULL")}, LineRenderer: {(lineRenderer != null ? "OK" : "NULL")}");
        
        isActive = active;
        
        if (lineRenderer != null)
        {
            lineRenderer.enabled = active;
            
            if (!active)
            {
                lineRenderer.positionCount = 0;
            }
        }
        else
        {
            Debug.LogError("[JumpTrajectory] LineRenderer is NULL!");
        }
        
        // También controlar el círculo de aterrizaje
        if (landingCircleRenderer != null)
        {
            if (!active)
            {
                landingCircleRenderer.enabled = false;
                landingCircleRenderer.positionCount = 0;
            }
        }
    }
    
    /// <summary>
    /// Establece la potencia del salto
    /// </summary>
    public void SetJumpPower(float power)
    {
        jumpPower = power;
    }
    
    /// <summary>
    /// Obtiene la velocidad inicial calculada para el salto actual
    /// </summary>
    public Vector3 GetLaunchVelocity()
    {
        return calculatedVelocity;
    }
    
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!isActive || trajectoryPoints == null || trajectoryPoints.Length < 2)
            return;
        
        // Dibujar la trayectoria con Gizmos
        Gizmos.color = trajectoryColor;
        for (int i = 0; i < trajectoryPoints.Length - 1; i++)
        {
            Gizmos.DrawLine(trajectoryPoints[i], trajectoryPoints[i + 1]);
        }
        
        // Dibujar punto de aterrizaje con círculo
        if (hasValidLanding)
        {
            Gizmos.color = landingCircleColor;
            
            // Dibujar círculo de aterrizaje en gizmos
            Vector3 right = Vector3.Cross(landingNormal, Vector3.forward).normalized;
            if (right.magnitude < 0.01f)
                right = Vector3.Cross(landingNormal, Vector3.right).normalized;
            Vector3 forward = Vector3.Cross(right, landingNormal).normalized;
            
            Vector3 prevPoint = landingPoint + right * landingCircleRadius;
            for (int i = 1; i <= 32; i++)
            {
                float angle = (i / 32f) * Mathf.PI * 2f;
                Vector3 offset = (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * landingCircleRadius;
                Vector3 point = landingPoint + offset;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
            
            // Centro del círculo
            Gizmos.DrawWireSphere(landingPoint, 0.1f);
        }
        
        // Dibujar esferas en los puntos de la trayectoria
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        foreach (var point in trajectoryPoints)
        {
            Gizmos.DrawWireSphere(point, 0.05f);
        }
    }
#endif
}
