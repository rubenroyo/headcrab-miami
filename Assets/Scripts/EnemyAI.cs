using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Sistema de IA para enemigos con visión cónica, patrullaje y persecución.
/// Requiere NavMeshAgent en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyController))]
public class EnemyAI : MonoBehaviour
{
    public enum AIState
    {
        Idle,           // Quieto esperando
        Wandering,      // Deambulando aleatoriamente
        Patrolling,     // Siguiendo una ruta definida
        Chasing,        // Persiguiendo al jugador (lo ve)
        Investigating,  // Yendo a la última posición conocida (no lo ve)
        Searching,      // Buscando al jugador en la zona (deambular temporal)
        Returning       // Volviendo a la patrulla/posición original
    }

    [Header("Visión")]
    [SerializeField] private float viewAngle = 60f;
    [SerializeField] private float viewDistance = 4f;
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private LayerMask playerMask;
    
    [Header("Debug Visión")]
    [SerializeField] private bool showVisionCone = true;
    [SerializeField] private Color visionConeColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private int visionConeSegments = 20;

    [Header("Comportamiento")]
    [SerializeField] private AIBehaviorType defaultBehavior = AIBehaviorType.Wander;
    [SerializeField] private float searchDuration = 5f; // Tiempo buscando en la zona antes de volver
    
    [Header("Deambular")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderInterval = 3f;
    [SerializeField] private float minWanderDistance = 2f;

    [Header("Patrulla")]
    [Tooltip("Arrastra aquí el objeto Route que contiene los waypoints como hijos")]
    [SerializeField] private Transform patrolRouteObject;
    [SerializeField] private string patrolRouteId = ""; // ID alternativo para buscar por nombre
    [HideInInspector]
    [SerializeField] private List<PatrolPoint> patrolRoute = new List<PatrolPoint>();
    [SerializeField] private bool loopPatrol = true;
    
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f; // Giro rápido
    [SerializeField] private float acceleration = 999f; // Aceleración instantánea
    [SerializeField] private float stoppingDistance = 0.3f;
    [SerializeField] private float pathUpdateInterval = 0.2f;

    // Estado actual
    public AIState CurrentState { get; private set; } = AIState.Idle;
    public bool CanSeePlayer { get; private set; }
    public Transform DetectedPlayer { get; private set; }

    // Componentes
    private NavMeshAgent navAgent;
    private EnemyController enemyController;
    
    // Visión
    private Transform playerTransform;
    private Vector3 lastKnownPlayerPosition;
    
    // Búsqueda
    private float searchTimer;
    private Vector3 searchOrigin; // Centro de búsqueda (última posición conocida)
    
    // Deambular
    private float nextWanderTime;
    private Vector3 wanderOrigin; // Posición inicial para volver
    
    // Patrulla
    private int currentPatrolIndex;
    private float patrolWaitTimer;
    private bool isWaitingAtPatrolPoint;
    private bool patrolForward = true; // Para rutas ping-pong
    
    // Pathfinding
    private float nextPathUpdateTime;

    public enum AIBehaviorType
    {
        Wander,
        Patrol,
        Stationary
    }

    [System.Serializable]
    public class PatrolPoint
    {
        public Transform point;
        public float waitTime = 0f;
        
        public PatrolPoint() { }
        public PatrolPoint(Transform p, float wait = 0f)
        {
            point = p;
            waitTime = wait;
        }
    }

    // Propiedades públicas para configuración en runtime
    public float ViewAngle { get => viewAngle; set => viewAngle = value; }
    public float ViewDistance { get => viewDistance; set => viewDistance = value; }
    public bool ShowVisionCone { get => showVisionCone; set => showVisionCone = value; }
    public List<PatrolPoint> PatrolRoute => patrolRoute;
    public Transform PatrolRouteObject { get => patrolRouteObject; set => patrolRouteObject = value; }
    public string PatrolRouteId { get => patrolRouteId; set => patrolRouteId = value; }

    void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        enemyController = GetComponent<EnemyController>();
        wanderOrigin = transform.position;
    }

    void Start()
    {
        ConfigureNavMeshAgent();
        FindPlayer();
        LoadPatrolRoute();
        InitializeBehavior();
    }

