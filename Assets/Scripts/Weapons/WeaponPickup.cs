using UnityEngine;

/// <summary>
/// Permite recoger un arma del suelo.
/// El enemigo poseído que colisione con esto y pulse "E" equipará el arma.
/// Contiene datos serializados del arma (tipo + balas).
/// </summary>
[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    [Header("Datos del Arma")]
    [SerializeField] private WeaponType weaponType;
    [SerializeField] private int currentBullets = -1; // -1 = usar balas por defecto del tipo
    
    private bool isPickedUp = false;
    
    // El InventoryHolder que está en rango para recoger
    private InventoryHolder nearbyInventory;
    
    public WeaponType WeaponType => weaponType;
    public int CurrentBullets => currentBullets >= 0 ? currentBullets : (weaponType != null ? weaponType.bulletsPerMagazine : 0);
    public bool CanBePickedUp => !isPickedUp && weaponType != null && nearbyInventory != null;
    
    void Start()
    {
        // Asegurar que el collider es trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
        
        // Si no se especificaron balas, usar las del cargador
        if (currentBullets < 0 && weaponType != null)
        {
            currentBullets = weaponType.bulletsPerMagazine;
        }
    }
    
    /// <summary>
    /// Inicializa el pickup con tipo y balas (usado cuando se suelta un arma)
    /// </summary>
    public void Initialize(WeaponType type, int bullets)
    {
        weaponType = type;
        currentBullets = bullets;
        isPickedUp = false;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (isPickedUp || weaponType == null) return;
        
        // Solo enemigos poseídos pueden recoger armas
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy == null || !enemy.IsPossessed)
            return;
        
        InventoryHolder inventory = enemy.GetComponent<InventoryHolder>();
        if (inventory != null)
        {
            nearbyInventory = inventory;
            Debug.Log($"Pulsa E para recoger {weaponType.weaponName}");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null)
        {
            InventoryHolder inventory = enemy.GetComponent<InventoryHolder>();
            if (inventory == nearbyInventory)
            {
                nearbyInventory = null;
            }
        }
    }
    
    void Update()
    {
        if (!CanBePickedUp) return;
        
        // Detectar tecla E para recoger
        if (Input.GetKeyDown(KeyCode.E))
        {
            PickUp();
        }
    }
    
    /// <summary>
    /// Recoge el arma (llamado cuando se pulsa E)
    /// </summary>
    public void PickUp()
    {
        if (!CanBePickedUp) return;
        
        // Crear datos del arma
        WeaponData newWeaponData = new WeaponData(weaponType, CurrentBullets);
        
        // Equipar el arma
        WeaponData previousWeapon = nearbyInventory.EquipWeapon(newWeaponData);
        
        // Si tenía un arma anterior, crear pickup donde estaba este
        if (previousWeapon != null && previousWeapon.weaponType != null && previousWeapon.weaponType.pickupPrefab != null)
        {
            GameObject pickupObj = Instantiate(previousWeapon.weaponType.pickupPrefab, transform.position, Quaternion.identity);
            WeaponPickup pickup = pickupObj.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                pickup.Initialize(previousWeapon.weaponType, previousWeapon.currentBullets);
            }
        }
        
        isPickedUp = true;
        
        // Destruir este pickup
        Destroy(gameObject);
    }
}
