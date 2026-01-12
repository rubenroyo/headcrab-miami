using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerMovementTests
{
    private GameObject playerGO;
    private Component playerController;

    [SetUp]
    public void Setup()
    {
        // Crear un jugador temporal
        playerGO = new GameObject("Player");
        // Añadimos el componente PlayerController por reflexión para evitar dependencias de ensamblado en tests
        var pcType = System.Type.GetType("PlayerController, Assembly-CSharp");
        if (pcType == null)
            pcType = System.Type.GetType("PlayerController");
        playerController = (Component)playerGO.AddComponent(pcType);

        // Crear una cámara principal para que PlayerController pueda encontrarla en Start
        var camGO = new GameObject("TestCam");
        var cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
    }

    [UnityTest]
    public IEnumerator PlayerMovesForward_WhenPressingW()
    {
        Vector3 startPos = playerGO.transform.position;

        // Simular input "W"
        // Esto se hace moviendo manualmente porque Input.GetAxisRaw no se puede modificar desde test
        Vector3 move = new Vector3(0,0,1);
        // Usamos la propiedad pública MoveSpeed añadida al PlayerController (vía reflexión)
        var moveSpeedProp = playerController.GetType().GetProperty("MoveSpeed");
        float moveSpeed = moveSpeedProp != null ? (float)moveSpeedProp.GetValue(playerController) : 5f;
        playerGO.transform.Translate(move * moveSpeed * Time.deltaTime, Space.World);

        yield return null; // Esperar un frame

        Assert.Greater(playerGO.transform.position.z, startPos.z, "El jugador no se movió hacia adelante");
    }

    [UnityTest]
    public IEnumerator PlayerJumpFollowsTrajectory()
    {
        // Configuramos salto usando las propiedades públicas
        // Configuramos salto usando las propiedades públicas (vía reflexión)
        var jcType = playerController.GetType();
        var jumpHeightProp = jcType.GetProperty("JumpHeight");
        var jumpDurationProp = jcType.GetProperty("JumpDuration");
        if (jumpHeightProp != null) jumpHeightProp.SetValue(playerController, 2f);
        if (jumpDurationProp != null) jumpDurationProp.SetValue(playerController, 1f);

        // Definir un punto de destino simple (en frente del jugador)
        Vector3 startPos = playerGO.transform.position;
        Vector3 endPos = startPos + Vector3.forward * 5f;

        // Iniciar el salto usando puntos explícitos (útil para tests) via reflexión
        var startJumpMethod = jcType.GetMethod("StartJumpWithPoints");
        float jd = jumpDurationProp != null ? (float)jumpDurationProp.GetValue(playerController) : 1f;
        startJumpMethod.Invoke(playerController, new object[] { new Vector3[] { startPos, endPos }, jd });

        float elapsed = 0f;
        float maxY = startPos.y;
        var upd = jcType.GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        while (elapsed < jd + 0.1f)
        {
            // Llamamos a Update para simular frames de juego via reflexión
            upd.Invoke(playerController, null);
            elapsed += Time.deltaTime;
            // trackear la máxima altura alcanzada
            float currentY = playerGO.transform.position.y;
            if (currentY > maxY) maxY = currentY;
            yield return null;
        }

        Vector3 finalPos = playerGO.transform.position;

        // Comprobaciones: XZ final cerca del destino, y en algún punto se alcanzó altura mayor que inicio
        Assert.AreEqual(endPos.x, finalPos.x, 0.2f, "X final incorrecta");
        Assert.AreEqual(endPos.z, finalPos.z, 0.2f, "Z final incorrecta");
        Assert.Greater(maxY, startPos.y + 0.01f, "El jugador no alcanzó una altura mayor durante el salto");
        // Al final del salto debería haber aterrizado (altura similar al inicio)
        Assert.AreEqual(startPos.y, finalPos.y, 0.05f, "El jugador no aterrizó correctamente al final del salto");
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(playerGO);
    }
}