    /// <summary>
    /// Carga la ruta de patrulla desde el objeto referenciado o por ID.
    /// </summary>
    private void LoadPatrolRoute()
    {
        // Si no hay objeto asignado, buscar por ID
        if (patrolRouteObject == null && !string.IsNullOrEmpty(patrolRouteId))
        {
            GameObject routeObj = GameObject.Find(patrolRouteId);
            if (routeObj != null)
            {
                patrolRouteObject = routeObj.transform;
            }
        }

        // Cargar waypoints desde los hijos del objeto ruta
        if (patrolRouteObject != null && patrolRouteObject.childCount > 0)
        {
            patrolRoute.Clear();
            foreach (Transform child in patrolRouteObject)
            {
                patrolRoute.Add(new PatrolPoint(child, 0f));
            }
            Debug.Log($"{name}: Cargados {patrolRoute.Count} waypoints desde '{patrolRouteObject.name}'");
        }
    }

    void Update()
    {
        // No hacer nada si está poseído
        if (enemyController.IsPossessed)
        {
            navAgent.isStopped = true;
            return;
        }

        // DEBUG: Estado actual cada 0.5s
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[STATE] {name}: {CurrentState}, isStopped={navAgent.isStopped}, hasPath={navAgent.hasPath}, remainingDist={navAgent.remainingDistance:F2}");
        }

        // Actualizar detección de visión
        UpdateVision();
        
        // Rotación instantánea hacia el destino o jugador
        UpdateRotation();
        
