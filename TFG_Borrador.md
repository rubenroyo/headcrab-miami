# Headcrab Miami - Trabajo de Fin de Grado
## Borrador de la Memoria

---

# 1. Introducción

## 1.1 Motivación

Desde pequeño he sido un apasionado de los videojuegos, no solo como forma de entretenimiento sino como medio artístico y técnico. La posibilidad de combinar programación, diseño y creatividad en un mismo proyecto siempre me ha atraído enormemente. Durante la carrera he adquirido conocimientos en programación orientada a objetos, estructuras de datos, algoritmos y patrones de diseño que quería poner en práctica en un proyecto personal significativo.

La elección de desarrollar un videojuego como TFG viene motivada por:

- **Pasión personal**: Los videojuegos han sido una constante en mi vida y quería entender cómo se construyen desde dentro.
- **Aplicación práctica de conocimientos**: Un videojuego requiere aplicar múltiples disciplinas: programación, matemáticas (física, trayectorias), inteligencia artificial, diseño de sistemas, gestión de recursos...
- **Reto técnico**: Desarrollar un juego completo es un desafío que me obliga a resolver problemas complejos de forma creativa.
- **Portfolio profesional**: Un proyecto de esta envergadura demuestra capacidades técnicas a futuros empleadores.

## 1.2 Descripción del Juego

**Headcrab Miami** es un videojuego de acción top-down (vista cenital) desarrollado en Unity con estética pixel art. El juego se inspira en títulos como Hotline Miami, combinando acción frenética con una mecánica de posesión única.

### Premisa

El jugador controla a una criatura (inspirada en los headcrabs de Half-Life) que puede saltar sobre los enemigos y poseerlos, tomando control de sus cuerpos para luchar contra el resto de enemigos. Esta mecánica de posesión añade una capa estratégica al gameplay clásico del género.

### Características Principales

- **Perspectiva top-down** con cámara isométrica y pixel art a 480x270 escalado a 1080p
- **Sistema de posesión**: Salta sobre enemigos para controlar sus cuerpos
- **Combate con armas**: Pistolas, escopetas y armas cuerpo a cuerpo
- **IA enemiga avanzada**: Enemigos con visión, patrullas, búsqueda y recogida de armas
- **Física de salto en dos fases**: Sistema de movimiento fluido con trayectoria visible

### Público Objetivo

Jugadores que disfruten de:
- Juegos de acción rápida tipo arcade
- Desafíos que requieren reflejos y planificación
- Estética retro/pixel art
- Mecánicas innovadoras que añadan profundidad estratégica

---

# 2. Estado del Arte

El género de los **top-down shooters** (también llamados twin-stick shooters cuando usan dos joysticks) tiene una rica historia que se remonta a los orígenes de los videojuegos. A continuación, analizamos la evolución del género a través de títulos clave y comparamos sus características con Headcrab Miami.

## 2.1 Orígenes del Género: Robotron: 2084 (1982)

**Robotron: 2084**, desarrollado por Williams Electronics y diseñado por Eugene Jarvis, es considerado uno de los juegos más influyentes en la historia de los shooters. Fue pionero en el uso de controles twin-stick, permitiendo al jugador moverse con un joystick mientras disparaba en cualquier dirección con el otro.

### Características de Robotron: 2084

| Aspecto | Descripción |
|---------|-------------|
| **Perspectiva** | Top-down, pantalla fija |
| **Controles** | Twin-stick (movimiento + disparo independientes) |
| **Objetivo** | Sobrevivir oleadas de robots y rescatar humanos |
| **Dificultad** | Muy alta, enfocado en reflejos y supervivencia |
| **Mecánica principal** | Disparo en 8 direcciones, esquivar enemigos |
| **Progresión** | Puntuación, sin niveles narrativos |

### Legado

Robotron estableció la plantilla para los shooters multidireccionales y demostró que el control independiente de movimiento y disparo podía crear una experiencia de juego profundamente satisfactoria. Su influencia se puede ver en títulos como Geometry Wars, Smash TV y, en última instancia, en juegos modernos como Hotline Miami.

## 2.2 Referente del Género: Hotline Miami (2012)

**Hotline Miami**, desarrollado por Dennaton Games y publicado por Devolver Digital, es considerado el estándar de oro de los top-down shooters modernos y uno de los juegos indie más influyentes jamás creados.

### Contexto y Desarrollo

- Desarrollado en 9 meses por un equipo de 2 personas (Jonatan Söderström y Dennis Wedin)
- Motor: GameMaker
- Presupuesto: Mínimo, financiado parcialmente durante el desarrollo
- Ventas: Más de 1.5 millones de copias (2015), más de 5 millones combinado con la secuela

