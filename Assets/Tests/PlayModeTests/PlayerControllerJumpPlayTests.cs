using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests en modo Play para PlayerController: saltos y parábolas.
/// Cubre StartJump, UpdateJump, EndJump y la trayectoria parabólica.
/// </summary>
public class PlayerControllerJumpPlayTests
{
    private GameObject playerGO;
    private Component playerControllerComponent;
    private System.Type playerControllerType;
    private GameObject cameraGO;

    [SetUp]
    public void Setup()
    {
        // Crear GameObject del jugador
        playerGO = new GameObject("TestPlayer");
        
        // Añadir PlayerController por reflexión
        playerControllerType = System.Type.GetType("PlayerController, Assembly-CSharp");
        if (playerControllerType == null)
            playerControllerType = System.Type.GetType("PlayerController");
        playerControllerComponent = (Component)playerGO.AddComponent(playerControllerType);

        // Crear cámara
        cameraGO = new GameObject("TestCamera");
        var cam = cameraGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(playerGO);
        Object.Destroy(cameraGO);
    }

    /// <summary>
    /// Test: Salto ejecuta una parábola correcta.
    /// Valida que el jugador alcanza una altura máxima y cae nuevamente.
    /// </summary>
    [UnityTest]
    public IEnumerator Jump_ExecutesParabolicArc()
    {
        Vector3 startPos = playerGO.transform.position;
        Vector3 endPos = startPos + Vector3.forward * 5f;

        // Configurar salto usando propiedades públicas
        var jcType = playerControllerComponent.GetType();
        var jumpHeightProp = jcType.GetProperty("JumpHeight");
        var jumpDurationProp = jcType.GetProperty("JumpDuration");
        
        if (jumpHeightProp != null) jumpHeightProp.SetValue(playerControllerComponent, 3f);
        if (jumpDurationProp != null) jumpDurationProp.SetValue(playerControllerComponent, 0.8f);

        // Iniciar salto con puntos explícitos
        var startJumpMethod = jcType.GetMethod("StartJumpWithPoints");
        float jumpDuration = jumpDurationProp != null ? (float)jumpDurationProp.GetValue(playerControllerComponent) : 0.8f;
        startJumpMethod.Invoke(playerControllerComponent, new object[] { new Vector3[] { startPos, endPos }, jumpDuration });

        // Simular frames durante el salto
        float elapsed = 0f;
        float maxY = startPos.y;
        float midpointY = startPos.y;
        
        var updateMethod = jcType.GetMethod("Update", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        while (elapsed < jumpDuration + 0.1f)
        {
            updateMethod.Invoke(playerControllerComponent, null);
            elapsed += Time.deltaTime;

            float currentY = playerGO.transform.position.y;
            
            // Registrar Y máxima (debería ser alrededor de 0.5 de la duración)
            if (currentY > maxY) maxY = currentY;
            
            // Registrar Y en mitad del salto
            if (elapsed >= jumpDuration * 0.4f && elapsed <= jumpDuration * 0.6f && midpointY == startPos.y)
                midpointY = currentY;
            
            yield return null;
        }

        Vector3 finalPos = playerGO.transform.position;

        // Validaciones
        Assert.Greater(maxY, startPos.y + 0.1f, "El jugador debería alcanzar una altura mayor durante el salto");
        Assert.Greater(midpointY, startPos.y + 0.1f, "La altura en el medio del salto debería ser significativa");
        Assert.AreEqual(startPos.y, finalPos.y, 0.1f, "El jugador debería aterrizar a la misma altura inicial");
        Assert.AreEqual(endPos.x, finalPos.x, 0.2f, "La X final debería estar cerca del destino");
        Assert.AreEqual(endPos.z, finalPos.z, 0.2f, "La Z final debería estar cerca del destino");
    }

    /// <summary>
    /// Test: Salto sigue la trayectoria con múltiples puntos.
    /// Valida que GetPositionAlongTrajectory interpola correctamente.
    /// </summary>
    [UnityTest]
    public IEnumerator Jump_FollowsMultiPointTrajectory()
    {
        Vector3 startPos = playerGO.transform.position;
        Vector3 midPoint = startPos + Vector3.forward * 2.5f + Vector3.right * 2.5f;
        Vector3 endPos = startPos + Vector3.forward * 5f;

        // Crear trayectoria con 3 puntos (inicio, medio, fin)
        Vector3[] trajectoryPoints = new Vector3[] { startPos, midPoint, endPos };

        // Configurar y ejecutar salto
        var jcType = playerControllerComponent.GetType();
        var jumpDurationProp = jcType.GetProperty("JumpDuration");
        if (jumpDurationProp != null) jumpDurationProp.SetValue(playerControllerComponent, 1f);

        var startJumpMethod = jcType.GetMethod("StartJumpWithPoints");
        startJumpMethod.Invoke(playerControllerComponent, new object[] { trajectoryPoints, 1f });

        float elapsed = 0f;
        var updateMethod = jcType.GetMethod("Update",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        while (elapsed < 1.1f)
        {
            updateMethod.Invoke(playerControllerComponent, null);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 finalPos = playerGO.transform.position;

        // Validar que el jugador llegó aproximadamente al final
        Assert.AreEqual(endPos.x, finalPos.x, 0.3f, "X final debería estar cerca del destino");
        Assert.AreEqual(endPos.z, finalPos.z, 0.3f, "Z final debería estar cerca del destino");
    }

    /// <summary>
    /// Test: CurrentState cambia a Jumping cuando inicia el salto.
    /// Valida que el estado se actualiza correctamente.
    /// </summary>
    [Test]
    public void JumpState_ChangesToJumping()
    {
        Vector3 startPos = playerGO.transform.position;
        Vector3 endPos = startPos + Vector3.forward * 5f;

        // Iniciar salto
        var jcType = playerControllerComponent.GetType();
        var startJumpMethod = jcType.GetMethod("StartJumpWithPoints");
        startJumpMethod.Invoke(playerControllerComponent, new object[] { new Vector3[] { startPos, endPos }, 0.5f });

        // Verificar estado
        var stateProperty = jcType.GetProperty("CurrentState");
        var currentState = stateProperty.GetValue(playerControllerComponent);
        
        var playerStateEnum = System.Type.GetType("PlayerState, Assembly-CSharp");
        if (playerStateEnum == null)
            playerStateEnum = System.Type.GetType("PlayerState");
        var jumpingState = System.Enum.Parse(playerStateEnum, "Jumping");

        Assert.AreEqual(jumpingState, currentState, "El estado debería cambiar a Jumping");
    }

    /// <summary>
    /// Test: Altura máxima del salto es proporcional a JumpHeight.
    /// Valida que incrementar JumpHeight aumenta el arco.
    /// </summary>
    [UnityTest]
    public IEnumerator JumpHeight_AffectsArcHeight()
    {
        Vector3 startPos = playerGO.transform.position;
        Vector3 endPos = startPos + Vector3.forward * 5f;

        // Configurar salto con altura específica
        var jcType = playerControllerComponent.GetType();
        var jumpHeightProp = jcType.GetProperty("JumpHeight");
        if (jumpHeightProp != null) jumpHeightProp.SetValue(playerControllerComponent, 5f);

        var jumpDurationProp = jcType.GetProperty("JumpDuration");
        if (jumpDurationProp != null) jumpDurationProp.SetValue(playerControllerComponent, 0.8f);

        // Iniciar salto
        var startJumpMethod = jcType.GetMethod("StartJumpWithPoints");
        startJumpMethod.Invoke(playerControllerComponent, new object[] { new Vector3[] { startPos, endPos }, 0.8f });

        float elapsed = 0f;
        float maxY = startPos.y;
        var updateMethod = jcType.GetMethod("Update",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        while (elapsed < 0.9f)
        {
            updateMethod.Invoke(playerControllerComponent, null);
            elapsed += Time.deltaTime;

            float currentY = playerGO.transform.position.y;
            if (currentY > maxY) maxY = currentY;
            
            yield return null;
        }

        float actualHeight = maxY - startPos.y;
        Assert.Greater(actualHeight, 2f, "Con JumpHeight=5, la altura alcanzada debería ser significativa (>2)");
    }

    /// <summary>
    /// Test: Duración del salto respeta JumpDuration.
    /// Valida que el tiempo de salto es el configurado.
    /// </summary>
    [UnityTest]
    public IEnumerator JumpDuration_ControlsTiming()
    {
        Vector3 startPos = playerGO.transform.position;
        Vector3 endPos = startPos + Vector3.forward * 5f;
        float jumpDuration = 0.5f;

        // Configurar salto
        var jcType = playerControllerComponent.GetType();
        var jumpDurationProp = jcType.GetProperty("JumpDuration");
        if (jumpDurationProp != null) jumpDurationProp.SetValue(playerControllerComponent, jumpDuration);

        var startJumpMethod = jcType.GetMethod("StartJumpWithPoints");
        startJumpMethod.Invoke(playerControllerComponent, new object[] { new Vector3[] { startPos, endPos }, jumpDuration });

        // Medir tiempo hasta que termine el salto
        float elapsed = 0f;
        float jumpEndTime = -1f;
        var updateMethod = jcType.GetMethod("Update",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        var stateProperty = jcType.GetProperty("CurrentState");
        var playerStateEnum = System.Type.GetType("PlayerState, Assembly-CSharp");
        if (playerStateEnum == null)
            playerStateEnum = System.Type.GetType("PlayerState");
        var jumpingState = System.Enum.Parse(playerStateEnum, "Jumping");
        var normalState = System.Enum.Parse(playerStateEnum, "Normal");

        while (elapsed < 1f)
        {
            updateMethod.Invoke(playerControllerComponent, null);
            var currentState = stateProperty.GetValue(playerControllerComponent);
            
            if (currentState.Equals(normalState) && jumpEndTime < 0)
                jumpEndTime = elapsed;
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Validar que el salto completó dentro de un rango razonable
        // Nota: Unity puede tener variaciones en Time.deltaTime
        Assert.Greater(jumpEndTime, -0.1f, "El salto debería haber terminado");
        Assert.Less(jumpEndTime, jumpDuration + 0.5f, "El salto no debería extenderse mucho más allá de la duración especificada");
    }
}
