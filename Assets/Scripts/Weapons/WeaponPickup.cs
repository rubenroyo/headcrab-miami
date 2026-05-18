using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Pickup de arma del suelo con lógica de recogida dual:
///   - IA:      llama a PickUpByAI() cuando llega al radio del arma (EnemyAI).
///   - Poseído: PlayerController detecta que el enemigo está en el radio y
///              el jugador pulsa E → llama PickUpByPlayer().
///
/// Registro estático: WeaponPickup.All permite a PlayerController encontrar
/// los pickups cercanos sin FindObjectsByType por frame.
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    // ── Registro estático ────────────────────────────────────────────────
    private static readonly List<WeaponPickup> s_All = new();
    public  static IReadOnlyList<WeaponPickup> All => s_All;

    // ── Datos del arma ───────────────────────────────────────────────────
    [Header("Datos del Arma")]
    [SerializeField] private WeaponType weaponType;
    [Tooltip("Balas en el cargador. -1 = usar magazineSize del WeaponType.")]
    [SerializeField] private int bulletsInMagazine = -1;
    [Tooltip("Balas en reserva.")]
    [SerializeField] private int reserveBullets = 0;

    [Header("Radio de recogida")]
    [Tooltip("Radio de la esfera de interacción.")]
    [SerializeField] private float pickupRadius = 1.5f;

    // ── Estado ───────────────────────────────────────────────────────────
    private bool isPickedUp = false;

    /// Inventarios actualmente dentro del radio de recogida.
    private readonly HashSet<InventoryHolder> nearbyInventories = new();

    // ── Propiedades ──────────────────────────────────────────────────────
    public WeaponType WeaponType    => weaponType;
    public bool       CanBePickedUp => !isPickedUp && weaponType != null;
    public bool       IsPickedUp    => isPickedUp;

    /// Balas en cargador (para que la IA evalúe si merece la pena recoger el arma).
    public int CurrentBullets => bulletsInMagazine >= 0
        ? bulletsInMagazine
        : (weaponType != null ? weaponType.magazineSize : 0);

    /// ¿Tiene este inventory dentro de su radio?
    public bool IsNearby(InventoryHolder inv) => nearbyInventories.Contains(inv);

    // ── Ciclo de vida ────────────────────────────────────────────────────

    void Awake()
    {
        // ── Trigger sphere para detección de proximidad ──────────────────
        // Buscamos una SphereCollider ya existente que sea trigger.
        // Si no hay ninguna, añadimos una nueva. No tocamos los colliders
        // no-trigger del prefab (son los que impiden atravesar el suelo).
        SphereCollider sphere = null;
        foreach (SphereCollider sc in GetComponents<SphereCollider>())
        {
            if (sc.isTrigger) { sphere = sc; break; }
        }
        if (sphere == null)
            sphere = gameObject.AddComponent<SphereCollider>();

        sphere.radius    = pickupRadius;
        sphere.isTrigger = true;

        // ── Collider físico (no-trigger) para reposar en el suelo ────────
        // Si el prefab no trae ninguno, añadimos una caja genérica de arma.
        bool hasPhysicsCollider = false;
        foreach (Collider c in GetComponents<Collider>())
        {
            if (!c.isTrigger) { hasPhysicsCollider = true; break; }
        }
        if (!hasPhysicsCollider)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.size      = new Vector3(0.15f, 0.08f, 0.45f); // perfil genérico de pistola
            box.isTrigger = false;
        }

        // Valores por defecto de balas
        if (bulletsInMagazine < 0 && weaponType != null)
            bulletsInMagazine = weaponType.magazineSize;
    }

    void OnEnable()  => s_All.Add(this);
    void OnDisable() { s_All.Remove(this); nearbyInventories.Clear(); }

    // ── Trigger ──────────────────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (isPickedUp) return;
        InventoryHolder inv = other.GetComponentInParent<InventoryHolder>();
        if (inv != null) nearbyInventories.Add(inv);
    }

    void OnTriggerExit(Collider other)
    {
        InventoryHolder inv = other.GetComponentInParent<InventoryHolder>();
        if (inv != null) nearbyInventories.Remove(inv);
    }

    // ── API pública ──────────────────────────────────────────────────────

    /// <summary>
    /// Inicializa el pickup con tipo y estado de munición separado (magazine / reserva).
    /// Llamar al instanciar un arma soltada para preservar los stats exactos.
    /// </summary>
    public void Initialize(WeaponType type, int magazine, int reserve)
    {
        weaponType        = type;
        bulletsInMagazine = magazine;
        reserveBullets    = reserve;
        isPickedUp        = false;

        // Actualizar radio del collider si ya existe
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null) sphere.radius = pickupRadius;
    }

    /// <summary>
    /// Recogida por IA. No requiere trigger ni input.
    /// El enemigo llama a esto cuando llega al radio del arma (EnemyAI.UpdateSeekingItem).
    /// El dropPoint se obtiene del InventoryHolder del enemigo.
    /// </summary>
    public bool PickUpByAI(InventoryHolder inventory)
    {
        if (!CanBePickedUp || inventory == null) return false;
        PickUpInternal(inventory);
        return true;
    }

    /// <summary>
    /// Recogida por el jugador poseído. Solo funciona si el inventory está en el radio.
    /// Llamar desde PlayerController cuando se pulsa E.
    /// </summary>
    public bool PickUpByPlayer(InventoryHolder inventory, Transform _dropPoint = null)
    {
        if (!CanBePickedUp) return false;
        if (!nearbyInventories.Contains(inventory)) return false;
        PickUpInternal(inventory);
        return true;
    }

    // ── Lógica interna ───────────────────────────────────────────────────

    private void PickUpInternal(InventoryHolder inventory)
    {
        // Construir WeaponData con el estado de munición exacto
        WeaponData newWeapon = new WeaponData(weaponType, 0);
        newWeapon.bulletsInMagazine = bulletsInMagazine >= 0 ? bulletsInMagazine : weaponType.magazineSize;
        newWeapon.reserveBullets    = reserveBullets;

        // Equipar — devuelve el arma anterior si había
        WeaponData previousWeapon = inventory.EquipWeapon(newWeapon);

        // Si tenía arma, soltarla delante del enemigo
        if (previousWeapon != null && previousWeapon.weaponType?.pickupPrefab != null)
        {
            SpawnDroppedWeapon(previousWeapon, inventory.GetDropPosition(), inventory.transform.forward);
        }

        isPickedUp = true;
        Destroy(gameObject);
    }

    /// <summary>
    /// Instancia el prefab de pickup de un arma y le aplica física de tiro.
    /// Estático para que InventoryHolder.DropWeapon() también pueda usarlo.
    /// </summary>
    public static GameObject SpawnDroppedWeapon(WeaponData weapon, Vector3 position, Vector3 throwDirection, float throwForce = 8f)
    {
        if (weapon?.weaponType?.pickupPrefab == null) return null;

        GameObject obj    = Instantiate(weapon.weaponType.pickupPrefab, position, Random.rotation);
        WeaponPickup pickup = obj.GetComponent<WeaponPickup>();
        if (pickup != null)
            pickup.Initialize(weapon.weaponType, weapon.bulletsInMagazine, weapon.reserveBullets);

        // Física de tiro — si el prefab no tiene Rigidbody se añade en runtime
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.linearDamping       = 0.5f;
        rb.AddForce(throwDirection.normalized * throwForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);

        return obj;
    }
}
