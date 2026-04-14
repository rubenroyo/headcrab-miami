using UnityEngine;

/// <summary>
/// Calcula una parábola con longitud de arco fija dado un rayo de ápice.
/// 
/// Problema: Hay infinitas parábolas cuyo ápice está en un rayo dado.
/// Solución: Solo UNA tiene la longitud de arco exacta deseada.
/// 
/// Algoritmo:
/// 1. Búsqueda binaria a lo largo del rayo para encontrar el punto del ápice
/// 2. Para cada candidato, construir la parábola P0 → Apex → Landing
/// 3. Calcular longitud de arco numéricamente (Simpson's rule)
/// 4. Converger hasta que arc_length == target_length
/// </summary>
public static class ParabolaArcSolver
{
    /// <summary>
    /// Resultado del cálculo de la parábola
    /// </summary>
    public struct ParabolaResult
    {
        public bool IsValid;
        public Vector3 Apex;           // Punto más alto de la parábola
        public Vector3 LandingPoint;   // Donde aterriza
        public Vector3[] Points;       // Puntos para renderizar
        public float ActualArcLength;  // Longitud real calculada
        public float FlightTime;       // Tiempo de vuelo estimado
        public Vector3 InitialVelocity; // Velocidad inicial para la física
    }
    
    /// <summary>
    /// Calcula la parábola dado el origen, rayo del ápice y longitud de arco deseada.
    /// </summary>
    /// <param name="origin">P0 - Posición inicial del jugador</param>
    /// <param name="apexRayOrigin">Origen del rayo (posición de la cámara)</param>
    /// <param name="apexRayDirection">Dirección del rayo (normalizada)</param>
    /// <param name="targetArcLength">Longitud de arco deseada (ej: 10 unidades)</param>
    /// <param name="gravity">Gravedad (positiva, ej: 20)</param>
    /// <param name="resolution">Número de puntos en la trayectoria</param>
    /// <param name="groundMask">LayerMask para detectar el suelo</param>
    /// <returns>Resultado con la parábola calculada</returns>
    public static ParabolaResult SolveParabola(
        Vector3 origin,
        Vector3 apexRayOrigin,
        Vector3 apexRayDirection,
        float targetArcLength,
        float gravity,
        int resolution,
        LayerMask groundMask)
    {
        ParabolaResult result = new ParabolaResult { IsValid = false };
        
        // Normalizar dirección del rayo
        apexRayDirection = apexRayDirection.normalized;
        
        // Buscar el rango de distancias válidas a lo largo del rayo
        // El ápice debe estar:
        // 1. Por encima del origen (apex.y > origin.y)
        // 2. A una distancia razonable
        
        float minDist = 0.1f;
        float maxDist = targetArcLength * 2f; // El ápice no puede estar más lejos que el doble del arco
        
        // Encontrar punto inicial válido en el rayo
        Vector3 testApex = apexRayOrigin + apexRayDirection * minDist;
        
        // Si el rayo va hacia abajo desde el origen, no es válido
        if (apexRayDirection.y < -0.9f)
        {
            Debug.LogWarning("[ParabolaSolver] Apex ray pointing too far down");
            return result;
        }
        
        // Búsqueda binaria para encontrar el ápice correcto
        const int maxIterations = 50;
        const float tolerance = 0.01f; // 1cm de tolerancia
        
        float bestDist = -1f;
        float bestArcLength = 0f;
        
        for (int iter = 0; iter < maxIterations; iter++)
        {
            float midDist = (minDist + maxDist) / 2f;
            Vector3 candidateApex = apexRayOrigin + apexRayDirection * midDist;
            
            // El ápice debe estar por encima del origen
            if (candidateApex.y <= origin.y + 0.1f)
            {
                // Mover minDist hacia adelante para buscar un ápice más alto
                minDist = midDist;
                continue;
            }
            
            // Calcular la parábola con este ápice
            float arcLength = CalculateParabolaArcLength(origin, candidateApex, gravity, resolution, groundMask, out Vector3 landing);
            
            if (arcLength < 0)
            {
                // No se encontró aterrizaje válido, probar con ápice más cercano
                maxDist = midDist;
                continue;
            }
            
            bestDist = midDist;
            bestArcLength = arcLength;
            
            // Verificar convergencia
            float error = Mathf.Abs(arcLength - targetArcLength);
            if (error < tolerance)
            {
                // ¡Encontrado!
                break;
            }
            
            // Ajustar búsqueda
            if (arcLength < targetArcLength)
            {
                // Necesitamos un arco más largo → ápice más lejos
                minDist = midDist;
            }
            else
            {
                // Arco muy largo → ápice más cerca
                maxDist = midDist;
            }
        }
        
        if (bestDist < 0)
        {
            Debug.LogWarning("[ParabolaSolver] Could not find valid apex");
            return result;
        }
        
        // Construir resultado final con el mejor ápice encontrado
        Vector3 finalApex = apexRayOrigin + apexRayDirection * bestDist;
        Vector3[] points = GenerateParabolaPoints(origin, finalApex, gravity, resolution, groundMask, 
            out Vector3 finalLanding, out float flightTime, out Vector3 initialVelocity);
        
        if (points == null || points.Length < 2)
        {
            return result;
        }
        
        result.IsValid = true;
        result.Apex = finalApex;
        result.LandingPoint = finalLanding;
        result.Points = points;
        result.ActualArcLength = bestArcLength;
        result.FlightTime = flightTime;
        result.InitialVelocity = initialVelocity;
        
        return result;
    }
    
