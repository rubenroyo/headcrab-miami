using UnityEngine;

/// <summary>
/// Define un tipo de arma (Glock, Rifle…).
/// ScriptableObject — crear uno por arma.
/// Menu: Headcrab / Weapon Type
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponType", menuName = "Headcrab/Weapon Type")]
public class WeaponType : ScriptableObject
{
    [Header("Identificación")]
    public string weaponName = "Pistol";

    [Header("Munición")]
    [Tooltip("Balas que añade un cargador de este tipo")]
    public int bulletsPerMagazine = 10;

    [Tooltip("Máximo de balas que puede tener el arma")]
    public int maxBullets = 30;

    [Header("Disparo — Hitscan")]
    [Tooltip("Segundos entre disparos")]
    public float fireRate = 0.25f;

    [Tooltip("Si es true, mantener el clic dispara continuamente (SMG, ametralladora). " +
             "Si es false, cada clic es un disparo (pistola, escopeta).")]
    public bool isAutomatic = false;

    [Tooltip("Daño que inflige cada bala")]
    public float damage = 30f;

    [Tooltip("Distancia máxima del raycast. Si no golpea nada, el tracer va hasta este límite.")]
    public float maxHitscanDistance = 500f;

    [Tooltip("Capas que puede golpear el hitscan. Incluye Enemy y Wall como mínimo.")]
    public LayerMask impactLayerMask = Physics.DefaultRaycastLayers;

    [Tooltip("Proyectiles por disparo (1 = pistola/SMG, 8 = escopeta). Cada pellet hace su propio raycast con dispersión independiente.")]
    [Range(1, 20)]
    public int pelletsPerShot = 1;

    [Header("Cuerpo a Cuerpo")]
    [Tooltip("Si es true, el arma ataca en melee. No consume balas ni usa hitscan.")]
    public bool isMelee = false;

    [Tooltip("Radio del ataque cuerpo a cuerpo (metros)")]
    public float meleeRange = 1.5f;

    [Tooltip("Daño por golpe cuerpo a cuerpo")]
    public float meleeDamage = 60f;

    [Header("Dispersión")]
    [Tooltip("Dispersión base del arma en grados (sin ningún modificador)")]
    [Range(0f, 20f)]
    public float baseDispersion = 2f;

    [Tooltip("Multiplicador de dispersión al moverse andando (> 1 = más dispersión)")]
    public float movingDispersionMultiplier = 2f;

    [Tooltip("Multiplicador de dispersión al correr")]
    public float sprintingDispersionMultiplier = 4f;

    [Tooltip("Multiplicador de dispersión al apuntar ADS (< 1 = menos dispersión)")]
    public float aimingDispersionMultiplier = 0.4f;

    [Header("Efectos Visuales — Tracer")]
    [Tooltip("Prefab del tracer (línea + luz). Debe tener el componente BulletTracer.")]
    public GameObject tracerPrefab;

    [Tooltip("Duración del tracer en segundos. A 60fps, 0.067s ≈ 4 frames.")]
    public float tracerDuration = 0.067f;

    [Header("Prefabs")]
    [Tooltip("Prefab del arma en el SUELO (con collider trigger para recoger)")]
    public GameObject pickupPrefab;

    [Tooltip("Prefab del arma EQUIPADA (visual en la mano del enemigo). " +
             "Debe tener WeaponVisual con MuzzlePoint configurado.")]
    public GameObject equippedPrefab;

    [Tooltip("Prefab del cargador en el SUELO")]
    public GameObject magazinePickupPrefab;

    [Header("Primera Persona (FPS)")]
    [Tooltip("Prefab del arma para vista FPS (mano + pistola). " +
             "Debe tener WeaponVisual con MuzzlePoint configurado.")]
    public GameObject fpsPrefab;

    [Tooltip("Posición del modelo FPS relativo al punto de ojos")]
    public Vector3 fpsPositionOffset = new Vector3(0.2f, -0.2f, 0.4f);

    [Tooltip("Rotación del modelo FPS")]
    public Vector3 fpsRotationOffset = Vector3.zero;

    [Header("Recoil (Retroceso FPS)")]
    [Tooltip("Ángulo de rotación del retroceso en el eje Z (hacia arriba)")]
    public float recoilAngle = 15f;

    [Tooltip("Duración del retroceso (subida)")]
    public float recoilDuration = 0.05f;

    [Tooltip("Duración de la recuperación (bajada)")]
    public float recoilRecoveryDuration = 0.15f;

    [Tooltip("Intensidad del camera shake al disparar")]
    public float fireShakeIntensity = 0.15f;

    [Header("Posicionamiento Equipada (Tercera Persona)")]
    [Tooltip("Offset local desde el punto de anclaje del enemigo")]
    public Vector3 equippedPositionOffset = new Vector3(0f, 0.5f, 0.75f);

    [Tooltip("Rotación local del arma equipada")]
    public Vector3 equippedRotationOffset = Vector3.zero;

    /// <summary>
    /// Calcula la dispersión final combinando base del arma + estado + temblor del enemigo.
    /// </summary>
    public float GetDispersion(WeaponState state, float handTremor = 0f)
    {
        float multiplier = state switch
        {
            WeaponState.Aiming    => aimingDispersionMultiplier,
            WeaponState.Moving    => movingDispersionMultiplier,
            WeaponState.Sprinting => sprintingDispersionMultiplier,
            _                     => 1f
        };

        return baseDispersion * multiplier + handTremor;
    }
}
