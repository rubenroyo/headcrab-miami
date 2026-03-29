using UnityEngine;

/// <summary>
/// Visualiza la ruta de patrulla dibujando líneas entre los waypoints hijos.
/// Añadir este componente a un objeto "Route" que contenga waypoints como hijos.
/// </summary>
public class PatrolRouteVisualizer : MonoBehaviour
{
    [Header("Visualización")]
    [SerializeField] private Color lineColor = Color.cyan;
    [SerializeField] private Color waypointColor = Color.yellow;
    [SerializeField] private float waypointRadius = 0.3f;
    [SerializeField] private bool showInGame = false;
    [SerializeField] private bool loopRoute = true;

    [Header("Flechas direccionales")]
    [SerializeField] private bool showArrows = true;
    [SerializeField] private float arrowSize = 0.5f;

    void OnDrawGizmos()
    {
        DrawRoute();
    }

    private void DrawRoute()
    {
        if (transform.childCount < 2) return;

        Transform[] waypoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints[i] = transform.GetChild(i);
        }

        // Dibujar waypoints y líneas
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Vector3 currentPos = waypoints[i].position;

            // Dibujar esfera del waypoint
            Gizmos.color = waypointColor;
            Gizmos.DrawWireSphere(currentPos, waypointRadius);

            // Dibujar línea al siguiente waypoint
            int nextIndex = (i + 1) % waypoints.Length;
            
            // Si no es loop, no dibujar la línea del último al primero
            if (!loopRoute && i == waypoints.Length - 1) continue;
            
            if (waypoints[nextIndex] != null)
            {
                Vector3 nextPos = waypoints[nextIndex].position;
                
                // Línea
                Gizmos.color = lineColor;
                Gizmos.DrawLine(currentPos, nextPos);

                // Flecha direccional
                if (showArrows)
                {
                    DrawArrow(currentPos, nextPos);
                }
            }
        }
    }

    private void DrawArrow(Vector3 from, Vector3 to)
    {
        Vector3 direction = (to - from).normalized;
        Vector3 midPoint = Vector3.Lerp(from, to, 0.5f);
        
        // Calcular puntas de la flecha
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 arrowTip = midPoint + direction * arrowSize * 0.5f;
        Vector3 arrowLeft = midPoint - direction * arrowSize * 0.5f + right * arrowSize * 0.3f;
        Vector3 arrowRight = midPoint - direction * arrowSize * 0.5f - right * arrowSize * 0.3f;

        Gizmos.color = lineColor;
        Gizmos.DrawLine(arrowTip, arrowLeft);
        Gizmos.DrawLine(arrowTip, arrowRight);
    }

    /// <summary>
    /// Obtiene los waypoints ordenados como array de Transforms.
    /// </summary>
    public Transform[] GetWaypoints()
    {
        Transform[] waypoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints[i] = transform.GetChild(i);
        }
        return waypoints;
    }

    // Visualización en runtime (opcional)
    void Update()
    {
        if (showInGame && Application.isPlaying)
        {
            DrawRuntimeLines();
        }
    }

    private void DrawRuntimeLines()
    {
        if (transform.childCount < 2) return;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform current = transform.GetChild(i);
            int nextIndex = (i + 1) % transform.childCount;
            
            if (!loopRoute && i == transform.childCount - 1) continue;
            
            Transform next = transform.GetChild(nextIndex);
            
            if (current != null && next != null)
            {
                Debug.DrawLine(current.position, next.position, lineColor);
            }
        }
    }
}