    /// <summary>
    /// Calcula la longitud de arco de una parábola desde origen hasta aterrizaje.
    /// Usa la fórmula paramétrica de la parábola y Simpson's rule para integración.
    /// </summary>
    private static float CalculateParabolaArcLength(
        Vector3 origin, 
        Vector3 apex, 
        float gravity,
        int segments,
        LayerMask groundMask,
        out Vector3 landingPoint)
    {
        landingPoint = Vector3.zero;
        
        // Calcular parámetros de la parábola
        // La parábola pasa por origin, tiene vértice en apex, y baja por gravedad
        
        // Dirección horizontal del salto (de origen hacia la proyección horizontal del ápice)
        Vector3 originToApex = apex - origin;
        Vector3 horizontalDir = new Vector3(originToApex.x, 0, originToApex.z);
        float horizontalDistToApex = horizontalDir.magnitude;
        
        if (horizontalDistToApex < 0.01f)
        {
            // Salto vertical puro - usar forward del origen como dirección
            horizontalDir = Vector3.forward;
            horizontalDistToApex = 0.01f;
        }
        else
        {
            horizontalDir = horizontalDir.normalized;
        }
        
        float apexHeight = apex.y - origin.y;
        
        // Para una parábola simétrica con gravedad:
        // y = y0 + vy*t - 0.5*g*t²
        // El tiempo hasta el ápice: t_apex = vy / g
        // Altura del ápice: h = vy² / (2g)
        // Por lo tanto: vy = sqrt(2 * g * h)
        
        float vy = Mathf.Sqrt(2f * gravity * apexHeight);
        float timeToApex = vy / gravity;
        
        // Velocidad horizontal para alcanzar la distancia horizontal en ese tiempo
        float vx = horizontalDistToApex / timeToApex;
        
        // Generar puntos y calcular longitud de arco
        float totalArcLength = 0f;
        Vector3 prevPoint = origin;
        bool foundLanding = false;
        
        // Simular hasta encontrar el suelo (máximo 2x tiempo al ápice para la bajada)
        float maxTime = timeToApex * 4f;
        float dt = maxTime / segments;
        
        for (int i = 1; i <= segments; i++)
        {
            float t = i * dt;
            
            // Posición en el tiempo t
            float x = vx * t;
            float y = vy * t - 0.5f * gravity * t * t;
            
            Vector3 point = origin + horizontalDir * x + Vector3.up * y;
            
            // Detectar colisión con suelo
            Vector3 direction = point - prevPoint;
            float distance = direction.magnitude;
            
            if (distance > 0.001f && Physics.Raycast(prevPoint, direction.normalized, out RaycastHit hit, distance * 1.1f, groundMask))
            {
                landingPoint = hit.point;
                totalArcLength += Vector3.Distance(prevPoint, landingPoint);
                foundLanding = true;
                break;
            }
            
            // Acumular longitud de arco
            totalArcLength += distance;
            prevPoint = point;
            
            // Si bajamos mucho sin encontrar suelo, abortar
            if (point.y < origin.y - 50f)
            {
                break;
            }
        }
        
        if (!foundLanding)
        {
            // Estimar punto de aterrizaje al nivel del origen
            // Resolver: origin.y + vy*t - 0.5*g*t² = origin.y
            // 0 = vy*t - 0.5*g*t² = t*(vy - 0.5*g*t)
            // t = 2*vy/g (tiempo total de vuelo para aterrizar al mismo nivel)
            float totalFlightTime = 2f * vy / gravity;
            float totalHorizontalDist = vx * totalFlightTime;
            landingPoint = origin + horizontalDir * totalHorizontalDist;
            
            // Recalcular longitud de arco más precisamente para parábola completa
            totalArcLength = CalculateArcLengthAnalytical(vx, vy, gravity, totalFlightTime);
        }
        
        return totalArcLength;
    }
    