        // Máquina de estados
        switch (CurrentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Wandering:
                UpdateWandering();
                break;
            case AIState.Patrolling:
                UpdatePatrolling();
                break;
            case AIState.Chasing:
                UpdateChasing();
                break;
            case AIState.Investigating:
                UpdateInvestigating();
                break;
            case AIState.Searching:
                UpdateSearching();
                break;
            case AIState.Returning:
                UpdateReturning();
                break;
        }
    }

    /// <summary>
    /// Rotación instantánea hacia el objetivo.
    /// SOLO mira al jugador real si CanSeePlayer es true.
    /// En otros casos, mira hacia la dirección de movimiento.
    /// </summary>
    private void UpdateRotation()
    {
        Vector3 lookTarget = Vector3.zero;
        bool shouldRotate = false;
        string rotationReason = "none";

        // SOLO mirar al jugador si ACTUALMENTE lo vemos
        if (CanSeePlayer && playerTransform != null)
        {
            lookTarget = playerTransform.position;
            shouldRotate = true;
            rotationReason = "PLAYER_VISIBLE";
        }
        // En cualquier otro caso, mirar hacia donde nos movemos
        else if (navAgent.hasPath && navAgent.velocity.sqrMagnitude > 0.01f)
        {
            lookTarget = transform.position + navAgent.velocity.normalized;
            shouldRotate = true;
            rotationReason = "MOVEMENT_DIR";
        }
        else
        {
            rotationReason = $"NO_ROTATION (hasPath={navAgent.hasPath}, vel={navAgent.velocity.sqrMagnitude:F3})";
        }

        // Log cada 0.5 segundos para no spammear
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[ROT] {name}: State={CurrentState}, CanSee={CanSeePlayer}, Reason={rotationReason}, Target={lookTarget}");
        }

        if (shouldRotate)
        {
            Vector3 direction = (lookTarget - transform.position);
            direction.y = 0f; // Solo rotación horizontal
            
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(direction.normalized);
            }
        }
    }

    private void ConfigureNavMeshAgent()
    {
        // Movimiento rígido sin aceleración/desaceleración
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = 0f; // Desactivar rotación automática del NavMesh
        navAgent.acceleration = acceleration; // Aceleración instantánea
        navAgent.stoppingDistance = stoppingDistance;
        navAgent.updateRotation = false; // Rotación manual
        navAgent.updatePosition = true;
        navAgent.autoBraking = false; // No desacelerar antes de llegar
        
        // Verificar que está sobre NavMesh
        if (!navAgent.isOnNavMesh)
        {
            // Intentar colocar sobre NavMesh
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                navAgent.Warp(hit.position);
                Debug.Log($"{name}: Reposicionado sobre NavMesh en {hit.position}");
            }
            else
            {
                Debug.LogWarning($"{name}: No se encontró NavMesh cercano. Asegúrate de que el enemigo esté sobre el NavMesh.");
            }
        }
    }

    private void FindPlayer()
    {
        // Buscar el jugador por tag o por componente
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            // Fallback: buscar PlayerController
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                playerTransform = pc.transform;
            }
        }
    }

    private void InitializeBehavior()
    {
        switch (defaultBehavior)
        {
            case AIBehaviorType.Wander:
                SetState(AIState.Wandering);
                break;
            case AIBehaviorType.Patrol:
                if (patrolRoute.Count > 0)
                    SetState(AIState.Patrolling);
                else
                    SetState(AIState.Wandering);
                break;
            case AIBehaviorType.Stationary:
                SetState(AIState.Idle);
                break;
        }
    }

    #region Vision System

    private void UpdateVision()
    {
        bool wasSeeing = CanSeePlayer;
        CanSeePlayer = false;
        DetectedPlayer = null;

        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        // 1. Verificar distancia
        Vector3 directionToPlayer = playerTransform.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        if (distanceToPlayer > viewDistance)
        {
            if (wasSeeing) Debug.Log($"[VISION] {name}: PERDIDO - Fuera de distancia ({distanceToPlayer:F1} > {viewDistance})");
            goto CheckLostPlayer;
        }

        // 2. Verificar ángulo (cono de visión)
        directionToPlayer.Normalize();
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);

        if (angleToPlayer > viewAngle / 2f)
        {
            if (wasSeeing) Debug.Log($"[VISION] {name}: PERDIDO - Fuera del cono ({angleToPlayer:F1}° > {viewAngle/2f}°)");
            goto CheckLostPlayer;
        }

        // 3. Verificar línea de visión (raycast para obstáculos)
        Vector3 eyePosition = transform.position + Vector3.up * 1f; // Altura de los ojos
        Vector3 playerEyePosition = playerTransform.position + Vector3.up * 0.5f;
        Vector3 dirToPlayerEyes = (playerEyePosition - eyePosition).normalized;

        if (Physics.Raycast(eyePosition, dirToPlayerEyes, out RaycastHit hit, viewDistance, obstructionMask | playerMask))
        {
            // Verificar si golpeamos al jugador o a un obstáculo
            if (hit.transform == playerTransform || hit.transform.CompareTag("Player"))
            {
                CanSeePlayer = true;
                DetectedPlayer = playerTransform;
                lastKnownPlayerPosition = playerTransform.position;
                
                if (!wasSeeing) Debug.Log($"[VISION] {name}: VISTO! lastKnown={lastKnownPlayerPosition}");
                
                // Si vemos al jugador y no estamos persiguiendo, cambiar estado
                if (CurrentState != AIState.Chasing)
                {
                    SetState(AIState.Chasing);
                }
            }
            else
            {
                if (wasSeeing) Debug.Log($"[VISION] {name}: PERDIDO - Obstruido por {hit.transform.name}");
            }
        }
        else
        {
            if (wasSeeing) Debug.Log($"[VISION] {name}: PERDIDO - Raycast no golpeó nada");
        }

        CheckLostPlayer:
        // Si perdimos de vista al jugador mientras lo perseguíamos
        if (!CanSeePlayer && CurrentState == AIState.Chasing)
        {
            Debug.Log($"[VISION] {name}: Cambiando a Investigating. lastKnown={lastKnownPlayerPosition}");
            SetState(AIState.Investigating);
        }
    }

    #endregion

    #region State Updates

    private void UpdateIdle()
    {
        navAgent.isStopped = true;
        // Puede girar lentamente buscando amenazas
    }

    private void UpdateWandering()
    {
        navAgent.isStopped = false;
        navAgent.speed = moveSpeed;

        // Buscar nuevo punto de deambulación
        if (Time.time >= nextWanderTime || HasReachedDestination())
        {
            Vector3 newDestination = GetRandomWanderPoint();
            if (newDestination != Vector3.zero)
            {
                navAgent.SetDestination(newDestination);
            }
            nextWanderTime = Time.time + wanderInterval;
        }
    }

    private void UpdatePatrolling()
    {
        navAgent.isStopped = false;
        navAgent.speed = moveSpeed;

        if (patrolRoute.Count == 0)
        {
            SetState(AIState.Wandering);
            return;
        }

        // Si estamos esperando en un punto
        if (isWaitingAtPatrolPoint)
        {
            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0f)
            {
                isWaitingAtPatrolPoint = false;
                MoveToNextPatrolPoint();
            }
            return;
        }

        // Verificar si llegamos al punto actual
        if (HasReachedDestination())
        {
            PatrolPoint currentPoint = patrolRoute[currentPatrolIndex];
            
            if (currentPoint.waitTime > 0f)
            {
                isWaitingAtPatrolPoint = true;
                patrolWaitTimer = currentPoint.waitTime;
                navAgent.isStopped = true;
            }
            else
            {
                MoveToNextPatrolPoint();
            }
        }
    }

    private void UpdateChasing()
    {
        navAgent.isStopped = false;
        navAgent.speed = chaseSpeed;

        // Actualizar destino periódicamente mientras vemos al jugador
        if (Time.time >= nextPathUpdateTime)
        {
            if (CanSeePlayer && playerTransform != null)
            {
                navAgent.SetDestination(playerTransform.position);
            }
            nextPathUpdateTime = Time.time + pathUpdateInterval;
        }
    }

    private void UpdateInvestigating()
    {
        navAgent.isStopped = false;
        navAgent.speed = chaseSpeed; // Ir rápido a investigar

        // Si volvemos a ver al jugador, perseguir
        if (CanSeePlayer)
        {
            SetState(AIState.Chasing);
            return;
        }

        // Asegurar que tenemos destino
        if (!navAgent.hasPath && !navAgent.pathPending)
        {
            Debug.Log($"[INVEST] {name}: Sin path! Estableciendo destino a {lastKnownPlayerPosition}");
            navAgent.SetDestination(lastKnownPlayerPosition);
        }

        // Log de debug
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[INVEST] {name}: Yendo a {lastKnownPlayerPosition}, remaining={navAgent.remainingDistance:F2}, velocity={navAgent.velocity.magnitude:F2}");
        }

        // Si llegamos a la última posición conocida, empezar a buscar
        if (HasReachedDestination())
        {
            Debug.Log($"[INVEST] {name}: Llegué a última posición. Iniciando búsqueda.");
            searchOrigin = lastKnownPlayerPosition;
            searchTimer = searchDuration;
            SetState(AIState.Searching);
        }
    }

    private void UpdateSearching()
    {
        navAgent.isStopped = false;
        navAgent.speed = moveSpeed;

        // Si vemos al jugador, perseguir inmediatamente
        if (CanSeePlayer)
        {
            SetState(AIState.Chasing);
            return;
        }

        // Decrementar timer
        searchTimer -= Time.deltaTime;

        // Log de debug
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[SEARCH] {name}: timer={searchTimer:F1}s, velocity={navAgent.velocity.magnitude:F2}, hasPath={navAgent.hasPath}");
        }

        // Si el tiempo de búsqueda terminó, volver al comportamiento original
        if (searchTimer <= 0f)
        {
            Debug.Log($"{name}: Búsqueda terminada. Volviendo a comportamiento original.");
            ReturnToPreviousBehavior();
            return;
        }

        // Verificar si necesita nuevo destino
        bool hasReached = HasReachedDestination();
        bool noPath = !navAgent.hasPath && !navAgent.pathPending;
        bool timeUp = Time.time >= nextWanderTime;
        
        if (hasReached || noPath || timeUp)
        {
            Vector3 searchPoint = GetRandomSearchPoint();
            if (searchPoint != Vector3.zero)
            {
                bool pathSet = navAgent.SetDestination(searchPoint);
                Debug.Log($"{name}: [Search] Nuevo destino: {searchPoint}, pathSet={pathSet}, remaining={navAgent.remainingDistance:F2}");
                nextWanderTime = Time.time + 2f;
            }
            else
            {
                // Fallback al centro de búsqueda
                navAgent.SetDestination(searchOrigin);
                Debug.Log($"{name}: [Search] Fallback a searchOrigin: {searchOrigin}");
                nextWanderTime = Time.time + 1f;
            }
        }
    }

    /// <summary>
    /// Obtiene un punto aleatorio cerca del área de búsqueda.
    /// </summary>
    private Vector3 GetRandomSearchPoint()
    {
        float searchRadius = 4f; // Radio de búsqueda
        
        for (int i = 0; i < 15; i++)
        {
            // Generar punto aleatorio en un círculo alrededor del origen de búsqueda
            Vector2 randomCircle = Random.insideUnitCircle * searchRadius;
            Vector3 randomDirection = new Vector3(randomCircle.x, 0, randomCircle.y);
            randomDirection += searchOrigin;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
            {
                // Aceptar si está a más de 1 unidad de distancia actual
                if (Vector3.Distance(transform.position, hit.position) >= 1f)
                {
                    return hit.position;
                }
            }
        }
        
        // Fallback: volver al origen de búsqueda
        if (NavMesh.SamplePosition(searchOrigin, out NavMeshHit fallbackHit, 2f, NavMesh.AllAreas))
        {
            return fallbackHit.position;
        }
        
        return Vector3.zero;
    }

    private void UpdateReturning()
    {
        navAgent.isStopped = false;
        navAgent.speed = moveSpeed;

        if (HasReachedDestination())
        {
            // Volver al comportamiento por defecto
            InitializeBehavior();
        }
    }

    #endregion

    #region Helper Methods

    private void SetState(AIState newState)
    {
        AIState previousState = CurrentState;
        CurrentState = newState;

        // Debug
        Debug.Log($"{name}: {previousState} -> {newState}");

        // Acciones al entrar en el nuevo estado
        switch (newState)
        {
            case AIState.Chasing:
                navAgent.isStopped = false;
                break;
            case AIState.Patrolling:
                if (patrolRoute.Count > 0 && patrolRoute[currentPatrolIndex].point != null)
                {
                    navAgent.SetDestination(patrolRoute[currentPatrolIndex].point.position);
                }
                break;
            case AIState.Investigating:
                navAgent.isStopped = false;
                navAgent.SetDestination(lastKnownPlayerPosition);
                Debug.Log($"{name}: Investigando última posición conocida: {lastKnownPlayerPosition}");
                break;
            case AIState.Searching:
                // Forzar búsqueda de punto inmediata
                nextWanderTime = 0f;
                navAgent.isStopped = false;
                // Establecer primer destino de búsqueda
                Vector3 firstSearchPoint = GetRandomSearchPoint();
                if (firstSearchPoint != Vector3.zero)
                {
                    navAgent.SetDestination(firstSearchPoint);
                }
                Debug.Log($"{name}: Buscando al jugador durante {searchTimer}s en zona {searchOrigin}");
                break;
            case AIState.Wandering:
                nextWanderTime = 0f; // Forzar búsqueda de punto inmediata
                navAgent.isStopped = false;
                break;
        }
    }

    private void ReturnToPreviousBehavior()
    {
        Debug.Log($"{name}: Perdió al jugador. Volviendo a comportamiento anterior.");
        
        switch (defaultBehavior)
        {
            case AIBehaviorType.Wander:
                SetState(AIState.Wandering);
                break;
            case AIBehaviorType.Patrol:
                SetState(AIState.Returning);
                // Ir al punto de patrulla donde lo dejamos
                if (patrolRoute.Count > 0 && patrolRoute[currentPatrolIndex].point != null)
                {
                    navAgent.SetDestination(patrolRoute[currentPatrolIndex].point.position);
                }
                break;
            case AIBehaviorType.Stationary:
                SetState(AIState.Returning);
                navAgent.SetDestination(wanderOrigin);
                break;
        }
    }

    private bool HasReachedDestination()
    {
        // No contar como llegado si está calculando ruta
        if (navAgent.pathPending) return false;
        
        // Si no tiene path, no ha llegado (no tiene a dónde ir)
        if (!navAgent.hasPath) return false;
        
        // Verificar distancia restante
        return navAgent.remainingDistance <= navAgent.stoppingDistance + 0.1f;
    }

    private Vector3 GetRandomWanderPoint()
    {
        for (int i = 0; i < 10; i++) // Intentar 10 veces
        {
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
            randomDirection += wanderOrigin;
            randomDirection.y = transform.position.y;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                // Verificar que no esté demasiado cerca
                if (Vector3.Distance(transform.position, hit.position) >= minWanderDistance)
                {
                    return hit.position;
                }
            }
        }
        return Vector3.zero;
    }

    private void MoveToNextPatrolPoint()
    {
        if (loopPatrol)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolRoute.Count;
        }
        else
        {
            // Ping-pong
            if (patrolForward)
            {
                currentPatrolIndex++;
                if (currentPatrolIndex >= patrolRoute.Count)
                {
                    currentPatrolIndex = patrolRoute.Count - 2;
                    patrolForward = false;
                }
            }
            else
            {
                currentPatrolIndex--;
                if (currentPatrolIndex < 0)
                {
                    currentPatrolIndex = 1;
                    patrolForward = true;
                }
            }
        }

        // Clamp por seguridad
        currentPatrolIndex = Mathf.Clamp(currentPatrolIndex, 0, patrolRoute.Count - 1);

        if (patrolRoute[currentPatrolIndex].point != null)
        {
            navAgent.SetDestination(patrolRoute[currentPatrolIndex].point.position);
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Añade un punto a la ruta de patrulla.
    /// </summary>
    public void AddPatrolPoint(Transform point, float waitTime = 0f)
    {
        patrolRoute.Add(new PatrolPoint(point, waitTime));
    }

    /// <summary>
    /// Limpia la ruta de patrulla.
    /// </summary>
    public void ClearPatrolRoute()
    {
        patrolRoute.Clear();
        currentPatrolIndex = 0;
    }

    /// <summary>
    /// Fuerza al enemigo a ir a una posición específica.
    /// </summary>
    public void GoToPosition(Vector3 position)
    {
        navAgent.SetDestination(position);
        SetState(AIState.Returning);
    }

    /// <summary>
    /// Alerta al enemigo de la presencia del jugador en una posición.
    /// </summary>
    public void AlertToPosition(Vector3 position)
    {
        lastKnownPlayerPosition = position;
        SetState(AIState.Chasing);
    }

    /// <summary>
    /// Activa/desactiva la visualización del cono de visión.
    /// </summary>
    public void ToggleVisionCone()
    {
        showVisionCone = !showVisionCone;
    }

    #endregion

    #region Gizmos - Visualización del cono de visión

    void OnDrawGizmos()
    {
        if (!showVisionCone) return;
        DrawVisionCone();
    }

    void OnDrawGizmosSelected()
    {
        // Siempre mostrar cuando está seleccionado
        DrawVisionCone();
        DrawPatrolRoute();
    }

    private void DrawVisionCone()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        
        // Color basado en si ve al jugador o no
        Color coneColor = CanSeePlayer ? Color.red : visionConeColor;
        Gizmos.color = coneColor;

        // Dibujar el arco del cono
        float halfAngle = viewAngle / 2f;
        Vector3 forward = transform.forward;
        
        // Calcular los bordes del cono
        Vector3 leftBoundary = Quaternion.Euler(0, -halfAngle, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, halfAngle, 0) * forward;

        // Dibujar líneas de los bordes
        Gizmos.DrawLine(origin, origin + leftBoundary * viewDistance);
        Gizmos.DrawLine(origin, origin + rightBoundary * viewDistance);

        // Dibujar arco
        Vector3 previousPoint = origin + leftBoundary * viewDistance;
        float angleStep = viewAngle / visionConeSegments;
        
        for (int i = 1; i <= visionConeSegments; i++)
        {
            float angle = -halfAngle + (angleStep * i);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
            Vector3 point = origin + direction * viewDistance;
            
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        // Dibujar área del cono con mesh (solo en modo juego o con Handles)
        #if UNITY_EDITOR
        DrawFilledCone(origin, forward, halfAngle, viewDistance, coneColor);
        #endif
    }

    #if UNITY_EDITOR
    private void DrawFilledCone(Vector3 origin, Vector3 forward, float halfAngle, float distance, Color color)
    {
        UnityEditor.Handles.color = new Color(color.r, color.g, color.b, 0.1f);
        
        // Dibujar el arco relleno
        Vector3 from = Quaternion.Euler(0, -halfAngle, 0) * forward;
        UnityEditor.Handles.DrawSolidArc(origin, Vector3.up, from, halfAngle * 2, distance);
    }
    #endif

    private void DrawPatrolRoute()
    {
        if (patrolRoute == null || patrolRoute.Count == 0) return;

        Gizmos.color = Color.cyan;
        
        for (int i = 0; i < patrolRoute.Count; i++)
        {
            if (patrolRoute[i].point == null) continue;

            // Dibujar esfera en el punto
            float sphereSize = patrolRoute[i].waitTime > 0 ? 0.5f : 0.3f;
            Gizmos.DrawWireSphere(patrolRoute[i].point.position, sphereSize);

            // Dibujar línea al siguiente punto
            int nextIndex = (i + 1) % patrolRoute.Count;
            if (nextIndex < patrolRoute.Count && patrolRoute[nextIndex].point != null)
            {
                if (loopPatrol || nextIndex > i)
                {
                    Gizmos.DrawLine(patrolRoute[i].point.position, patrolRoute[nextIndex].point.position);
                }
            }

            // Dibujar número del punto
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                patrolRoute[i].point.position + Vector3.up * 0.7f, 
                $"P{i}" + (patrolRoute[i].waitTime > 0 ? $" ({patrolRoute[i].waitTime}s)" : "")
            );
            #endif
        }
    }

    #endregion
}