### Características de Hotline Miami

| Aspecto | Descripción |
|---------|-------------|
| **Perspectiva** | Top-down con pixel art |
| **Ambientación** | Miami, años 80, neón y violencia |
| **Muerte instantánea** | Tanto el jugador como los enemigos mueren de un golpe |
| **Reinicio rápido** | Reinicio instantáneo tras morir, fomenta experimentación |
| **Máscaras** | Sistema de habilidades mediante máscaras de animales |
| **IA impredecible** | Enemigos con comportamiento variable, no determinista |
| **Narrativa** | Historia críptica sobre violencia y realidad |
| **Música** | Synthwave, fundamental para la experiencia |

### Innovaciones Clave

1. **Fluidez muerte-reinicio**: El juego elimina penalizaciones por morir, convirtiendo cada intento en un experimento.
2. **Mensaje anti-violencia**: A pesar de su contenido violento, el juego critica la violencia gratuita en los videojuegos.
3. **Estética sintetizada**: Combinación perfecta de pixel art, música synthwave y narrativa surrealista.

### Influencia

Hotline Miami inspiró una generación de desarrolladores indie y ayudó a popularizar el género synthwave en la música de videojuegos. Juegos como Katana ZERO, Ruiner y el propio Ape Out reconocen su influencia.

## 2.3 Título Reciente: Ape Out (2019)

**Ape Out**, desarrollado por Gabe Cuzzillo y publicado por Devolver Digital, representa la evolución más artística del género, combinando gameplay brutal con una presentación audiovisual única.

### Contexto y Desarrollo

- Desarrollado durante estudios en NYU Game Center
- Motor: Unity
- Financiado parcialmente por Indie Fund
- Colaboración con Bennett Foddy (QWOP, Getting Over It)

### Características de Ape Out

| Aspecto | Descripción |
|---------|-------------|
| **Perspectiva** | Top-down, estilo Saul Bass |
| **Protagonista** | Un gorila escapando de cautiverio |
| **Combate** | Melee puro (empujar, agarrar, usar como escudo) |
| **Niveles** | Procedurales, laberintos generados |
| **Presentación** | Estilo carátula de álbum de jazz, minimalista |
| **Música** | Jazz dinámico que reacciona al gameplay |
| **Muerte** | Un golpe mata (jugador y enemigos) |

### Innovaciones Clave

1. **Música procedural**: La banda sonora de percusión jazz se genera en tiempo real basándose en las acciones del jugador.
2. **Arte minimalista**: Estilo visual inspirado en Saul Bass con siluetas y colores planos.
3. **Estructura de álbum**: El juego se divide en "discos" con "pistas", reforzando la temática musical.

### Reconocimientos

- BAFTA al Logro en Audio (2020)
- Finalista en Independent Games Festival (2016)

## 2.4 Tabla Comparativa

| Característica | Robotron: 2084 | Hotline Miami | Ape Out | Headcrab Miami |
|----------------|----------------|---------------|---------|----------------|
| **Año** | 1982 | 2012 | 2019 | 2026 |
| **Motor** | Hardware arcade | GameMaker | Unity | Unity |
| **Perspectiva** | Top-down fijo | Top-down | Top-down | Top-down isométrico |
| **Estilo visual** | Vectorial/simple | Pixel art | Minimalista/Bass | Pixel art |
| **Muerte** | Varios golpes | Un golpe | Un golpe | Un golpe |
| **Armas** | Disparo | Melee + disparo | Solo melee | Melee + disparo |
| **Mecánica única** | Twin-stick | Máscaras | Escudo humano | Posesión |
| **IA enemiga** | Patrones fijos | Variable | Reactiva | Estados + visión |
| **Reinicio** | Arcade (vidas) | Instantáneo | Instantáneo | Instantáneo |
| **Narrativa** | Ninguna | Surrealista | Implícita | Por definir |
| **Música** | Efectos | Synthwave | Jazz procedural | Por definir |
| **Niveles** | Generados | Diseñados | Procedurales | Diseñados |

## 2.5 Posicionamiento de Headcrab Miami

Headcrab Miami toma elementos de sus predecesores mientras introduce su propia identidad:

### De Robotron: 2084
- Control independiente de movimiento y apuntado
- Acción frenética contra múltiples enemigos

### De Hotline Miami
- Estética pixel art
- Muerte instantánea y reinicio rápido
- IA enemiga con comportamientos variados
- Combinación de armas melee y de fuego

