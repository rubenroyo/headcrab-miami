# 📋 Headcrab Miami - Planificación TFG (HackNPlan)

## 🎯 Alcance del Proyecto
- Menú principal
- Selector de niveles (3 niveles)
- Opciones
- Créditos
- Gameplay completo con IA enemiga, armas y posesión

---

# 🏃 SPRINT 1 - Núcleo del Gameplay (COMPLETADO)

## 📁 Categoría: Programming

### Tarea 1.1: Sistema de Movimiento del Jugador
| Campo | Valor |
|-------|-------|
| **Tag** | Core, Player |
| **Importance** | Must Have |
| **Estimated** | 8h |
| **Logged** | 8h |
| **Description** | Implementar sistema de movimiento isométrico con CharacterController. Incluye rotación hacia cursor, velocidad configurable y sistema de estados. |
| **Subtasks** | ✅ Movimiento WASD ✅ Rotación hacia cursor ✅ Integración con CharacterController ✅ Máquina de estados (Normal, Aiming, Jumping, Possessing) |

### Tarea 1.2: Sistema de Cámara
| Campo | Valor |
|-------|-------|
| **Tag** | Core, Camera |
| **Importance** | Must Have |
| **Estimated** | 6h |
| **Logged** | 6h |
| **Description** | Cámara con modos TopDown e Isométrico, pixel snapping, smoothing y modo apuntado con offset. |
| **Subtasks** | ✅ Modo TopDown (45°, 90°, 45°) ✅ Modo Isométrico (30°, 135°, 45°) ✅ Pixel snapping para pixel art ✅ Modo apuntado con offset hacia cursor ✅ Animaciones suaves con DOTween |

### Tarea 1.3: Sistema de Salto en Dos Fases
| Campo | Valor |
|-------|-------|
| **Tag** | Core, Player |
| **Importance** | Must Have |
| **Estimated** | 4h |
| **Logged** | 4h |
| **Description** | Implementar salto realista con fase ascendente y descendente independientes. Gravedad aumentada en descenso. |
| **Subtasks** | ✅ Fase ascendente con velocidad inicial ✅ Fase descendente con gravedad aumentada (2x) ✅ Detección de aterrizaje con raycast ✅ Integración con CharacterController.Move() |

### Tarea 1.4: Sistema de Posesión
| Campo | Valor |
|-------|-------|
| **Tag** | Core, Player, Enemy |
| **Importance** | Must Have |
| **Estimated** | 10h |
| **Logged** | 10h |
| **Description** | Permitir al jugador saltar sobre enemigos y poseerlos, controlando su cuerpo. Incluye transferencia entre cuerpos. |
| **Subtasks** | ✅ Detección de enemigo debajo al caer ✅ Transición a estado poseído ✅ Suspensión de IA y ragdoll ✅ Control del cuerpo poseído ✅ Eyección con Space+RightClick ✅ Transferencia a otro enemigo |

### Tarea 1.5: Sistema de Armas - Estructura Base
| Campo | Valor |
|-------|-------|
| **Tag** | Core, Weapons |
| **Importance** | Must Have |
| **Estimated** | 8h |
| **Logged** | 8h |
| **Description** | Crear arquitectura del sistema de armas con ScriptableObjects y enums. |
| **Subtasks** | ✅ Enum WeaponType (Pistol, Rifle, Shotgun, Knife) ✅ ScriptableObject WeaponData (daño, cadencia, capacidad, dispersión) ✅ Sistema de munición (actual/cargador) ✅ Lógica de recarga automática |

### Tarea 1.6: Sistema de Inventario (InventoryHolder)
| Campo | Valor |
|-------|-------|
| **Tag** | Core, Weapons |
| **Importance** | Must Have |
| **Estimated** | 6h |
| **Logged** | 6h |
| **Description** | Componente que gestiona el arma equipada y munición para cualquier entidad (jugador/enemigo). |
| **Subtasks** | ✅ Referencia a WeaponData equipado ✅ Tracking de munición actual y cargadores ✅ Método TryFire() con cooldown ✅ Método Reload() ✅ Método EquipWeapon() |

