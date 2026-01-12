using UnityEngine;

public class TrajectoryPreview : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public Camera mainCamera;
    public LineRenderer lineRenderer;

    [Header("Salto")]
    public float maxDistance = 10f;

    [Header("Rebotes")]
    [SerializeField] private int maxBounces = 3; 
    [SerializeField] private LayerMask wallLayerMask;

    private Vector3[] cachedTrajectoryPoints;


    void Update()
    {
        // No mostrar la línea si el Player está saltando
        PlayerController playerCtrl = player.GetComponent<PlayerController>();
        if (playerCtrl != null && playerCtrl.CurrentState == PlayerState.Jumping)
        {
            lineRenderer.enabled = false;
            return;
        }

        if (Input.GetMouseButton(1)) // clic derecho
        {
            lineRenderer.enabled = true;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            Vector3 mouseWorld = player.position;
            if (groundPlane.Raycast(ray, out float distance))
                mouseWorld = ray.GetPoint(distance);

            Vector3 dir = mouseWorld - player.position;
            dir.y = 0f;

            if (dir.magnitude > maxDistance)
                dir = dir.normalized * maxDistance;

            Vector3[] points = CalculateBouncePoints(player.position, dir, maxDistance);
            cachedTrajectoryPoints = points;

            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }


    Vector3[] CalculateBouncePoints(Vector3 start, Vector3 direction, float maxDist)
    {
        Vector3[] points = new Vector3[maxBounces + 2]; // inicio + rebotes + fin
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

                // Calculamos dirección reflejada
                currentDir = Vector3.Reflect(currentDir, hit.normal);
                // Reducimos la distancia restante
                remainingDistance -= Vector3.Distance(currentPos, hit.point);
                // Nueva posición de origen
                currentPos = hit.point + currentDir * 0.01f; // pequeño offset para evitar engancharse
                bounces++;
            }
            else
            {
                // No choca → último punto
                points[bounces + 1] = currentPos + currentDir * remainingDistance;
                break;
            }
        }

        // Redimensionamos el array para quitar posiciones vacías
        int finalLength = bounces + 2;
        Vector3[] finalPoints = new Vector3[finalLength];
        for (int i = 0; i < finalLength; i++)
            finalPoints[i] = points[i];

        return finalPoints;
    }

    /// <summary>
    /// Obtiene el punto final de la trayectoria actual (último punto del arco)
    /// </summary>
    public Vector3 GetTrajectoryEndPoint()
    {
        if (cachedTrajectoryPoints == null || cachedTrajectoryPoints.Length == 0)
            return player.position;
        
        return cachedTrajectoryPoints[cachedTrajectoryPoints.Length - 1];
    }

    /// <summary>
    /// Obtiene todos los puntos de la trayectoria (con rebotes)
    /// </summary>
    public Vector3[] GetTrajectoryPoints()
    {
        if (cachedTrajectoryPoints == null || cachedTrajectoryPoints.Length == 0)
            return new Vector3[] { player.position };
        
        return cachedTrajectoryPoints;
    }

    /// <summary>
    /// Activa o desactiva el renderer de trayectoria
    /// </summary>
    public void SetActive(bool active)
    {
        lineRenderer.enabled = active;
    }
}