### De Ape Out
- Publicador compartido (Devolver Digital inspira el estilo)
- Énfasis en el combate físico/melee

### Aportación Original: Sistema de Posesión
La mecánica de posesión distingue a Headcrab Miami del resto:
- El jugador no tiene cuerpo propio permanente
- Debe saltar sobre enemigos para controlarlos
- Hereda las armas y capacidades del cuerpo poseído
- Añade una capa táctica: ¿qué enemigo poseer? ¿cuándo saltar a otro?

Esta mecánica transforma el género de "matar a todos" a "poseer estratégicamente", ofreciendo un loop de gameplay único.

---

# 3. Análisis de Requisitos

> **NOTA**: Esta sección está pendiente de revisión con el tutor. Las historias de usuario en el contexto de un videojuego pueden enfocarse de forma diferente a una aplicación tradicional.

## 3.1 Historias de Usuario

*[Sección pendiente de desarrollo tras consulta con tutor]*

### Posibles enfoques:

**Opción A: Jugador como usuario**
- "Como jugador, quiero poder poseer enemigos para sentir que tengo control estratégico sobre el campo de batalla"
- "Como jugador, quiero que los enemigos patrullen y busquen para sentir que el mundo está vivo"

**Opción B: Desarrollador como usuario**
- "Como desarrollador de mi TFG, quiero implementar un sistema de IA modular para demostrar conocimientos de patrones de diseño"
- "Como desarrollador, quiero crear un sistema de armas basado en ScriptableObjects para facilitar la extensibilidad"

**Opción C: Enfoque mixto**
- Requisitos funcionales desde la perspectiva del jugador
- Requisitos técnicos desde la perspectiva del desarrollador

*[Consultar con tutor cuál es el enfoque más apropiado para el TFG]*

---

# 4. Diseño

## 4.1 Arquitectura General

El proyecto sigue una arquitectura basada en componentes propia de Unity, con separación clara de responsabilidades.

### Diagrama de Capas

```
┌─────────────────────────────────────────────────────────────┐
│                     CAPA DE PRESENTACIÓN                    │
│  (Sprites, UI, Cámara, Post-proceso, Pixel Art Rendering)  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     CAPA DE GAMEPLAY                        │
│  (PlayerController, EnemyAI, InventoryHolder, Combat)       │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     CAPA DE DATOS                           │
│  (ScriptableObjects: WeaponType, WeaponData, Settings)      │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                     CAPA DE SISTEMAS                        │
│  (NavMesh, Physics, Input System, Audio)                    │
└─────────────────────────────────────────────────────────────┘
```

## 4.2 Diagrama de Clases Principal

```
┌─────────────────────┐     ┌─────────────────────┐
│   PlayerController  │     │      EnemyAI        │
├─────────────────────┤     ├─────────────────────┤
│ - state: PlayerState│     │ - state: AIState    │
│ - moveSpeed: float  │     │ - viewRadius: float │
│ - jumpForce: float  │     │ - viewAngle: float  │
├─────────────────────┤     ├─────────────────────┤
│ + Move()            │     │ + Patrol()          │
│ + Jump()            │     │ + Chase()           │
│ + PossessEnemy()    │     │ + Search()          │
│ + Shoot()           │     │ + CanSeePlayer()    │
└─────────────────────┘     └─────────────────────┘
         │                           │
         │     ┌─────────────────┐   │
         └────►│ InventoryHolder │◄──┘
               ├─────────────────┤
               │ - weapon: WeaponType
               │ - currentAmmo: int
               ├─────────────────┤
               │ + TryFire()     │
               │ + Reload()      │
               │ + EquipWeapon() │
               └─────────────────┘
                       │
                       ▼
               ┌─────────────────┐
               │   WeaponType    │
               │ (ScriptableObj) │
               ├─────────────────┤
               │ - weaponName    │
               │ - bulletsPerMag │
               │ - fireRate      │
               │ - bulletSpeed   │
               │ - bulletPrefab  │
               └─────────────────┘
```

## 4.3 Diagrama de Estados del Jugador

```
                    ┌───────────┐
                    │   START   │
                    └─────┬─────┘
                          │
                          ▼
                    ┌───────────┐
         ┌─────────│  NORMAL   │─────────┐
         │         └─────┬─────┘         │
         │               │               │
    [RightClick]    [Space+Click]   [Land on Enemy]
         │               │               │
         ▼               ▼               ▼
   ┌───────────┐   ┌───────────┐   ┌───────────┐
   │  AIMING   │   │  JUMPING  │   │ POSSESSING│
   └─────┬─────┘   └─────┬─────┘   └─────┬─────┘
         │               │               │
    [Release]       [Land/Miss]    [Possess OK]
         │               │               │
         └───────────────┴───────────────┘
                         │
                         ▼
                   ┌───────────┐
                   │  NORMAL   │
                   └───────────┘
```