### Tarea 1.7: Sistema de Balas
| Campo | Valor |
|-------|-------|
| **Tag** | Core, Weapons |
| **Importance** | Must Have |
| **Estimated** | 4h |
| **Logged** | 3h |
| **Description** | Proyectiles con movimiento lineal, pool de objetos y colisiones. |
| **Subtasks** | ✅ Movimiento cinemático (sin física real) ✅ Colisión con paredes ✅ Colisión con enemigos ⚠️ Sistema de daño (TODO) ✅ Timeout automático (3s) |

---

## 📁 Categoría: Art

### Tarea 1.8: Configuración de Pixel Art
| Campo | Valor |
|-------|-------|
| **Tag** | Visual, Core |
| **Importance** | Must Have |
| **Estimated** | 4h |
| **Logged** | 4h |
| **Description** | Configurar renderizado pixel art con resolución 480x270 escalada a 1920x1080. |
| **Subtasks** | ✅ RenderTexture con filtro Point ✅ SettingsController singleton ✅ Sistema de pixelSize configurable (1-10) ✅ Sub-pixel scrolling para movimiento fluido |

### Tarea 1.9: Post-Proceso Edge Detection
| Campo | Valor |
|-------|-------|
| **Tag** | Visual, PostProcess |
| **Importance** | Should Have |
| **Estimated** | 6h |
| **Logged** | 6h |
| **Description** | Custom ScriptableRendererFeature para URP que detecta bordes usando depth y normales. |
| **Subtasks** | ✅ Detección por profundidad (depth threshold) ✅ Detección por normales ✅ Colorización de bordes con hue shift ✅ Integración con Render Graph API |

---

## 📁 Categoría: Design

### Tarea 1.10: Diseño del Sistema de Estados del Jugador
| Campo | Valor |
|-------|-------|
| **Tag** | GDD, Player |
| **Importance** | Must Have |
| **Estimated** | 2h |
| **Logged** | 2h |
| **Description** | Documentar máquina de estados del jugador con transiciones. |
| **Subtasks** | ✅ Diagrama de estados ✅ Condiciones de transición ✅ Comportamientos por estado |

---

# 🏃 SPRINT 2 - IA Enemiga y Combate (COMPLETADO)

## 📁 Categoría: Programming

### Tarea 2.1: Sistema de IA Base con NavMesh
| Campo | Valor |
|-------|-------|
| **Tag** | AI, Enemy |
| **Importance** | Must Have |
| **Estimated** | 10h |
| **Logged** | 10h |
| **Description** | Implementar IA con NavMeshAgent, máquina de 9 estados y rotación manual. |
| **Subtasks** | ✅ Configuración NavMeshAgent (aceleración instantánea 999) ✅ Estados: Idle, Wandering, Patrolling, Chasing, CombatEngaged, Investigating, Searching, Returning, SeekingItem ✅ Rotación manual hacia destino ✅ Debug visual de estados |

### Tarea 2.2: Sistema de Visión Enemiga
| Campo | Valor |
|-------|-------|
| **Tag** | AI, Enemy |
| **Importance** | Must Have |
| **Estimated** | 6h |
| **Logged** | 6h |
| **Description** | Implementar cono de visión con detección de jugador y ítems. |
| **Subtasks** | ✅ Parámetros: viewRadius (10), viewAngle (120°), detectionRadius (15) ✅ Raycast para line-of-sight ✅ Layer mask para obstáculos ✅ Detección de ítems en cono de visión |

### Tarea 2.3: Sistema de Patrulla
| Campo | Valor |
|-------|-------|
| **Tag** | AI, Enemy |
| **Importance** | Must Have |
| **Estimated** | 4h |
| **Logged** | 4h |
| **Description** | Rutas de patrulla con waypoints y tiempos de espera. |
| **Subtasks** | ✅ Lista de Transform para waypoints ✅ Índice cíclico de patrulla ✅ Tiempo de espera en cada punto (2s) ✅ Debug visual de rutas con Gizmos |

### Tarea 2.4: Sistema de Búsqueda
| Campo | Valor |
|-------|-------|
| **Tag** | AI, Enemy |
| **Importance** | Should Have |
| **Estimated** | 4h |
| **Logged** | 4h |
| **Description** | Comportamiento de búsqueda cuando pierde de vista al jugador. |
| **Subtasks** | ✅ Estado Investigating (va a última posición conocida) ✅ Estado Searching (gira buscando) ✅ Duración de búsqueda configurable (5s) ✅ Retorno a patrulla si no encuentra |

