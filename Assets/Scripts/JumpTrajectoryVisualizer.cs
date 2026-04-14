using UnityEngine;

/// <summary>
/// Visualiza la trayectoria parabólica del salto del jugador.
/// 
/// NUEVO SISTEMA: Longitud de arco fija
/// - El ápice de la parábola está en un rayo desde la cámara hacia el apexTarget
/// - La longitud total del arco es fija (arcLength)
/// - Solo existe UNA parábola que cumple ambas condiciones
/// 
/// Rebotes en paredes: conservan la distancia total (sin pérdida de velocidad)
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class JumpTrajectoryVisualizer : MonoBehaviour
{
    [Header("Arc Length System")]
    [Tooltip("Longitud total del arco de la parábola (en unidades)")]
    [SerializeField] private float arcLength = 10f;
    
    [Tooltip("Gravedad aplicada al salto")]
    [SerializeField] private float gravity = 20f;
    
    [Tooltip("Transform que define la dirección del ápice (colocado ligeramente encima del jugador)")]
    [SerializeField] private Transform apexTarget;
    
    [Tooltip("Multiplicador de velocidad para recorrer la trayectoria")]
    [SerializeField] private float jumpSpeedMultiplier = 1f;
    
    [Header("Trajectory Settings")]
    [Tooltip("Número de puntos en la trayectoria")]
    [SerializeField] private int trajectoryResolution = 50;
    
    [Tooltip("Altura desde donde sale la línea (relativa al jugador)")]
    [SerializeField] private float startHeightOffset = 0.5f;
    
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
    
    [Header("Landing Indicator")]
    [Tooltip("Radio del círculo de aterrizaje")]
    [SerializeField] private float landingCircleRadius = 0.5f;
    
    [Tooltip("Número de segmentos del círculo")]
    [SerializeField] private int circleSegments = 32;
    
    [Tooltip("Color del indicador de aterrizaje")]
    [SerializeField] private Color landingCircleColor = Color.yellow;
    
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera mainCamera;
    
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
    private Vector3 calculatedVelocity;
    private Vector3 trajectoryStartPosition;
    private float physicsFlightTime = 0f;
    private Vector3 apexPoint; // Punto del ápice calculado
    
    // Propiedades públicas
    public Vector3[] TrajectoryPoints => trajectoryPoints;
    public Vector3 StartPosition => trajectoryStartPosition;
    public Vector3 LaunchDirection => launchDirection;
    public Vector3 LandingPoint => landingPoint;
    public bool HasValidLanding => hasValidLanding;
    public float ArcLength => arcLength;
    public float Gravity => gravity;
    public bool IsActive => isActive;
    public Vector3 ApexPoint => apexPoint;
    
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
            
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        // Inicializar array
        trajectoryPoints = new Vector3[trajectoryResolution];
        
        Debug.Log($"[JumpTrajectory] Start - Player: {(player != null ? player.name : "NULL")}, Camera: {(mainCamera != null ? "OK" : "NULL")}, ApexTarget: {(apexTarget != null ? apexTarget.name : "NULL")}");
        
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
    /// Calcula la trayectoria usando el nuevo sistema de longitud de arco fija.
    /// 
    /// 1. Construye el rayo del ápice: desde la cámara hacia el apexTarget
    /// 2. Usa ParabolaArcSolver para encontrar el ápice único que produce la longitud de arco deseada
    /// 3. Aplica rebotes en paredes SIN pérdida de velocidad
    /// </summary>
    void CalculateTrajectory()
    {
        // Posición inicial
        Vector3 startPos = playerPivot != null ? playerPivot.position : player.position + Vector3.up * startHeightOffset;
        trajectoryStartPosition = startPos;
        
        // Construir el rayo del ápice
        Vector3 cameraPos = mainCamera != null ? mainCamera.transform.position : player.position + Vector3.up * 2f + Vector3.back * 3f;
        
        Vector3 apexRayDirection;
        if (apexTarget != null)
        {
            // Rayo desde la cámara hacia el apexTarget
            apexRayDirection = (apexTarget.position - cameraPos).normalized;
        }
        else
        {
            // Fallback: usar la dirección de la cámara con inclinación hacia arriba
            Vector3 cameraForward = mainCamera != null ? mainCamera.transform.forward : player.forward;
            Vector3 horizontalDir = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            // Ángulo de 45° hacia arriba desde la horizontal
            apexRayDirection = (horizontalDir + Vector3.up).normalized;
        }
        
        // Dirección horizontal del salto
        launchDirection = new Vector3(apexRayDirection.x, 0, apexRayDirection.z).normalized;
        if (launchDirection.magnitude < 0.01f)
            launchDirection = player.forward;
        
        // Usar el solver para encontrar la parábola
        var result = ParabolaArcSolver.SolveParabola(
            startPos,
            cameraPos,
            apexRayDirection,
            arcLength,
            gravity,
            trajectoryResolution,
            groundMask
        );
        
        if (result.IsValid)
        {
            apexPoint = result.Apex;
            calculatedVelocity = result.InitialVelocity;
            physicsFlightTime = result.FlightTime;
            
            // Ahora simular con rebotes (sin pérdida de velocidad)
            SimulateTrajectoryWithBounces(startPos, result.InitialVelocity);
        }
        else
        {
            // Fallback: parábola simple si el solver falla
            Debug.LogWarning("[JumpTrajectory] Solver failed, using fallback");
            SimulateTrajectoryFallback(startPos);
        }
    }
    
    /// <summary>
    /// Simula la trayectoria con rebotes en paredes SIN pérdida de velocidad.
    /// La distancia total del arco se conserva incluso después de rebotar.
    /// </summary>
    void SimulateTrajectoryWithBounces(Vector3 startPos, Vector3 velocity)
    {
        hasValidLanding = false;
        landingNormal = Vector3.up;
        int bounceCount = 0;
        
        var pointsList = new System.Collections.Generic.List<Vector3>();
        
        Vector3 currentPos = startPos;
        Vector3 currentVelocity = velocity;
        float remainingArcLength = arcLength;
        float accumulatedLength = 0f;
        
        pointsList.Add(currentPos);
        
        // Calcular tiempo estimado de vuelo
        float estimatedFlightTime = physicsFlightTime > 0 ? physicsFlightTime * 2f : 3f;
        float dt = estimatedFlightTime / trajectoryResolution;
        int maxIterations = trajectoryResolution * 3;
        
        for (int i = 0; i < maxIterations && remainingArcLength > 0.01f; i++)
        {
            // Siguiente posición
            Vector3 nextPos = currentPos + currentVelocity * dt - 0.5f * gravity * Vector3.up * dt * dt;
            Vector3 nextVelocity = currentVelocity - gravity * Vector3.up * dt;
            
            Vector3 direction = nextPos - currentPos;
            float segmentLength = direction.magnitude;
            
            if (segmentLength < 0.001f)
            {
                currentVelocity = nextVelocity;
                continue;
            }
            
            // Verificar colisión con suelo
            if (Physics.SphereCast(currentPos, collisionRadius * 0.5f, direction.normalized, out RaycastHit groundHit, segmentLength, groundMask))
            {
                landingPoint = groundHit.point;
                landingNormal = groundHit.normal;
                pointsList.Add(landingPoint);
                hasValidLanding = true;
                break;
            }
            
            // Verificar colisión con paredes
            if (bounceCount < maxBounces && Physics.SphereCast(currentPos, collisionRadius * 0.5f, direction.normalized, out RaycastHit wallHit, segmentLength, wallMask))
            {
                Vector3 hitPoint = wallHit.point + wallHit.normal * collisionRadius * 0.6f;
                float hitSegmentLength = Vector3.Distance(currentPos, hitPoint);
                
                accumulatedLength += hitSegmentLength;
                remainingArcLength -= hitSegmentLength;
                pointsList.Add(hitPoint);
                
                // REBOTE SIN PÉRDIDA DE VELOCIDAD
                // Reflejar velocidad horizontal manteniendo la magnitud
                Vector3 horizontalVel = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
                Vector3 reflectedHorizontal = Vector3.Reflect(horizontalVel, wallHit.normal);
                // NO multiplicamos por bounceVelocityFactor - conservamos toda la velocidad
                currentVelocity = new Vector3(reflectedHorizontal.x, currentVelocity.y, reflectedHorizontal.z);
                
                currentPos = hitPoint;
                bounceCount++;
                continue;
            }
            
            // Límite de altura
            if (nextPos.y < startPos.y - 50f)
                break;
            
            accumulatedLength += segmentLength;
            remainingArcLength -= segmentLength;
            
            currentPos = nextPos;
            currentVelocity = nextVelocity;
            pointsList.Add(currentPos);
        }
        
        // Si no encontramos suelo pero consumimos el arco, marcar el último punto como aterrizaje
        if (!hasValidLanding && pointsList.Count > 1)
        {
            landingPoint = pointsList[pointsList.Count - 1];
            // Hacer raycast hacia abajo para encontrar el suelo real
            if (Physics.Raycast(landingPoint + Vector3.up * 2f, Vector3.down, out RaycastHit floorHit, 10f, groundMask))
            {
                landingPoint = floorHit.point;
                pointsList[pointsList.Count - 1] = landingPoint;
                landingNormal = floorHit.normal;
                hasValidLanding = true;
            }
        }
        
        trajectoryPoints = pointsList.ToArray();
        physicsFlightTime = trajectoryPoints.Length * dt;
    }
    
    /// <summary>
    /// Fallback simple si el solver falla
    /// </summary>
    void SimulateTrajectoryFallback(Vector3 startPos)
    {
        // Calcular velocidad simple basada en la longitud del arco
        float estimatedSpeed = arcLength / 2f; // Aproximación muy simple
        float angle = 45f * Mathf.Deg2Rad;
        
        Vector3 velocity = launchDirection * estimatedSpeed * Mathf.Cos(angle) + Vector3.up * estimatedSpeed * Mathf.Sin(angle);
        calculatedVelocity = velocity;
        
        SimulateTrajectoryWithBounces(startPos, velocity);
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
    /// Establece la longitud del arco de la parábola
    /// </summary>
    public void SetArcLength(float length)
    {
        arcLength = Mathf.Max(1f, length);
    }
    
    /// <summary>
    /// Obtiene la velocidad inicial calculada para el salto actual
    /// </summary>
    public Vector3 GetLaunchVelocity()
    {
        return calculatedVelocity;
    }
    
    /// <summary>
    /// Asigna el transform del apex target en runtime
    /// </summary>
    public void SetApexTarget(Transform target)
    {
        apexTarget = target;
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
        
        // Dibujar el ápice de la parábola
        if (apexPoint != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(apexPoint, 0.2f);
            
            // Línea del origen al ápice
            if (trajectoryPoints.Length > 0)
            {
                Gizmos.DrawLine(trajectoryPoints[0], apexPoint);
            }
        }
        
        // Dibujar rayo del ápice si el target existe
        if (apexTarget != null && mainCamera != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 rayStart = mainCamera.transform.position;
            Vector3 rayDir = (apexTarget.position - rayStart).normalized;
            Gizmos.DrawRay(rayStart, rayDir * 20f);
            Gizmos.DrawWireSphere(apexTarget.position, 0.15f);
        }
        
        // Dibujar punto de aterrizaje con círculo
        if (hasValidLanding)
        {
            Gizmos.color = landingCircleColor;
            
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
            
            Gizmos.DrawWireSphere(landingPoint, 0.1f);
        }
        
        // Dibujar esferas pequeñas en cada punto de la trayectoria
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        foreach (var point in trajectoryPoints)
        {
            Gizmos.DrawWireSphere(point, 0.03f);
        }
    }
#endif
}