## 4.4 Diagrama de Estados del Enemigo (IA)

```
                         ┌───────────┐
                         │   IDLE    │
                         └─────┬─────┘
                               │
              ┌────────────────┼────────────────┐
              │                │                │
        [Has Route]      [Random Move]    [See Item]
              │                │                │
              ▼                ▼                ▼
        ┌───────────┐    ┌───────────┐    ┌───────────┐
        │ PATROLLING│    │ WANDERING │    │SEEKING_ITEM│
        └─────┬─────┘    └─────┬─────┘    └─────┬─────┘
              │                │                │
              └────────┬───────┴────────────────┘
                       │
                  [See Player]
                       │
                       ▼
                 ┌───────────┐
        ┌────────│  CHASING  │────────┐
        │        └─────┬─────┘        │
        │              │              │
   [Has Weapon]   [Lost Sight]   [No Weapon]
        │              │              │
        ▼              ▼              ▼
  ┌────────────┐ ┌─────────────┐ ┌───────────┐
  │COMBAT      │ │INVESTIGATING│ │ Continue  │
  │ENGAGED     │ └──────┬──────┘ │ CHASING   │
  └────────────┘        │        └───────────┘
                        │
                   [Not Found]
                        │
                        ▼
                  ┌───────────┐
                  │ SEARCHING │
                  └─────┬─────┘
                        │
                   [Timeout]
                        │
                        ▼
                  ┌───────────┐
                  │ RETURNING │
                  └───────────┘
```

## 4.5 Sistema de Armas (WeaponType)

```
┌─────────────────────────────────────────────────────────────┐
│                    WeaponType (SO)                          │
├─────────────────────────────────────────────────────────────┤
│  Identificación                                             │
│  ├── weaponName: string                                     │
│                                                             │
│  Munición                                                   │
│  ├── bulletsPerMagazine: int                               │
│  └── maxBullets: int                                        │
│                                                             │
│  Disparo                                                    │
│  ├── fireRate: float (segundos entre disparos)             │
│  ├── bulletSpeed: float                                     │
│  └── bulletLifetime: float                                  │
│                                                             │
│  Prefabs                                                    │
│  ├── bulletPrefab: GameObject                              │
│  ├── pickupPrefab: GameObject (arma en suelo)              │
│  ├── equippedPrefab: GameObject (arma en mano)             │
│  └── magazinePickupPrefab: GameObject                      │
│                                                             │
│  Posicionamiento                                            │
│  ├── equippedPositionOffset: Vector3                       │
│  ├── equippedRotationOffset: Vector3                       │
│  └── muzzleOffset: Vector3                                  │
└─────────────────────────────────────────────────────────────┘

Instancias planificadas:
┌──────────┬─────────┬──────────┬───────────┐
│ Pistola  │Escopeta │ Tubería  │  Rifle?   │
│ fireRate │fireRate │  melee   │ fireRate  │
│   0.25s  │  0.8s   │  0.5s    │   0.1s    │
│ bullets:1│bullets:6│ damage:X │ bullets:1 │
└──────────┴─────────┴──────────┴───────────┘
```

## 4.6 Sistema de Pixelización