### Tarea 2.5: Sistema de Prioridades
| Campo | Valor |
|-------|-------|
| **Tag** | AI, Enemy |
| **Importance** | Must Have |
| **Estimated** | 6h |
| **Logged** | 6h |
| **Description** | Implementar enum de prioridades para decidir objetivo del enemigo. |
| **Subtasks** | ✅ Enum TargetPriority (Weapon=1, Magazine=2, WeaponWithAmmo=3, Player=4, Patrol=5) ✅ Evaluación continua de prioridades ✅ Recogida de ítems con visión (no radio) ✅ Sistema de compromiso con objetivo |

### Tarea 2.6: Sistema de Combate Enemigo
| Campo | Valor |
|-------|-------|
| **Tag** | AI, Enemy, Combat |
| **Importance** | Must Have |
| **Estimated** | 8h |
| **Logged** | 8h |
| **Description** | Estado CombatEngaged donde el enemigo armado mantiene distancia y dispara. |
| **Subtasks** | ✅ Distancia óptima configurable (4-6 unidades) ✅ Reposicionamiento si está muy cerca/lejos ✅ Rotación hacia jugador constante ✅ TryShootAtPlayer() con line-of-sight ✅ Dispersión de disparo |

### Tarea 2.7: Sistema de Recogida de Ítems
| Campo | Valor |
|-------|-------|
| **Tag** | AI, Items |
| **Importance** | Must Have |
| **Estimated** | 6h |
| **Logged** | 6h |
| **Description** | Enemigos detectan y recogen armas/cargadores visibles. |
| **Subtasks** | ✅ Detección de WeaponPickup en cono de visión ✅ Detección de MagazinePickup ✅ Prioridad: Arma > Cargador > Jugador ✅ OnTriggerEnter para recoger ✅ Equipamiento automático del arma |

### Tarea 2.8: Pickups de Armas y Cargadores
| Campo | Valor |
|-------|-------|
| **Tag** | Items, Weapons |
| **Importance** | Must Have |
| **Estimated** | 4h |
| **Logged** | 4h |
| **Description** | Objetos recogibles que otorgan armas o munición. |
| **Subtasks** | ✅ WeaponPickup con referencia a WeaponData ✅ MagazinePickup con munición extra ✅ Modelo visual del arma ✅ Trigger collider para detección |

### Tarea 2.9: Controller del Enemigo Poseído
| Campo | Valor |
|-------|-------|
| **Tag** | Enemy, Possession |
| **Importance** | Must Have |
| **Estimated** | 6h |
| **Logged** | 6h |
| **Description** | EnemyController.cs que maneja al enemigo cuando está poseído vs libre. |
| **Subtasks** | ✅ Flags isPossessed, isUnderPlayerControl ✅ HandlePossession()/HandleUnpossession() ✅ Suspensión de NavMeshAgent ✅ Movimiento manual cuando poseído ✅ Gravedad propia (no CharacterController) |

---

## 📁 Categoría: Art

### Tarea 2.10: UI de Trayectoria (Dots)
| Campo | Valor |
|-------|-------|
| **Tag** | UI, Visual |
| **Importance** | Should Have |
| **Estimated** | 4h |
| **Logged** | 4h |
| **Description** | Visualización de la trayectoria del salto con puntos que siguen una parábola. |
| **Subtasks** | ✅ TrajectoryUI.cs con pool de dots ✅ Física de parábola (velocidad inicial + gravedad) ✅ 30 dots por defecto ✅ Actualización en tiempo real |

### Tarea 2.11: Preview de Trayectoria (LineRenderer)
| Campo | Valor |
|-------|-------|
| **Tag** | UI, Visual |
| **Importance** | Should Have |
| **Estimated** | 4h |
| **Logged** | 4h |
| **Description** | LineRenderer que muestra trayectoria incluyendo rebotes en paredes. |
| **Subtasks** | ✅ TrajectoryPreview.cs con LineRenderer ✅ Raycast para detectar rebotes ✅ Máximo 3 rebotes ✅ Máxima distancia 50 unidades |

---

## 📁 Categoría: Design

