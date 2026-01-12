# Test Suite para Headcrab Miami - Unity 6.3 LTS

## Overview

Suite completa de tests automáticos para el proyecto Headcrab Miami usando NUnit y Unity Test Runner. Cubre todas las clases principales del juego con tests en modo **EditMode** y **PlayMode**.

## Estructura de Tests

```
Assets/
├── Tests/
│   ├── EditModeTests/
│   │   ├── EditModeTests.asmdef
│   │   └── TrajectoryPreviewEditTests.cs
│   │       └── Tests funciones puras sin dependencias de físicas
│   │
│   └── PlayModeTests/
│       ├── PlayModeTests.asmdef
│       ├── PlayerMovementTests.cs (tests existentes)
│       ├── PlayerControllerMovementPlayTests.cs
│       ├── PlayerControllerJumpPlayTests.cs
│       ├── PlayerControllerPossessionPlayTests.cs
│       ├── EnemyControllerPlayTests.cs
│       └── CameraFollowPlayTests.cs
```

## Tests por Clase

### 1. TrajectoryPreview (EditMode)

**Archivo:** `TrajectoryPreviewEditTests.cs`

Tests conceptuales que validan:
- ✅ Cálculo de puntos sin obstáculos
- ✅ Obtención del punto final de trayectoria
- ✅ Cálculo de distancia total
- ✅ Interpolación a lo largo de la trayectoria
- ✅ Toggle del LineRenderer (activar/desactivar)
- ✅ Cálculo de rebotes (reflexión)

**Por qué EditMode:** No necesita físicas ni escena completa, solo validación de lógica matemática.

---

### 2. PlayerController - Movimiento (PlayMode)

**Archivo:** `PlayerControllerMovementPlayTests.cs`

Tests que validan:
- ✅ Movimiento hacia la derecha (eje X)
- ✅ Movimiento hacia adelante (eje Z)
- ✅ Movimiento solo en plano XZ (Y no cambia)
- ✅ Estado inicial es Normal
- ✅ Rotación hacia la posición del ratón

**Métodos cubiertos:** `UpdateNormal`, `HandleMovement`, `RotateTowardsMouse`

---

### 3. PlayerController - Salto (PlayMode)

**Archivo:** `PlayerControllerJumpPlayTests.cs`

Tests que validan:
- ✅ Ejecución de parábola correcta (altura máxima y caída)
- ✅ Seguimiento de trayectoria con múltiples puntos
- ✅ Cambio de estado a Jumping
- ✅ Altura máxima proporcional a `JumpHeight`
- ✅ Duración del salto respeta `JumpDuration`

**Métodos cubiertos:** `StartJump`, `StartJumpWithPoints`, `UpdateJump`, `EndJump`, `CalculateTotalTrajectoryDistance`, `GetPositionAlongTrajectory`

---

### 4. PlayerController - Posesión (PlayMode)

**Archivo:** `PlayerControllerPossessionPlayTests.cs`

Tests que validan:
- ✅ Cambio de estado a Possessing al poseer enemigo
- ✅ Posición del jugador sobre el enemigo (3 unidades arriba)
- ✅ ReleaseEnemy devuelve estado a Normal
- ✅ Desmontar ejecuta parábola desde el enemigo
- ✅ EndDismount devuelve estado a Normal

**Métodos cubiertos:** `UpdatePossessing`, `PossessEnemy`, `ReleaseEnemy`, `StartDismount`, `EndDismount`, `CheckPossessionCollision`

---

### 5. EnemyController (PlayMode)

**Archivo:** `EnemyControllerPlayTests.cs`

Tests que validan:
- ✅ OnPossessed cambia `IsPossessed` a true
- ✅ OnReleased cambia `IsPossessed` a false
- ✅ Toggle correcto entre poseído/liberado
- ✅ `CanBePossessed` retorna valor válido
- ✅ Ciclo completo de posesión funciona

**Métodos cubiertos:** `OnPossessed`, `OnReleased`, propiedades `IsPossessed`, `CanBePossessed`

---

### 6. CameraFollow (PlayMode)

