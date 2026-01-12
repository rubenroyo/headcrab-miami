# Possessed Enemy Movement Implementation

## ✅ Cambios Implementados

### 1. **PlayerController.cs**

#### Campo Serializado Nuevo (Línea 27)
```csharp
[SerializeField] private float possessedEnemyMoveSpeed = 4f; // Velocidad del enemigo cuando se le posee
```

**Ajustes en el Editor:**
- En el Inspector de PlayerController, verás un nuevo campo: **"Possessed Enemy Move Speed"**
- Valor por defecto: **4f** (puedes ajustarlo según el balance del juego)
- Este valor controla la velocidad del enemigo poseído (independiente de moveSpeed)

#### Método UpdatePossessing() (Línea 181)
**Cambios:**
- Reemplazó `HandleMovement()` por `HandlePossessedMovement()`
- Ahora el enemigo se mueve, no el Player

**Lógica:**
```csharp
// Mover al enemigo poseído
HandlePossessedMovement();

// Player se "sube" sobre el enemigo
Vector3 pos = possessedEnemy.transform.position + Vector3.up * 3f;
transform.position = pos;

// Rotar hacia el mouse (igual que el jugador normal)
RotateTowardsMouse();
```

#### Nuevo Método HandlePossessedMovement() (Línea 309)
```csharp
void HandlePossessedMovement()
{
    if (possessedEnemy == null) return;

    float h = Input.GetAxisRaw("Horizontal");
    float v = Input.GetAxisRaw("Vertical");

    Vector3 move = new Vector3(h, 0f, v).normalized;
    possessedEnemy.transform.Translate(move * possessedEnemyMoveSpeed * Time.deltaTime, Space.World);
}
```

**Funcionamiento:**
- Lee entrada WASD (Horizontal/Vertical)
- Calcula vector de movimiento normalizado
- Aplica velocidad `possessedEnemyMoveSpeed` al enemigo
- El Player sigue al enemigo automáticamente (posicionado 3 unidades arriba)

### 2. **Comportamiento en Gameplay**

#### Estado Normal
- Player se mueve con WASD a velocidad `moveSpeed` (5f)
- Cámara sigue al Player

#### Estado Possessing (Poseído)
- Player POSEE al enemigo
- WASD mueve al ENEMIGO a velocidad `possessedEnemyMoveSpeed` (4f)
- Player se mantiene 3 unidades arriba del enemigo
- Cámara se enfoca en el enemigo (sigue su rotación)
- Mouse rota tanto al Player como al enemigo (ambos giran igual)
- Click izquierdo + Right-click = desmontaje (salto)

## 📋 Controles Durante Posesión

| Input | Acción |
|-------|--------|
| **W/A/S/D** | Mover al enemigo poseído |
| **Mouse Movement** | Rotar al enemigo (yaw) |
| **Right-click** | Entrar modo aiming/apuntado |
| **Left-click + Right-click** | Desmontar (salto) |

## ⚙️ Ajustes en el Editor

### En el Inspector de PlayerController

**Sección "Movimiento":**
- `Move Speed`: Velocidad del jugador normal (default: 5f)

**Sección "Posesión":**
- `Possession Cooldown`: Tiempo de inmunidad tras desmontar (default: 0.5s)
- **`Possessed Enemy Move Speed`**: ⭐ **NUEVO** - Velocidad del enemigo poseído (default: 4f)

### Recomendaciones de Balance

| Escenario | Velocidad Sugerida |
|-----------|-------------------|
| Enemigo lento/pesado | 2-3 |
| Enemigo normal | 4-5 |
| Enemigo rápido/ágil | 6-8 |

Puedes ajustar este valor en el Inspector sin recompilar el código.

## 🧪 Test Coverage

Se agregó un nuevo test:
- **PossessedEnemy_MovesWithWASD()** en `PlayerControllerPossessionPlayTests.cs`
- Valida que el Player se mantiene correctamente posicionado sobre el enemigo poseído

## 📝 Notas Técnicas

### Rotación
- La rotación usa `RotateTowardsMouse()` que funciona igual para ambos:
  - Traza un raycast desde la cámara
  - Calcula el punto en el suelo
  - Rota al Player (que lleva al enemigo visualmente)

### Posicionamiento
- `transform.position = enemyGO.transform.position + Vector3.up * 3f;`
- El Player sigue al enemigo automáticamente
- La cámara sigue al Player (o al enemigo en este caso)

### Física
- El enemigo se mueve con `Translate()` en World Space
- Sin fricción ni gravedad adicional
- Compatible con colliders del enemigo (si tiene)

## 🔄 Flujo de Movimiento

```
Input (WASD)
    ↓
HandlePossessedMovement()
    ↓
possessedEnemy.transform.Translate(move * speed)
    ↓
Enemy Position Updated
    ↓
Player.position = enemy.position + 3 up
    ↓
Camera follows Player
    ↓
Visual Result: Enemy moves with Player on top
```

## ✨ Próximos Pasos

Una vez verificado en el editor:
1. Prueba el movimiento del enemigo poseído en gameplay
2. Ajusta `possessedEnemyMoveSpeed` según balance deseado
3. Continuar con implementación del sistema de armas (RF3)

---

**No se requieren cambios adicionales en el código. Solo revisar el Inspector si deseas ajustar la velocidad del enemigo.**