### Tarea 2.12: Editor Personalizado para EnemyAI
| Campo | Valor |
|-------|-------|
| **Tag** | Tools, Editor |
| **Importance** | Should Have |
| **Estimated** | 3h |
| **Logged** | 3h |
| **Description** | Custom Editor que organiza el Inspector de EnemyAI con foldouts y campos contextuales. |
| **Subtasks** | ✅ EnemyAIEditor.cs ✅ Foldouts para cada categoría ✅ Campos de distancia de combate ✅ Campos de detección de ítems |

---

# 🏃 SPRINT 3 - Armas y Tipos de Enemigos (PENDIENTE)

## 📁 Categoría: Programming

### Tarea 3.1: Sistema de Daño y Salud
| Campo | Valor |
|-------|-------|
| **Tag** | Combat, Core |
| **Importance** | Must Have |
| **Estimated** | 6h |
| **Logged** | 0h |
| **Description** | Implementar componente Health y sistema de daño completo para enemigos y jugador. |
| **Subtasks** | ⬜ Componente Health con vida actual/máxima ⬜ Método TakeDamage(float) ⬜ Evento OnDeath ⬜ Bullet.OnTriggerEnter aplica daño ⬜ Feedback visual de impacto ⬜ Muerte del enemigo (animación/ragdoll) |

### Tarea 3.2: Arma Cuerpo a Cuerpo - Tubería
| Campo | Valor |
|-------|-------|
| **Tag** | Weapons, Combat |
| **Importance** | Must Have |
| **Estimated** | 8h |
| **Logged** | 0h |
| **Description** | Implementar arma melee con animación de golpe, detección de colisión y daño. |
| **Subtasks** | ⬜ WeaponData para Pipe (daño, alcance, cooldown) ⬜ Añadir Melee a WeaponType enum ⬜ Sistema de ataque melee (hitbox temporal) ⬜ Animación de swing ⬜ Detección de enemigos en área de golpe ⬜ Integración con InventoryHolder |

### Tarea 3.3: Escopeta
| Campo | Valor |
|-------|-------|
| **Tag** | Weapons, Combat |
| **Importance** | Must Have |
| **Estimated** | 6h |
| **Logged** | 0h |
| **Description** | Arma de fuego con disparo múltiple (pellets) y dispersión amplia. |
| **Subtasks** | ⬜ WeaponData para Shotgun (daño, pellets, dispersión) ⬜ Disparo de múltiples balas en cono ⬜ Mayor retroceso/cooldown que pistola ⬜ Menor capacidad de cargador ⬜ Modelo visual y pickup |

### Tarea 3.4: Tipo de Enemigo 1
| Campo | Valor |
|-------|-------|
| **Tag** | AI, Enemy |
| **Importance** | Must Have |
| **Estimated** | 8h |
| **Logged** | 0h |
| **Description** | Primer tipo de enemigo con comportamiento específico. Detalles a definir antes del sprint. |
| **Subtasks** | ⬜ Definir comportamiento único ⬜ Parámetros de IA diferenciados ⬜ Modelo/sprite distintivo ⬜ Integración con sistema existente |

### Tarea 3.5: Tipo de Enemigo 2
| Campo | Valor |
|-------|-------|
| **Tag** | AI, Enemy |
| **Importance** | Must Have |
| **Estimated** | 8h |
| **Logged** | 0h |
| **Description** | Segundo tipo de enemigo con comportamiento específico. Detalles a definir antes del sprint. |
| **Subtasks** | ⬜ Definir comportamiento único ⬜ Parámetros de IA diferenciados ⬜ Modelo/sprite distintivo ⬜ Integración con sistema existente |

### Tarea 3.6: Sistema de Spawning de Enemigos
| Campo | Valor |
|-------|-------|
| **Tag** | AI, Core |
| **Importance** | Should Have |
| **Estimated** | 4h |
| **Logged** | 0h |
| **Description** | Manager para spawnear distintos tipos de enemigos con armas asignadas. |
| **Subtasks** | ⬜ EnemySpawner con lista de prefabs ⬜ Configuración de arma inicial por spawn point ⬜ Pool de enemigos opcional |

---

## 📁 Categoría: Art

### Tarea 3.7: Assets de Nuevas Armas
| Campo | Valor |
|-------|-------|
| **Tag** | Art, Weapons |
| **Importance** | Must Have |
| **Estimated** | 6h |
| **Logged** | 0h |
| **Description** | Crear modelos/sprites para tubería y escopeta. |
| **Subtasks** | ⬜ Modelo tubería (en mano y pickup) ⬜ Modelo escopeta (en mano y pickup) ⬜ Efectos visuales de golpe melee ⬜ Efectos de disparo escopeta |