```
Resolución Base: 1920 x 1080
Pixel Size: 4

Resolución de Render: 
  RenderWidth  = 1920 / 4 = 480
  RenderHeight = 1080 / 4 = 270

┌─────────────────────────────────────────┐
│         Pantalla 1920x1080              │
│  ┌───────────────────────────────────┐  │
│  │                                   │  │
│  │    RenderTexture 480x270          │  │
│  │    (Filtro: Point/Nearest)        │  │
│  │                                   │  │
│  │    Escalado 4x                    │  │
│  │                                   │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## 4.7 Mockups de UI

*[Sección pendiente - Incluir capturas del diseño de UI planificado]*

### Elementos de UI previstos:
- HUD: Munición actual / Munición en cargador
- Indicador de arma equipada
- Trayectoria de salto (dots)
- Menú principal
- Selector de niveles
- Menú de pausa

---

# 5. Resultado

> **NOTA**: Esta sección se completará cuando el proyecto esté finalizado.

## 5.1 Screenshots del Juego

*[Pendiente de capturas del juego final]*

### Capturas planificadas:
- [ ] Menú principal
- [ ] Gameplay: Jugador normal
- [ ] Gameplay: Posesión de enemigo
- [ ] Gameplay: Combate con armas
- [ ] Enemigos patrullando
- [ ] Sistema de trayectoria visible
- [ ] Los 3 niveles

## 5.2 Evaluación

### Métricas Técnicas
*[Pendiente de medición]*
- FPS promedio en hardware objetivo
- Tiempo de carga de escenas
- Uso de memoria

### Playtesting
*[Pendiente tras desarrollo]*
- Número de testers
- Feedback cualitativo
- Bugs encontrados y corregidos

## 5.3 Encuestas de itch.io

*[Pendiente tras publicación en itch.io]*

### Plan de publicación:
1. Crear página en itch.io
2. Subir build jugable
3. Incluir formulario de feedback
4. Recopilar métricas de descargas y tiempo de juego

---

# 6. Conclusiones

> **NOTA**: Esta sección se completará al finalizar el proyecto.

## 6.1 ¿Qué hemos aprendido?

### Técnicamente
*[Pendiente de reflexión final]*

Áreas de aprendizaje previstas:
- Desarrollo de IA con máquinas de estado
- NavMesh y pathfinding en Unity
- Sistemas de armas modulares con ScriptableObjects
- Renderizado pixel art con post-procesado
- Gestión de proyecto con HackNPlan/sprints

### Personalmente
*[Pendiente de reflexión final]*

## 6.2 ¿Qué haría de forma distinta?

*[Pendiente de análisis retrospectivo]*

Posibles áreas de mejora:
- Planificación inicial
- Estimación de tiempos
- Priorización de features
- Testing más temprano
- Documentación durante el desarrollo

## 6.3 Relación con la Carrera

### Asignaturas directamente aplicadas

| Asignatura | Aplicación en el Proyecto |
|------------|---------------------------|
| **Programación Orientada a Objetos** | Arquitectura basada en clases, herencia, polimorfismo |
| **Estructuras de Datos** | Listas de waypoints, pools de objetos, colas de prioridad |
| **Algoritmos** | Pathfinding (A* interno de NavMesh), detección de colisiones |
| **Ingeniería del Software** | Patrones de diseño (State, Singleton, Observer) |
| **Inteligencia Artificial** | Máquinas de estado finitos, sistema de visión, toma de decisiones |
| **Gráficos por Computador** | Transformaciones, cámara, renderizado, shaders |
| **Interacción Persona-Ordenador** | Diseño de controles, feedback visual, UX |
| **Gestión de Proyectos** | Metodología ágil, sprints, gestión de tareas |

### Competencias Transversales Desarrolladas

- **CT-01 Comprensión e Integración**: Integración de múltiples sistemas (IA, física, renderizado)
- **CT-02 Aplicación y Pensamiento Práctico**: Resolución de problemas técnicos reales
- **CT-03 Análisis y Resolución de Problemas**: Debugging, optimización
- **CT-06 Trabajo en Equipo y Liderazgo**: Aunque es proyecto individual, interacción con tutor
- **CT-10 Planificación y Gestión del Tiempo**: Sprints, estimaciones, deadlines

---

# Anexos

## Anexo A: Glosario

| Término | Definición |
|---------|------------|
| **Top-down** | Perspectiva de cámara que mira hacia abajo |
| **Twin-stick** | Control con dos joysticks (movimiento + apuntado) |
| **NavMesh** | Malla de navegación para pathfinding de IA |
| **ScriptableObject** | Asset de datos de Unity, no asociado a escena |
| **Pixel art** | Estilo artístico basado en píxeles visibles |
| **State machine** | Patrón de diseño basado en estados y transiciones |
| **Synthwave** | Género musical electrónico inspirado en los 80s |

## Anexo B: Referencias

### Videojuegos Analizados
- Williams Electronics. (1982). *Robotron: 2084*. Williams Electronics.
- Dennaton Games. (2012). *Hotline Miami*. Devolver Digital.
- Cuzzillo, G. (2019). *Ape Out*. Devolver Digital.

### Bibliografía Técnica
*[Pendiente de completar con recursos utilizados]*

### Recursos Web
- Unity Documentation: https://docs.unity3d.com/
- Wikipedia - Shoot 'em up: https://en.wikipedia.org/wiki/Shoot_%27em_up
- Wikipedia - Hotline Miami: https://en.wikipedia.org/wiki/Hotline_Miami
- Wikipedia - Ape Out: https://en.wikipedia.org/wiki/Ape_Out

---

*Documento generado: Marzo 2026*
*Última actualización: [Fecha]*