**Archivo:** `CameraFollowPlayTests.cs`

Tests que validan:
- ✅ Cámara sigue al jugador en modo normal
- ✅ SetJumping activa modo de salto
- ✅ Cámara desciende mientras sigue al jugador durante salto
- ✅ SetJumping(false) desactiva modo de salto
- ✅ Altura base de cámara es 20 unidades
- ✅ EnterAimMode y ExitAimMode funcionan correctamente

**Métodos cubiertos:** `SetJumping`, `UpdateJumpingCamera`, `LateUpdate`, `EnterAimMode`, `ExitAimMode`

---

## Cómo Ejecutar los Tests

### En Unity Editor

1. **Abrir Test Runner:**
   - `Window → Testing → Test Runner`

2. **EditMode Tests:**
   - Click en pestaña "EditMode"
   - Click en "Run All" para ejecutar todos los tests

3. **PlayMode Tests:**
   - Click en pestaña "PlayMode"
   - Click en "Run All" para ejecutar todos los tests
   - **Nota:** PlayMode tests necesitan que el juego esté en modo play

### Ejecución desde Terminal (CI/CD)

```bash
# Ejecutar todos los tests en modo EditMode
unity -projectPath . -runTests -testPlatform editmode -batchmode -nographics

# Ejecutar todos los tests en modo PlayMode
unity -projectPath . -runTests -testPlatform playmode -batchmode -nographics

# Ejecutar con reporte XML (para CI/CD)
unity -projectPath . -runTests -testPlatform editmode -batchmode -nographics -testResults ./test-results.xml
```

---

## Cobertura de Tests

Según el reporte de cobertura actual:

| Clase | Métodos Cubiertos | Cobertura |
|-------|-------------------|-----------|
| **PlayerController** | 20/30 | 66.6% |
| **CameraFollow** | 1/10 | 10% |
| **EnemyController** | 1/5 | 20% |
| **TrajectoryPreview** | 1/6 | 16.6% |

**Estos tests aumentarán significativamente la cobertura.**

---

## Características Especiales

### Uso de Reflexión
Como los tests están en un ensamblado separado, se usa reflexión para acceder a métodos y propiedades privadas:

```csharp
var playerControllerType = System.Type.GetType("PlayerController, Assembly-CSharp");
var updateMethod = playerControllerType.GetMethod("Update", 
    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
updateMethod.Invoke(playerControllerComponent, null);
```

### GameObject Cleanup
Todos los tests limpian sus GameObjects en `[TearDown]` para evitar contaminar tests posteriores:

```csharp
[TearDown]
public void Teardown()
{
    Object.Destroy(playerGO);
    Object.Destroy(cameraGO);
}
```

### Async/Timing
PlayMode tests usan `[UnityTest]` y `IEnumerator` para manejar frames:

```csharp
[UnityTest]
public IEnumerator Test_Name()
{
    // Código...
    yield return null; // Esperar un frame
    // Assertions...
}
```

---

## Próximos Pasos Recomendados

1. **Ejecutar los tests en Unity Test Runner**
2. **Revisar fallos específicos** y ajustar tolerancias si es necesario
3. **Aumentar cobertura** de métodos privados exponiendo métodos públicos de prueba si es necesario
4. **Integración CI/CD:** Configurar pipeline para ejecutar tests automáticamente en cada push
5. **Métricas de cobertura:** Generar reportes de cobertura con OpenCover o similares

---

## Notas Importantes

- Los tests usan **reflexión** para evitar dependencias directas entre assemblies (test assembly != game assembly)
- **PlayMode tests** requieren que haya una `MainCamera` en la escena (se crea en `Setup`)
- **EditMode tests** son puros y no dependen del motor de físicas
- Todos los tests son **independientes** y pueden ejecutarse en cualquier orden

---

## Referencias

- [Unity Test Framework Documentation](https://docs.unity3d.com/2023.2/Documentation/Manual/testing-editortestsrunner.html)
- [NUnit Documentation](https://docs.nunit.org/)
- [Game Programming Patterns - State Pattern](https://gameprogrammingpatterns.com/state.html)