### Tarea 3.8: Assets de Tipos de Enemigos
| Campo | Valor |
|-------|-------|
| **Tag** | Art, Enemy |
| **Importance** | Must Have |
| **Estimated** | 8h |
| **Logged** | 0h |
| **Description** | Crear modelos/sprites distintivos para cada tipo de enemigo. |
| **Subtasks** | ⬜ Modelo/sprite Enemigo Tipo 1 ⬜ Modelo/sprite Enemigo Tipo 2 ⬜ Variaciones de color/accesorios |

---

# 📊 RESUMEN SPRINT 3

| Categoría | Horas Estimadas |
|-----------|-----------------|
| Programming | 40h |
| Art | 14h |
| **TOTAL SPRINT 3** | **54h** |

---

# 📦 BACKLOG - Tareas Genéricas (Sprints Futuros)

> Estas tareas definen los aspectos clave restantes del proyecto. Se detallarán antes de comenzar cada sprint.

## 📁 Gameplay

### BL-1: Sistema de Menús
| Campo | Valor |
|-------|-------|
| **Tag** | UI, Core |
| **Importance** | Must Have |
| **Estimated** | 15h |
| **Description** | Menú principal, selector de niveles, opciones, créditos y pausa. |

### BL-2: Diseño de Niveles
| Campo | Valor |
|-------|-------|
| **Tag** | Level Design |
| **Importance** | Must Have |
| **Estimated** | 20h |
| **Description** | Crear los niveles del juego con layout, NavMesh, spawns y pickups. |

### BL-3: Condiciones de Victoria/Derrota
| Campo | Valor |
|-------|-------|
| **Tag** | Gameplay |
| **Importance** | Must Have |
| **Estimated** | 6h |
| **Description** | Detectar fin de nivel, pantallas de resultado y progresión. |

---

## 📁 Audio y Visual

### BL-4: Sistema de Audio
| Campo | Valor |
|-------|-------|
| **Tag** | Audio |
| **Importance** | Should Have |
| **Estimated** | 10h |
| **Description** | AudioManager, música de fondo, efectos de sonido de gameplay. |

### BL-5: Polish Visual
| Campo | Valor |
|-------|-------|
| **Tag** | Art, VFX |
| **Importance** | Should Have |
| **Estimated** | 12h |
| **Description** | Efectos de partículas, feedback visual, animaciones adicionales. |

---

## 📁 Documentación

### BL-6: Documentación TFG
| Campo | Valor |
|-------|-------|
| **Tag** | Docs |
| **Importance** | Must Have |
| **Estimated** | 20h |
| **Description** | Memoria del TFG: introducción, estado del arte, requisitos, diseño, resultado, conclusiones. |

---

# 📊 RESUMEN DE HORAS

| Sprint/Fase | Estimado | Logueado | Estado |
|-------------|----------|----------|--------|
| **Sprint 1** | 62h | 62h | ✅ Completado |
| **Sprint 2** | 65h | 65h | ✅ Completado |
| **Sprint 3** | 54h | 0h | ⬜ Pendiente |
| **Backlog** | 83h | 0h | 📦 Por planificar |
| **TOTAL** | **264h** | **127h** | 48% |

---

# 📝 NOTAS PARA IMPORTAR A HACKNPLAN

1. **Crear Proyecto**: "Headcrab Miami TFG"
2. **Crear Milestones**: Sprint 1, Sprint 2, Sprint 3, Backlog
3. **Categorías disponibles**: Programming, Art, Design, Sound, Bug
4. **Tags sugeridos**: Core, Player, Enemy, AI, Weapons, Combat, UI, Level Design, Audio, Docs, VFX

## Workflow de Importación:
1. Crear categorías si no existen
2. Crear tags personalizados
3. Para cada tarea:
   - Crear tarea con título
   - Asignar categoría y tags
   - Establecer importancia
   - Añadir descripción
   - Crear subtasks como checklist
   - Registrar estimated/logged time
   - Mover a sprint correspondiente
4. Las tareas del Backlog se moverán a sprints futuros cuando se detallen
