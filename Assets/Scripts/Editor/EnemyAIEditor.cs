using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

/// <summary>
/// Editor personalizado para EnemyAI con gestión automática de waypoints.
/// </summary>
[CustomEditor(typeof(EnemyAI))]
public class EnemyAIEditor : Editor
{
    private EnemyAI enemyAI;
    
    // SerializedProperties para las secciones que NO son la lista de patrulla
    private SerializedProperty viewAngleProp;
    private SerializedProperty viewDistanceProp;
    private SerializedProperty obstructionMaskProp;
    private SerializedProperty playerMaskProp;
    private SerializedProperty showVisionConeProp;
    private SerializedProperty visionConeColorProp;
    private SerializedProperty visionConeSegmentsProp;
    private SerializedProperty defaultBehaviorProp;
    private SerializedProperty searchDurationProp;
    private SerializedProperty wanderRadiusProp;
    private SerializedProperty wanderIntervalProp;
    private SerializedProperty minWanderDistanceProp;
    private SerializedProperty patrolRouteObjectProp;
    private SerializedProperty patrolRouteIdProp;
    private SerializedProperty loopPatrolProp;
    private SerializedProperty moveSpeedProp;
    private SerializedProperty chaseSpeedProp;
    private SerializedProperty rotationSpeedProp;
    private SerializedProperty stoppingDistanceProp;
    private SerializedProperty pathUpdateIntervalProp;
    
    private bool showWaypointsList = true;

