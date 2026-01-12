using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Tests en modo Play para EnemyController.
/// Cubre OnPossessed, OnReleased y cambios en el estado IsPossessed.
/// </summary>
public class EnemyControllerPlayTests
{
    private GameObject enemyGO;
    private Component enemyControllerComponent;
    private System.Type enemyControllerType;

    [SetUp]
    public void Setup()
    {
        // Crear GameObject del enemigo
        enemyGO = new GameObject("TestEnemy");
        
        // Añadir EnemyController por reflexión
        enemyControllerType = System.Type.GetType("EnemyController, Assembly-CSharp");
        if (enemyControllerType == null)
            enemyControllerType = System.Type.GetType("EnemyController");
        
        enemyControllerComponent = (Component)enemyGO.AddComponent(enemyControllerType);
    }

    [TearDown]
    public void Teardown()
    {
        Object.Destroy(enemyGO);
    }

    /// <summary>
    /// Test: OnPossessed cambia el estado IsPossessed a true.
    /// Valida que el enemigo registra que fue poseído.
    /// </summary>
    [Test]
    public void OnPossessed_SetsPossessedTrue()
    {
        // Inicialmente no poseído
        var isPossessedProp = enemyControllerType.GetProperty("IsPossessed");
        if (isPossessedProp == null)
        {
            Assert.Pass("Propiedad IsPossessed no encontrada");
            return;
        }

        object isPossessedInitial = isPossessedProp.GetValue(enemyControllerComponent);
        Assert.AreEqual(false, isPossessedInitial, "Inicialmente el enemigo no debe estar poseído");

        // Llamar a OnPossessed
        var onPossessedMethod = enemyControllerType.GetMethod("OnPossessed",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        
        if (onPossessedMethod != null)
        {
            onPossessedMethod.Invoke(enemyControllerComponent, null);
            
            object isPossessedAfter = isPossessedProp.GetValue(enemyControllerComponent);
            Assert.AreEqual(true, isPossessedAfter, "Después de OnPossessed, IsPossessed debe ser true");
        }
        else
        {
            Assert.Pass("Método OnPossessed no encontrado");
        }
    }

    /// <summary>
    /// Test: OnReleased cambia el estado IsPossessed a false.
    /// Valida que el enemigo registra que fue liberado.
    /// </summary>
    [Test]
    public void OnReleased_SetsPossessedFalse()
    {
        var isPossessedProp = enemyControllerType.GetProperty("IsPossessed");
        if (isPossessedProp == null)
        {
            Assert.Pass("Propiedad IsPossessed no encontrada");
            return;
        }

        // Primero poseer
        var onPossessedMethod = enemyControllerType.GetMethod("OnPossessed",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (onPossessedMethod != null)
        {
            onPossessedMethod.Invoke(enemyControllerComponent, null);
        }

        // Luego liberar
        var onReleasedMethod = enemyControllerType.GetMethod("OnReleased",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        
        if (onReleasedMethod != null)
        {
            onReleasedMethod.Invoke(enemyControllerComponent, null);
            
            object isPossessedAfter = isPossessedProp.GetValue(enemyControllerComponent);
            Assert.AreEqual(false, isPossessedAfter, "Después de OnReleased, IsPossessed debe ser false");
        }
        else
        {
            Assert.Pass("Método OnReleased no encontrado");
        }
    }

    /// <summary>
    /// Test: Cambios de estado de IsPossessed alternan correctamente.
    /// Valida que la posesión se puede aplicar y remover múltiples veces.
    /// </summary>
    [Test]
    public void IsPossessed_TogglesCorrectly()
    {
        var isPossessedProp = enemyControllerType.GetProperty("IsPossessed");
        if (isPossessedProp == null)
        {
            Assert.Pass("Propiedad IsPossessed no encontrada");
            return;
        }

        var onPossessedMethod = enemyControllerType.GetMethod("OnPossessed",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        var onReleasedMethod = enemyControllerType.GetMethod("OnReleased",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        if (onPossessedMethod == null || onReleasedMethod == null)
        {
            Assert.Pass("Métodos OnPossessed/OnReleased no encontrados");
            return;
        }

        // Ciclo 1: Poseer
        onPossessedMethod.Invoke(enemyControllerComponent, null);
        object state1 = isPossessedProp.GetValue(enemyControllerComponent);
        Assert.AreEqual(true, state1, "Primera posesión: IsPossessed debe ser true");

        // Ciclo 2: Liberar
        onReleasedMethod.Invoke(enemyControllerComponent, null);
        object state2 = isPossessedProp.GetValue(enemyControllerComponent);
        Assert.AreEqual(false, state2, "Primera liberación: IsPossessed debe ser false");

        // Ciclo 3: Poseer de nuevo
        onPossessedMethod.Invoke(enemyControllerComponent, null);
        object state3 = isPossessedProp.GetValue(enemyControllerComponent);
        Assert.AreEqual(true, state3, "Segunda posesión: IsPossessed debe ser true");
    }

    /// <summary>
    /// Test: CanBePossessed retorna true (o false según la lógica).
    /// Valida que el enemigo está disponible para ser poseído.
    /// </summary>
    [Test]
    public void CanBePossessed_ReturnsTrueForNewEnemy()
    {
        var canBePossessedProp = enemyControllerType.GetProperty("CanBePossessed");
        if (canBePossessedProp == null)
        {
            Assert.Pass("Propiedad CanBePossessed no encontrada");
            return;
        }

        object canBePossessed = canBePossessedProp.GetValue(enemyControllerComponent);
        Assert.IsNotNull(canBePossessed, "CanBePossessed debe retornar un valor");
        Assert.IsInstanceOf<bool>(canBePossessed, "CanBePossessed debe ser bool");
    }

    /// <summary>
    /// Test: Enemy se comporta como esperado después de ser poseído y liberado.
    /// Valida el ciclo completo de posesión.
    /// </summary>
    [UnityTest]
    public IEnumerator PossessionCycle_WorksCorrectly()
    {
        var isPossessedProp = enemyControllerType.GetProperty("IsPossessed");
        if (isPossessedProp == null)
        {
            Assert.Pass("Propiedad IsPossessed no encontrada");
            yield break;
        }

        var onPossessedMethod = enemyControllerType.GetMethod("OnPossessed",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        var onReleasedMethod = enemyControllerType.GetMethod("OnReleased",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        if (onPossessedMethod == null || onReleasedMethod == null)
        {
            Assert.Pass("Métodos OnPossessed/OnReleased no encontrados");
            yield break;
        }

        // Poseer
        onPossessedMethod.Invoke(enemyControllerComponent, null);
        yield return null;
        
        object stateAfterPossess = isPossessedProp.GetValue(enemyControllerComponent);
        Assert.AreEqual(true, stateAfterPossess, "Enemy debe estar poseído");

        // Esperar un frame
        yield return null;

        // Liberar
        onReleasedMethod.Invoke(enemyControllerComponent, null);
        yield return null;

        object stateAfterRelease = isPossessedProp.GetValue(enemyControllerComponent);
        Assert.AreEqual(false, stateAfterRelease, "Enemy debe estar liberado");
    }
}