    /// <summary>
    /// Calcula la longitud de arco de una parábola analíticamente usando la integral.
    /// Arc length = ∫√(1 + (dy/dx)²) dx
    /// Para y = vy/vx * x - g/(2*vx²) * x²
    /// </summary>
    private static float CalculateArcLengthAnalytical(float vx, float vy, float g, float totalTime)
    {
        // Usar Simpson's rule para integrar
        // ds/dt = √(vx² + (vy - g*t)²)
        
        int n = 100; // Número de subdivisiones (debe ser par)
        float dt = totalTime / n;
        
        float sum = 0f;
        
        // Simpson's rule: ∫f(x)dx ≈ (h/3) * [f(x0) + 4*f(x1) + 2*f(x2) + 4*f(x3) + ... + f(xn)]
        for (int i = 0; i <= n; i++)
        {
            float t = i * dt;
            float vyAtT = vy - g * t;
            float speed = Mathf.Sqrt(vx * vx + vyAtT * vyAtT);
            
            float weight;
            if (i == 0 || i == n)
                weight = 1f;
            else if (i % 2 == 1)
                weight = 4f;
            else
                weight = 2f;
            
            sum += weight * speed;
        }
        
        return (dt / 3f) * sum;
    }
    
    /// <summary>
    /// Genera los puntos de la parábola para renderizado.
    /// </summary>
    private static Vector3[] GenerateParabolaPoints(
        Vector3 origin,
        Vector3 apex,
        float gravity,
        int resolution,
        LayerMask groundMask,
        out Vector3 landingPoint,
        out float flightTime,
        out Vector3 initialVelocity)
    {
        landingPoint = origin;
        flightTime = 0f;
        initialVelocity = Vector3.zero;
        
        // Calcular parámetros
        Vector3 originToApex = apex - origin;
        Vector3 horizontalDir = new Vector3(originToApex.x, 0, originToApex.z);
        float horizontalDistToApex = horizontalDir.magnitude;
        
        if (horizontalDistToApex < 0.01f)
        {
            horizontalDir = Vector3.forward;
            horizontalDistToApex = 0.01f;
        }
        else
        {
            horizontalDir = horizontalDir.normalized;
        }
        
        float apexHeight = apex.y - origin.y;
        if (apexHeight <= 0)
        {
            return null;
        }
        
        float vy = Mathf.Sqrt(2f * gravity * apexHeight);
        float timeToApex = vy / gravity;
        float vx = horizontalDistToApex / timeToApex;
        
        // Velocidad inicial
        initialVelocity = horizontalDir * vx + Vector3.up * vy;
        
        // Estimar tiempo total de vuelo
        float estimatedTotalTime = timeToApex * 4f; // Dar margen para encontrar el suelo
        
        System.Collections.Generic.List<Vector3> points = new System.Collections.Generic.List<Vector3>();
        points.Add(origin);
        
        float dt = estimatedTotalTime / resolution;
        Vector3 prevPoint = origin;
        bool foundLanding = false;
        
        for (int i = 1; i <= resolution * 2; i++) // Extra iterations to find ground
        {
            float t = i * dt;
            
            float x = vx * t;
            float y = vy * t - 0.5f * gravity * t * t;
            
            Vector3 point = origin + horizontalDir * x + Vector3.up * y;
            
            // Detectar colisión con suelo
            Vector3 direction = point - prevPoint;
            float distance = direction.magnitude;
            
            if (distance > 0.001f && Physics.Raycast(prevPoint + Vector3.up * 0.1f, direction.normalized, out RaycastHit hit, distance * 1.5f, groundMask))
            {
                landingPoint = hit.point;
                points.Add(landingPoint);
                flightTime = t;
                foundLanding = true;
                break;
            }
            
            points.Add(point);
            prevPoint = point;
            
            // Límite de seguridad
            if (point.y < origin.y - 100f || points.Count > resolution * 2)
            {
                break;
            }
        }
        
        if (!foundLanding)
        {
            // Aterrizar al nivel del origen
            float totalT = 2f * vy / gravity;
            float totalX = vx * totalT;
            landingPoint = origin + horizontalDir * totalX;
            points.Add(landingPoint);
            flightTime = totalT;
        }
        
        return points.ToArray();
    }
    
    /// <summary>
    /// Versión simplificada que solo necesita origen, dirección de mira y longitud de arco.
    /// Construye el rayo del ápice automáticamente apuntando hacia arriba en la dirección de mira.
    /// </summary>
    public static ParabolaResult SolveParabolaSimple(
        Vector3 origin,
        Vector3 lookDirection,
        float targetArcLength,
        float gravity,
        int resolution,
        LayerMask groundMask,
        float apexAngle = 45f) // Ángulo del rayo del ápice respecto al horizontal
    {
        // Construir rayo del ápice
        lookDirection.y = 0;
        lookDirection = lookDirection.normalized;
        
        float angleRad = apexAngle * Mathf.Deg2Rad;
        Vector3 apexRayDir = lookDirection * Mathf.Cos(angleRad) + Vector3.up * Mathf.Sin(angleRad);
        
        return SolveParabola(origin, origin, apexRayDir, targetArcLength, gravity, resolution, groundMask);
    }
}