    void OnEnable()
    {
        enemyAI = (EnemyAI)target;
        
        // Obtener todas las propiedades
        viewAngleProp = serializedObject.FindProperty("viewAngle");
        viewDistanceProp = serializedObject.FindProperty("viewDistance");
        obstructionMaskProp = serializedObject.FindProperty("obstructionMask");
        playerMaskProp = serializedObject.FindProperty("playerMask");
        showVisionConeProp = serializedObject.FindProperty("showVisionCone");
        visionConeColorProp = serializedObject.FindProperty("visionConeColor");
        visionConeSegmentsProp = serializedObject.FindProperty("visionConeSegments");
        defaultBehaviorProp = serializedObject.FindProperty("defaultBehavior");
        searchDurationProp = serializedObject.FindProperty("searchDuration");
        wanderRadiusProp = serializedObject.FindProperty("wanderRadius");
        wanderIntervalProp = serializedObject.FindProperty("wanderInterval");
        minWanderDistanceProp = serializedObject.FindProperty("minWanderDistance");
        patrolRouteObjectProp = serializedObject.FindProperty("patrolRouteObject");
        patrolRouteIdProp = serializedObject.FindProperty("patrolRouteId");
        loopPatrolProp = serializedObject.FindProperty("loopPatrol");
        moveSpeedProp = serializedObject.FindProperty("moveSpeed");
        chaseSpeedProp = serializedObject.FindProperty("chaseSpeed");
        rotationSpeedProp = serializedObject.FindProperty("rotationSpeed");
        stoppingDistanceProp = serializedObject.FindProperty("stoppingDistance");
        pathUpdateIntervalProp = serializedObject.FindProperty("pathUpdateInterval");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // === VISIÓN ===
        EditorGUILayout.LabelField("Visión", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(viewAngleProp);
        EditorGUILayout.PropertyField(viewDistanceProp);
        EditorGUILayout.PropertyField(obstructionMaskProp);
        EditorGUILayout.PropertyField(playerMaskProp);
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Debug Visión", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showVisionConeProp);
        EditorGUILayout.PropertyField(visionConeColorProp);
        EditorGUILayout.PropertyField(visionConeSegmentsProp);
        
        EditorGUILayout.Space(10);
        
        // === COMPORTAMIENTO ===
        EditorGUILayout.LabelField("Comportamiento", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(defaultBehaviorProp);
        EditorGUILayout.PropertyField(searchDurationProp, new GUIContent("Tiempo de Búsqueda (s)"));
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Deambular", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(wanderRadiusProp);
        EditorGUILayout.PropertyField(wanderIntervalProp);
        EditorGUILayout.PropertyField(minWanderDistanceProp);
        
        EditorGUILayout.Space(10);
        
        // === PATRULLA ===
        EditorGUILayout.LabelField("Patrulla", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(patrolRouteObjectProp, new GUIContent("Objeto Ruta"));
        EditorGUILayout.PropertyField(patrolRouteIdProp, new GUIContent("ID Ruta (alternativo)"));
        EditorGUILayout.PropertyField(loopPatrolProp, new GUIContent("Ruta en Bucle"));
        
        EditorGUILayout.Space(5);
        
        // Lista de waypoints personalizada
        DrawWaypointsList();
        
        EditorGUILayout.Space(10);
        
        // === MOVIMIENTO ===
        EditorGUILayout.LabelField("Movimiento", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(moveSpeedProp);
        EditorGUILayout.PropertyField(chaseSpeedProp);
        EditorGUILayout.PropertyField(rotationSpeedProp);
        EditorGUILayout.PropertyField(stoppingDistanceProp);
        EditorGUILayout.PropertyField(pathUpdateIntervalProp);
        
        EditorGUILayout.Space(10);
        
        // === DEBUG ===
        DrawDebugSection();
        
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawWaypointsList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Header con foldout
        EditorGUILayout.BeginHorizontal();
        showWaypointsList = EditorGUILayout.Foldout(showWaypointsList, $"Waypoints ({enemyAI.PatrolRoute.Count})", true);
        
        GUILayout.FlexibleSpace();
        
        // Botón para añadir waypoint
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            AddWaypoint();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        if (showWaypointsList && enemyAI.PatrolRoute.Count > 0)
        {
            EditorGUILayout.Space(5);
            
            int indexToRemove = -1;
            int indexToMoveUp = -1;
            int indexToMoveDown = -1;
            
            for (int i = 0; i < enemyAI.PatrolRoute.Count; i++)
            {
                var point = enemyAI.PatrolRoute[i];
                
                EditorGUILayout.BeginHorizontal();
                
                // Índice
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));
                
                // Nombre del waypoint (clickeable para seleccionar)
                string pointName = point.point != null ? point.point.name : "(vacío)";
                if (GUILayout.Button(pointName, EditorStyles.linkLabel, GUILayout.Width(100)))
                {
                    if (point.point != null)
                    {
                        Selection.activeGameObject = point.point.gameObject;
                        SceneView.FrameLastActiveSceneView();
                    }
                }
                
                // Tiempo de espera
                EditorGUILayout.LabelField("Espera:", GUILayout.Width(45));
                float newWaitTime = EditorGUILayout.FloatField(point.waitTime, GUILayout.Width(40));
                if (newWaitTime != point.waitTime)
                {
                    Undo.RecordObject(enemyAI, "Change Wait Time");
                    point.waitTime = newWaitTime;
                    EditorUtility.SetDirty(enemyAI);
                }
                EditorGUILayout.LabelField("s", GUILayout.Width(15));
                
                GUILayout.FlexibleSpace();
                
                // Botones de reordenar
                GUI.enabled = i > 0;
                if (GUILayout.Button("▲", GUILayout.Width(22)))
                {
                    indexToMoveUp = i;
                }
                GUI.enabled = i < enemyAI.PatrolRoute.Count - 1;
                if (GUILayout.Button("▼", GUILayout.Width(22)))
                {
                    indexToMoveDown = i;
                }
                GUI.enabled = true;
                
                // Botón eliminar
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("×", GUILayout.Width(22)))
                {
                    indexToRemove = i;
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            // Procesar acciones después del loop para evitar modificar la colección mientras iteramos
            if (indexToRemove >= 0)
            {
                RemoveWaypoint(indexToRemove);
            }
            if (indexToMoveUp >= 0)
            {
                SwapWaypoints(indexToMoveUp, indexToMoveUp - 1);
            }
            if (indexToMoveDown >= 0)
            {
                SwapWaypoints(indexToMoveDown, indexToMoveDown + 1);
            }
        }
        else if (showWaypointsList && enemyAI.PatrolRoute.Count == 0)
        {
            EditorGUILayout.HelpBox("No hay waypoints. Pulsa '+' para crear uno.", MessageType.Info);
        }
        
        EditorGUILayout.Space(5);
        
        // Botones adicionales
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Crear Ruta (3 puntos)"))
        {
            CreatePatrolRoute(3);
        }
        
        if (GUILayout.Button("Sincronizar con Route"))
        {
            SyncWithRouteObject();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }

    private void DrawDebugSection()
    {
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Toggle Cono de Visión"))
        {
            enemyAI.ToggleVisionCone();
            SceneView.RepaintAll();
        }
        
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField($"Estado: {enemyAI.CurrentState}");
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField($"Ve al jugador: {enemyAI.CanSeePlayer}");
        }
    }

    private void AddWaypoint()
    {
        Transform parent = GetOrCreateRouteObject();
        
        // Asegurar que tiene el visualizador
        if (parent.GetComponent<PatrolRouteVisualizer>() == null)
        {
            Undo.AddComponent<PatrolRouteVisualizer>(parent.gameObject);
        }

        int waypointIndex = parent.childCount;
        GameObject waypoint = new GameObject($"Waypoint_{waypointIndex}");
        Undo.RegisterCreatedObjectUndo(waypoint, "Create Waypoint");
        
        waypoint.transform.parent = parent;
        
        // Posicionar basándose en el último waypoint o en el enemigo
        if (enemyAI.PatrolRoute.Count > 0)
        {
            var lastPoint = enemyAI.PatrolRoute[enemyAI.PatrolRoute.Count - 1];
            if (lastPoint.point != null)
            {
                waypoint.transform.position = lastPoint.point.position + Vector3.forward * 2f;
            }
            else
            {
                waypoint.transform.position = enemyAI.transform.position + Vector3.forward * 2f;
            }
        }
        else
        {
            waypoint.transform.position = enemyAI.transform.position + Vector3.forward * 2f;
        }
        
        // Añadir a la lista
        Undo.RecordObject(enemyAI, "Add Waypoint");
        enemyAI.AddPatrolPoint(waypoint.transform, 0f);
        
        EditorUtility.SetDirty(enemyAI);
        Selection.activeGameObject = waypoint;
        SceneView.FrameLastActiveSceneView();
    }

    private void RemoveWaypoint(int index)
    {
        if (index < 0 || index >= enemyAI.PatrolRoute.Count) return;
        
        var point = enemyAI.PatrolRoute[index];
        GameObject waypointToDestroy = point.point != null ? point.point.gameObject : null;
        
        Undo.RecordObject(enemyAI, "Remove Waypoint");
        enemyAI.PatrolRoute.RemoveAt(index);
        EditorUtility.SetDirty(enemyAI);
        
        // Destruir el GameObject del waypoint
        if (waypointToDestroy != null)
        {
            Undo.DestroyObjectImmediate(waypointToDestroy);
        }
        
        // Renombrar los waypoints restantes para mantener orden
        RenameWaypoints();
    }

    private void SwapWaypoints(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= enemyAI.PatrolRoute.Count) return;
        if (indexB < 0 || indexB >= enemyAI.PatrolRoute.Count) return;
        
        Undo.RecordObject(enemyAI, "Reorder Waypoints");
        
        var temp = enemyAI.PatrolRoute[indexA];
        enemyAI.PatrolRoute[indexA] = enemyAI.PatrolRoute[indexB];
        enemyAI.PatrolRoute[indexB] = temp;
        
        EditorUtility.SetDirty(enemyAI);
        RenameWaypoints();
    }

    private void RenameWaypoints()
    {
        for (int i = 0; i < enemyAI.PatrolRoute.Count; i++)
        {
            var point = enemyAI.PatrolRoute[i];
            if (point.point != null)
            {
                string newName = $"Waypoint_{i}";
                if (point.point.name != newName)
                {
                    Undo.RecordObject(point.point.gameObject, "Rename Waypoint");
                    point.point.name = newName;
                }
            }
        }
    }

    private void SyncWithRouteObject()
    {
        Transform routeObject = enemyAI.PatrolRouteObject;
        
        if (routeObject == null)
        {
            // Intentar buscar por ID
            if (!string.IsNullOrEmpty(enemyAI.PatrolRouteId))
            {
                GameObject routeObj = GameObject.Find(enemyAI.PatrolRouteId);
                if (routeObj != null)
                {
                    routeObject = routeObj.transform;
                    Undo.RecordObject(enemyAI, "Assign Route");
                    enemyAI.PatrolRouteObject = routeObject;
                }
            }
        }

        if (routeObject == null)
        {
            EditorUtility.DisplayDialog("Info", "No hay objeto Route. Se creará uno al añadir waypoints.", "OK");
            return;
        }

        if (routeObject.childCount == 0)
        {
            EditorUtility.DisplayDialog("Info", $"El objeto '{routeObject.name}' no tiene hijos.", "OK");
            return;
        }

        Undo.RecordObject(enemyAI, "Sync Waypoints");
        enemyAI.ClearPatrolRoute();
        
        foreach (Transform child in routeObject)
        {
            enemyAI.AddPatrolPoint(child, 0f);
        }
        
        EditorUtility.SetDirty(enemyAI);
        Debug.Log($"Sincronizados {routeObject.childCount} waypoints desde '{routeObject.name}'.");
    }

    private void CreatePatrolRoute(int pointCount)
    {
        // Limpiar waypoints existentes primero
        while (enemyAI.PatrolRoute.Count > 0)
        {
            RemoveWaypoint(0);
        }
        
        Transform parent = GetOrCreateRouteObject();
        
        // Asegurar que tiene el visualizador
        if (parent.GetComponent<PatrolRouteVisualizer>() == null)
        {
            Undo.AddComponent<PatrolRouteVisualizer>(parent.gameObject);
        }
        
        Undo.RecordObject(enemyAI, "Create Patrol Route");
        
        for (int i = 0; i < pointCount; i++)
        {
            float angle = (360f / pointCount) * i * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * 3f;
            
            GameObject waypoint = new GameObject($"Waypoint_{i}");
            Undo.RegisterCreatedObjectUndo(waypoint, "Create Waypoint");
            
            waypoint.transform.parent = parent;
            waypoint.transform.position = enemyAI.transform.position + offset;
            
            enemyAI.AddPatrolPoint(waypoint.transform, 0f);
        }
        
        EditorUtility.SetDirty(enemyAI);
        Selection.activeGameObject = parent.gameObject;
    }

    private Transform GetOrCreateRouteObject()
    {
        // Usar el objeto asignado en el EnemyAI
        if (enemyAI.PatrolRouteObject != null)
        {
            return enemyAI.PatrolRouteObject;
        }

        // Buscar por ID si está configurado
        if (!string.IsNullOrEmpty(enemyAI.PatrolRouteId))
        {
            GameObject routeObj = GameObject.Find(enemyAI.PatrolRouteId);
            if (routeObj != null)
            {
                Undo.RecordObject(enemyAI, "Assign Route");
                enemyAI.PatrolRouteObject = routeObj.transform;
                EditorUtility.SetDirty(enemyAI);
                return routeObj.transform;
            }
        }

        // Extraer ID del nombre del enemigo o generar uno único
        string enemyId = ExtractEnemyId(enemyAI.name);
        string routeName = $"Enemy{enemyId}Route";
        
        // Verificar si ya existe con ese nombre
        GameObject existingRoute = GameObject.Find(routeName);
        if (existingRoute != null)
        {
            Undo.RecordObject(enemyAI, "Assign Route");
            enemyAI.PatrolRouteObject = existingRoute.transform;
            enemyAI.PatrolRouteId = routeName;
            EditorUtility.SetDirty(enemyAI);
            return existingRoute.transform;
        }
        
        // Crear nuevo objeto Route
        GameObject newRoute = new GameObject(routeName);
        newRoute.transform.position = Vector3.zero;
        newRoute.AddComponent<PatrolRouteVisualizer>();
        Undo.RegisterCreatedObjectUndo(newRoute, "Create Route");

        Undo.RecordObject(enemyAI, "Assign Route");
        enemyAI.PatrolRouteObject = newRoute.transform;
        enemyAI.PatrolRouteId = routeName;
        EditorUtility.SetDirty(enemyAI);
        
        Debug.Log($"Creado objeto de ruta: {routeName}");

        return newRoute.transform;
    }

    private string ExtractEnemyId(string enemyName)
    {
        System.Text.StringBuilder numbers = new System.Text.StringBuilder();
        foreach (char c in enemyName)
        {
            if (char.IsDigit(c))
            {
                numbers.Append(c);
            }
        }
        
        if (numbers.Length > 0)
        {
            return numbers.ToString();
        }
        
        int instanceId = enemyAI.GetInstanceID();
        return Mathf.Abs(instanceId % 10000).ToString();
    }

    // Dibujar handles en la escena para los waypoints
    void OnSceneGUI()
    {
        if (enemyAI.PatrolRoute == null) return;

        for (int i = 0; i < enemyAI.PatrolRoute.Count; i++)
        {
            var point = enemyAI.PatrolRoute[i];
            if (point.point == null) continue;

            // Handle para mover el waypoint
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(point.point.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(point.point, "Move Waypoint");
                point.point.position = newPos;
            }

            // Etiqueta con número y tiempo de espera
            string label = $"[{i}]";
            if (point.waitTime > 0)
            {
                label += $" ({point.waitTime}s)";
            }
            
            Handles.Label(point.point.position + Vector3.up * 1.5f, label, 
                new GUIStyle(GUI.skin.label) 
                { 
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.cyan }
                });
        }
    }
}
