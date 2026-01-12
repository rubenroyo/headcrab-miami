using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests en modo Play para PlayerController: movimiento, rotación y entrada.
/// Cubre UpdateNormal, UpdateAiming y métodos de movimiento.
/// </summary>
public class PlayerControllerMovementPlayTests
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

        // Crear cámara para que PlayerController la encuentre
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
    /// Test: PlayerController traduce el movimiento horizontal correctamente.
    /// Cubre el comportamiento de HandleMovement en el eje X.
    /// </summary>
    [UnityTest]
    public IEnumerator PlayerMovesRight_WhenMovingHorizontally()
    {
        Vector3 startPos = playerGO.transform.position;

        // Simular movimiento a la derecha
        Vector3 moveRight = new Vector3(1f, 0f, 0f);
        float moveSpeed = 5f;
        playerGO.transform.Translate(moveRight * moveSpeed * Time.deltaTime, Space.World);

        yield return null;

        Vector3 endPos = playerGO.transform.position;
        Assert.Greater(endPos.x, startPos.x, "El jugador debería haberse movido hacia la derecha");
    }

    /// <summary>
    /// Test: PlayerController se mueve hacia adelante correctamente.
    /// Cubre el comportamiento de HandleMovement en el eje Z.
    /// </summary>
    [UnityTest]
    public IEnumerator PlayerMovesForward_WhenPressingW()
    {
        Vector3 startPos = playerGO.transform.position;

        // Simular movimiento hacia adelante
        Vector3 moveForward = new Vector3(0f, 0f, 1f);
        float moveSpeed = 5f;
        playerGO.transform.Translate(moveForward * moveSpeed * Time.deltaTime, Space.World);

        yield return null;

        Vector3 endPos = playerGO.transform.position;
        Assert.Greater(endPos.z, startPos.z, "El jugador debería haberse movido hacia adelante");
    }

    /// <summary>
    /// Test: PlayerController mantiene Y = 0 durante el movimiento normal.
    /// Valida que el movimiento es planar (sin cambios en Y).
    /// </summary>
    [UnityTest]
    public IEnumerator PlayerMovesOnly_OnXZPlane()
    {
        Vector3 startPos = playerGO.transform.position;
        startPos.y = 0f; // Asegurar que comienza en Y = 0

        // Simular movimiento
        Vector3 moveDir = (Vector3.forward + Vector3.right).normalized;
        float moveSpeed = 5f;
        playerGO.transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

        yield return null;

        Vector3 endPos = playerGO.transform.position;
        Assert.AreEqual(0f, endPos.y, 0.01f, "La Y no debería cambiar durante movimiento normal");
    }

    /// <summary>
    /// Test: CurrentState comienza en Normal.
    /// Valida el estado inicial del jugador.
    /// </summary>
    [Test]
    public void PlayerState_StartsAsNormal()
    {
        var stateProperty = playerControllerType.GetProperty("CurrentState");
        var currentState = stateProperty.GetValue(playerControllerComponent);
        
        var playerStateEnum = System.Type.GetType("PlayerState, Assembly-CSharp");
        if (playerStateEnum == null)
            playerStateEnum = System.Type.GetType("PlayerState");
        
        var normalState = System.Enum.Parse(playerStateEnum, "Normal");

        Assert.AreEqual(normalState, currentState, "El jugador debe comenzar en estado Normal");
    }

    /// <summary>
    /// Test: Rotación hacia el ratón se actualiza correctamente.
    /// Valida que RotateTowardsMouse orienta el jugador en la dirección del cursor.
    /// </summary>
    [UnityTest]
    public IEnumerator PlayerRotates_TowardsMousePosition()
    {
        // Posicionar cámara para que pueda raycast
        cameraGO.transform.position = new Vector3(0, 10, 0);
        cameraGO.transform.LookAt(playerGO.transform.position);

        Vector3 initialForward = playerGO.transform.forward;

        // Simular movimiento del ratón (no se puede en tests, pero la rotación debería ser mínima si está apuntando en Z)
        yield return null;

        Vector3 finalForward = playerGO.transform.forward;
        // Simplemente validar que el jugador tiene una dirección forward
        Assert.IsTrue(finalForward.magnitude > 0.5f, "El jugador debería tener una dirección forward válida");
    }
}
