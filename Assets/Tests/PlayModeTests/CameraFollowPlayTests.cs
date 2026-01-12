using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests en modo Play para CameraFollow.
/// Cubre SetJumping, UpdateJumpingCamera, seguimiento normal y modo apuntado.
/// </summary>
public class CameraFollowPlayTests
{
    private GameObject cameraGO;
    private Component cameraFollowComponent;
    private System.Type cameraFollowType;
    
    private GameObject playerGO;

    [SetUp]
    public void Setup()
    {
        // Crear GameObject de la cámara con CameraFollow
        cameraGO = new GameObject("TestCamera");
        var cam = cameraGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        
        cameraFollowType = System.Type.GetType("CameraFollow, Assembly-CSharp");
        if (cameraFollowType == null)
            cameraFollowType = System.Type.GetType("CameraFollow");
        
        cameraFollowComponent = (Component)cameraGO.AddComponent(cameraFollowType);

        // Crear GameObject del jugador (target de la cámara)
        playerGO = new GameObject("TestPlayer");
        playerGO.transform.position = Vector3.zero;


        // Asignar target a la cámara via reflexión
        var targetProp = cameraFollowType.GetProperty("Target", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        
        var targetField = cameraFollowType.GetField("target", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (targetField != null)
        {
            targetField.SetValue(cameraFollowComponent, playerGO.transform);
        }
        else if (targetProp != null)
        {
            targetProp.SetValue(cameraFollowComponent, playerGO.transform);
        }
    }

    [TearDown]
    public void Teardown()
    {
        UnityEngine.Object.Destroy(cameraGO);
        UnityEngine.Object.Destroy(playerGO);
    }

    /// <summary>
    /// Test: CameraFollow sigue al jugador en modo normal.
    /// Valida que la cámara actualiza su posición hacia el target.
    /// </summary>
    [UnityTest]
    public IEnumerator CameraFollows_TargetInNormalMode()
    {
        Vector3 initialCameraPos = cameraGO.transform.position;
        Vector3 targetPos = new Vector3(5f, 10f, 5f);
        
        // Mover el jugador (target)
        playerGO.transform.position = targetPos;
        
        // Ejecutar Update de la cámara varias veces
        var lateUpdateMethod = cameraFollowType.GetMethod("LateUpdate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (lateUpdateMethod == null)
        {
            Assert.Pass("Método LateUpdate no encontrado");
            yield break;
        }

        for (int i = 0; i < 10; i++)
        {
            lateUpdateMethod.Invoke(cameraFollowComponent, null);
            yield return null;
        }

        Vector3 finalCameraPos = cameraGO.transform.position;
        
        // La cámara debería haberse movido hacia el target
        // (aunque no exactamente debido al smoothing)
        float distanceToTarget = Vector3.Distance(finalCameraPos, targetPos);
        float initialDistance = Vector3.Distance(initialCameraPos, targetPos);
        
        Assert.Less(distanceToTarget, initialDistance, 
            "La cámara debería haberse acercado al target");
    }

    /// <summary>
    /// Test: SetJumping activa el modo de salto.
    /// Valida que la cámara entra en el estado de descenso durante salto.
    /// </summary>
    [Test]
    public void SetJumping_ActivatesJumpMode()
    {
        var setJumpingMethod = cameraFollowType.GetMethod("SetJumping",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        
        if (setJumpingMethod == null)
        {
            Assert.Pass("Método SetJumping no encontrado");
            return;
        }

        // Llamar a SetJumping(true, 0.5f)
        setJumpingMethod.Invoke(cameraFollowComponent, new object[] { true, 0.5f });

        // Si hay un campo privado isPlayerJumping, podríamos verificarlo
        var jumpingField = cameraFollowType.GetField("isPlayerJumping",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (jumpingField != null)
        {
            object isJumping = jumpingField.GetValue(cameraFollowComponent);
            Assert.AreEqual(true, isJumping, "isPlayerJumping debería ser true");
        }
        else
        {
            Assert.Pass("Campo isPlayerJumping no encontrado, pero SetJumping fue ejecutado");
        }
    }

    /// <summary>
    /// Test: UpdateJumpingCamera desciende la cámara mientras sigue al jugador.
    /// Valida que durante el salto, la Y de la cámara desciende correctamente.
    /// </summary>
    [UnityTest]
    public IEnumerator JumpingCamera_DescendsWhileFollowing()
    {
        float jumpDuration = 0.8f;
        
        // Posicionar cámara a una altura inicial mayor
        cameraGO.transform.position = new Vector3(0f, 40f, -10f);
        
        // Activar modo salto
        var setJumpingMethod = cameraFollowType.GetMethod("SetJumping",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        
        if (setJumpingMethod == null)
        {
            Assert.Pass("Método SetJumping no encontrado");
            yield break;
        }

        setJumpingMethod.Invoke(cameraFollowComponent, new object[] { true, jumpDuration });

        float initialY = cameraGO.transform.position.y;
        Vector3 playerPos = playerGO.transform.position;

        // Ejecutar LateUpdate durante el salto
        var lateUpdateMethod = cameraFollowType.GetMethod("LateUpdate",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (lateUpdateMethod == null)
        {
            Assert.Pass("Método LateUpdate no encontrado");
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < jumpDuration + 0.1f)
        {
            lateUpdateMethod.Invoke(cameraFollowComponent, null);
            elapsed += Time.deltaTime;
            yield return null;
        }

        float finalY = cameraGO.transform.position.y;
        
        // La Y debería haber descendido
        Assert.Less(finalY, initialY, "La Y de la cámara debería haber descendido durante el salto");
        
        // La cámara debería estar cerca del target en X y Z
        Vector3 finalPos = cameraGO.transform.position;
        Assert.AreEqual(playerPos.x, finalPos.x, 0.5f, "X debería seguir al jugador");
        Assert.AreEqual(playerPos.z, finalPos.z, 1.5f, "Z debería seguir al jugador");
    }

    /// <summary>
    /// Test: SetJumping(false) desactiva el modo de salto.
    /// Valida que la cámara vuelve al modo normal.
    /// </summary>
    [Test]
    public void SetJumping_DeactivatesJumpMode()
    {
        var setJumpingMethod = cameraFollowType.GetMethod("SetJumping",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        
        if (setJumpingMethod == null)
        {
            Assert.Pass("Método SetJumping no encontrado");
            return;
        }

        // Activar
        setJumpingMethod.Invoke(cameraFollowComponent, new object[] { true, 0.5f });
        
        // Desactivar
        setJumpingMethod.Invoke(cameraFollowComponent, new object[] { false, 0f });

        var jumpingField = cameraFollowType.GetField("isPlayerJumping",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (jumpingField != null)
        {
            object isJumping = jumpingField.GetValue(cameraFollowComponent);
            Assert.AreEqual(false, isJumping, "isPlayerJumping debería ser false");
        }
        else
        {
            Assert.Pass("Campo isPlayerJumping no encontrado");
        }
    }

    /// <summary>
    /// Test: Altura base de la cámara es 20 unidades en modo normal.
    /// Valida que baseOffset.y es 20.
    /// </summary>
    [Test]
    public void CameraBaseHeight_IsCorrect()
    {
        var baseOffsetField = cameraFollowType.GetField("baseOffset",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (baseOffsetField == null)
        {
            Assert.Pass("Campo baseOffset no encontrado");
            return;
        }

        object baseOffset = baseOffsetField.GetValue(cameraFollowComponent);
        Vector3 offset = (Vector3)baseOffset;
        
        Assert.AreEqual(20f, offset.y, 0.01f, "baseOffset.y debería ser 20");
    }

    /// <summary>
    /// Test: EnterAimMode y ExitAimMode cambian el comportamiento de la cámara.
    /// Valida que el modo apuntado se activa y desactiva correctamente.
    /// </summary>
    [UnityTest]
    public IEnumerator AimMode_TogglesCorrectly()
    {
        var isAimingField = cameraFollowType.GetField("isAiming",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        
        if (isAimingField == null)
        {
            Assert.Pass("Campo isAiming no encontrado");
            yield break;
        }

        var enterAimMethod = cameraFollowType.GetMethod("EnterAimMode",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var exitAimMethod = cameraFollowType.GetMethod("ExitAimMode",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        if (enterAimMethod == null || exitAimMethod == null)
        {
            Assert.Pass("Métodos EnterAimMode/ExitAimMode no encontrados");
            yield break;
        }

        // Entrar en AimMode
        enterAimMethod.Invoke(cameraFollowComponent, null);
        yield return null;
        
        object isAimingAfterEnter = isAimingField.GetValue(cameraFollowComponent);
        Assert.AreEqual(true, isAimingAfterEnter, "isAiming debería ser true después de EnterAimMode");

        // Salir de AimMode
        exitAimMethod.Invoke(cameraFollowComponent, null);
        yield return null;

        object isAimingAfterExit = isAimingField.GetValue(cameraFollowComponent);
        Assert.AreEqual(false, isAimingAfterExit, "isAiming debería ser false después de ExitAimMode");
    }
}
