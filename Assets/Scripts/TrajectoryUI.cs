using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Sistema de trayectoria basado en UI con puntos que crecen desde el jugador hacia el ratón.
/// Los puntos se dividen en 3 tamaños según la distancia al jugador.
/// </summary>
public class TrajectoryUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private RectTransform container;
    [SerializeField] private Canvas canvas;

    [Header("Sprites de Trayectoria")]
    [SerializeField] private Sprite smallDotSprite;
    [SerializeField] private Sprite mediumDotSprite;
    [SerializeField] private Sprite largeDotSprite;

    [Header("Configuración de Trayectoria")]
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float growSpeed = 15f;
    [SerializeField] private float dotSpacing = 0.5f;

    [Header("Rebotes")]
    [SerializeField] private int maxBounces = 3;
    [SerializeField] private LayerMask wallLayerMask;

    // Pool de puntos de UI
    private readonly List<Image> dotPool = new List<Image>();
    private int activeDots = 0;

    // Estado de la trayectoria
    private float currentGrowDistance = 0f;
    private Vector3[] cachedTrajectoryPoints;
    private float totalTrajectoryDistance = 0f;
    private bool isActive = false;

    // Referencias cacheadas
    private PlayerController playerController;
    private SettingsController settingsController;
    private Camera mainCamera;

    void Start()
    {
        if (player != null)
            playerController = player.GetComponent<PlayerController>();

        settingsController = SettingsController.Instance;

        if (settingsController != null && settingsController.MainCamera != null)
            mainCamera = settingsController.MainCamera;
        else
            mainCamera = Camera.main;

        // Validar referencias antes de crear pool
        if (dotPrefab == null)
        {
           //Debug.LogError("TrajectoryUI: dotPrefab no está asignado!");
            return;
        }
        if (container == null)
        {
            //Debug.LogError("TrajectoryUI: container no está asignado!");
            return;
        }

        // Pre-crear pool de puntos
        EnsurePoolSize(50);
    }

    void Update()
    {
        // No mostrar si el jugador está saltando
        if (playerController != null && playerController.CurrentState == PlayerState.Jumping)
        {
            HideAllDots();
            return;
        }

        if (Input.GetMouseButton(1)) // Clic derecho mantenido
        {
            if (!isActive)
            {
                // Empezar a mostrar trayectoria
                isActive = true;
                currentGrowDistance = 0f;
                //Debug.Log("TrajectoryUI: Activada");
            }

            UpdateTrajectory();
        }
        else
        {
            if (isActive)
            {
                isActive = false;
                currentGrowDistance = 0f;
                HideAllDots();
                //Debug.Log("TrajectoryUI: Desactivada");
            }
        }
    }

    void UpdateTrajectory()
    {
        // Obtener posición del ratón en el mundo
        Vector3 mouseWorld = player.position;
        if (settingsController != null)
            mouseWorld = settingsController.GetMouseWorldPosition(mainCamera, Input.mousePosition);

        // Calcular dirección y distancia al ratón
        Vector3 dir = mouseWorld - player.position;
        dir.y = 0f;

        float distanceToMouse = dir.magnitude;
        float targetDistance = Mathf.Min(distanceToMouse, maxDistance);

        // Limitar dirección a maxDistance
        if (dir.magnitude > maxDistance)
            dir = dir.normalized * maxDistance;

        // Calcular puntos de trayectoria con rebotes
        cachedTrajectoryPoints = CalculateBouncePoints(player.position, dir, targetDistance);
        totalTrajectoryDistance = CalculateTotalDistance(cachedTrajectoryPoints);

        // Crecer la distancia visible con el tiempo (usando unscaledDeltaTime porque el tiempo está ralentizado)
        currentGrowDistance += growSpeed * Time.unscaledDeltaTime;
        currentGrowDistance = Mathf.Min(currentGrowDistance, totalTrajectoryDistance);

        // Renderizar los puntos
        RenderDots();
    }

    void RenderDots()
    {
        HideAllDots();

        if (cachedTrajectoryPoints == null || cachedTrajectoryPoints.Length < 2)
        {
            //Debug.Log("TrajectoryUI: No hay puntos de trayectoria");
            return;
        }

        // Calcular cuántos puntos necesitamos
        int numDots = Mathf.FloorToInt(currentGrowDistance / dotSpacing);
        
        //Debug.Log($"TrajectoryUI: growDist={currentGrowDistance:F2}, numDots={numDots}, totalDist={totalTrajectoryDistance:F2}");
        
        if (numDots <= 0)
            return;
        
        // Verificar que hay puntos en el pool
        if (dotPool.Count == 0)
        {
            //Debug.LogWarning("TrajectoryUI: No hay puntos en el pool. Verifica que el prefab tenga componente Image.");
            return;
        }
            
        EnsurePoolSize(numDots);

        activeDots = 0;

        for (int i = 0; i < numDots; i++)
        {
            float distance = (i + 1) * dotSpacing;
            if (distance > currentGrowDistance)
                break;

            // Obtener posición a lo largo de la trayectoria
            Vector3 worldPos = GetPositionAlongTrajectory(distance);

            // Convertir a posición de pantalla
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

            // Si está detrás de la cámara, no mostrar
            if (screenPos.z < 0)
                continue;

            // Escalar de resolución de RenderTexture a resolución de pantalla real
            if (settingsController != null)
            {
                float scaleX = (float)Screen.width / settingsController.RenderWidth;
                float scaleY = (float)Screen.height / settingsController.RenderHeight;
                screenPos.x *= scaleX;
                screenPos.y *= scaleY;
            }

            // Obtener el sprite según la distancia (dividido en 3 partes)
            Sprite dotSprite = GetSpriteForDistance(distance, totalTrajectoryDistance);

            // Activar y posicionar el punto
            Image dot = dotPool[activeDots];
            dot.gameObject.SetActive(true);
            dot.sprite = dotSprite;

            // Convertir screenPos a posición del canvas
            RectTransform dotRect = dot.rectTransform;
            
            // Usar la posición de pantalla directamente si el canvas es Screen Space Overlay
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                dotRect.position = screenPos;
            }
            else
            {
                // Para otros modos de canvas
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    container, screenPos, canvas.worldCamera, out Vector2 localPoint);
                dotRect.anchoredPosition = localPoint;
            }

            activeDots++;
        }
    }

    Sprite GetSpriteForDistance(float distance, float totalDistance)
    {
        if (totalDistance <= 0)
            return smallDotSprite;

        float progress = distance / totalDistance;

        // Dividir en 3 partes: pequeño (0-33%), mediano (33-66%), grande (66-100%)
        if (progress < 0.33f)
            return smallDotSprite;
        else if (progress < 0.66f)
            return mediumDotSprite;
        else
            return largeDotSprite;
    }

    Vector3[] CalculateBouncePoints(Vector3 start, Vector3 direction, float maxDist)
    {
        Vector3[] points = new Vector3[maxBounces + 2];
        points[0] = start;

        Vector3 currentPos = start;
        Vector3 currentDir = direction.normalized;
        float remainingDistance = maxDist;

        int bounces = 0;

        while (remainingDistance > 0 && bounces <= maxBounces)
        {
            Ray ray = new Ray(currentPos, currentDir);
            if (Physics.Raycast(ray, out RaycastHit hit, remainingDistance, wallLayerMask))
            {
                points[bounces + 1] = hit.point;
                currentDir = Vector3.Reflect(currentDir, hit.normal);
                remainingDistance -= Vector3.Distance(currentPos, hit.point);
                currentPos = hit.point + currentDir * 0.01f;
                bounces++;
            }
            else
            {
                points[bounces + 1] = currentPos + currentDir * remainingDistance;
                break;
            }
        }

        // Redimensionar array
        int finalLength = bounces + 2;
        Vector3[] finalPoints = new Vector3[finalLength];
        for (int i = 0; i < finalLength; i++)
            finalPoints[i] = points[i];

        return finalPoints;
    }

    float CalculateTotalDistance(Vector3[] points)
    {
        if (points == null || points.Length < 2)
            return 0f;

        float total = 0f;
        for (int i = 0; i < points.Length - 1; i++)
            total += Vector3.Distance(points[i], points[i + 1]);
        return total;
    }

    Vector3 GetPositionAlongTrajectory(float distance)
    {
        if (cachedTrajectoryPoints == null || cachedTrajectoryPoints.Length == 0)
            return player.position;

        float current = 0f;

        for (int i = 0; i < cachedTrajectoryPoints.Length - 1; i++)
        {
            float segLen = Vector3.Distance(cachedTrajectoryPoints[i], cachedTrajectoryPoints[i + 1]);
            if (current + segLen >= distance)
            {
                float t = (distance - current) / segLen;
                return Vector3.Lerp(cachedTrajectoryPoints[i], cachedTrajectoryPoints[i + 1], t);
            }
            current += segLen;
        }

        return cachedTrajectoryPoints[^1];
    }

    void EnsurePoolSize(int size)
    {
        // Evitar bucle infinito si no hay prefab
        if (dotPrefab == null || container == null)
            return;

        while (dotPool.Count < size)
        {
            GameObject dot = Instantiate(dotPrefab, container);
            dot.SetActive(false);
            Image img = dot.GetComponent<Image>();
            if (img != null)
                dotPool.Add(img);
            else
            {
                // Si el prefab no tiene Image, destruir y salir para evitar bucle infinito
                Destroy(dot);
                //Debug.LogError("TrajectoryUI: El prefab no tiene componente Image!");
                break;
            }
        }
    }

    void HideAllDots()
    {
        for (int i = 0; i < dotPool.Count; i++)
        {
            if (dotPool[i].gameObject.activeSelf)
                dotPool[i].gameObject.SetActive(false);
        }
        activeDots = 0;
    }

    // ============ API PÚBLICA ============

    /// <summary>
    /// Obtiene los puntos de la trayectoria hasta la distancia cargada actualmente.
    /// </summary>
    public Vector3[] GetTrajectoryPoints()
    {
        if (cachedTrajectoryPoints == null || cachedTrajectoryPoints.Length == 0)
            return new Vector3[] { player.position };

        // Si no hay distancia cargada, devolver posición del jugador
        if (currentGrowDistance <= 0)
            return new Vector3[] { player.position };

        // Construir array de puntos hasta la distancia cargada
        List<Vector3> chargedPoints = new List<Vector3>();
        chargedPoints.Add(cachedTrajectoryPoints[0]);

        float accumulatedDistance = 0f;

        for (int i = 0; i < cachedTrajectoryPoints.Length - 1; i++)
        {
            float segmentLength = Vector3.Distance(cachedTrajectoryPoints[i], cachedTrajectoryPoints[i + 1]);
            
            if (accumulatedDistance + segmentLength >= currentGrowDistance)
            {
                // Este segmento contiene el punto final
                float remainingDistance = currentGrowDistance - accumulatedDistance;
                float t = remainingDistance / segmentLength;
                Vector3 endPoint = Vector3.Lerp(cachedTrajectoryPoints[i], cachedTrajectoryPoints[i + 1], t);
                chargedPoints.Add(endPoint);
                break;
            }
            else
            {
                // Añadir el punto completo
                chargedPoints.Add(cachedTrajectoryPoints[i + 1]);
                accumulatedDistance += segmentLength;
            }
        }

        return chargedPoints.ToArray();
    }

    /// <summary>
    /// Obtiene la distancia actualmente cargada.
    /// </summary>
    public float GetChargedDistance()
    {
        return currentGrowDistance;
    }

    /// <summary>
    /// Obtiene el punto final de la trayectoria.
    /// </summary>
    public Vector3 GetTrajectoryEndPoint()
    {
        if (cachedTrajectoryPoints == null || cachedTrajectoryPoints.Length == 0)
            return player.position;

        return cachedTrajectoryPoints[^1];
    }

    /// <summary>
    /// Activa o desactiva la visualización de la trayectoria.
    /// </summary>
    public void SetActive(bool active)
    {
        if (!active)
        {
            isActive = false;
            currentGrowDistance = 0f;
            HideAllDots();
        }
    }

    /// <summary>
    /// Obtiene la distancia actual de crecimiento.
    /// </summary>
    public float CurrentGrowDistance => currentGrowDistance;

    /// <summary>
    /// Obtiene la distancia máxima de la trayectoria.
    /// </summary>
    public float MaxDistance => maxDistance;
}
