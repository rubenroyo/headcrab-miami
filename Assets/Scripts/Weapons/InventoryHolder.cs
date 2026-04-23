using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona el inventario de un enemigo: vida y arma equipada.
/// Se coloca en el GameObject del enemigo.
/// </summary>
public class InventoryHolder : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    
    [Header("Arma Equipada")]
    [SerializeField] private WeaponData equippedWeapon;
    
    [Header("Punto de Anclaje del Arma")]
    [Tooltip("Transform donde se posiciona el arma. Si es null, usa el transform del enemigo.")]
    [SerializeField] private Transform weaponAnchor;
    
    [Header("Pool de Balas")]
    [SerializeField] private int bulletPoolSize = 16;
    
    // Visual del arma equipada (instancia del equippedPrefab)
    private GameObject equippedWeaponVisual;
    
    // Pool de balas
    private readonly List<GameObject> bulletPool = new List<GameObject>();
    private Transform bulletPoolParent;
    private float nextFireTime = 0f;
    
    // Propiedades públicas
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0f;
    public WeaponData EquippedWeapon => equippedWeapon;
    public bool HasWeapon => equippedWeapon != null && equippedWeapon.weaponType != null;
    
    // Eventos
    public System.Action<float, float> OnHealthChanged; // current, max
    public System.Action OnDeath;
    public System.Action<WeaponData> OnWeaponChanged;
    
    void Awake()
    {
        // Si no hay anchor asignado, usar el transform del enemigo
        if (weaponAnchor == null)
            weaponAnchor = transform;
    }
    
    void Start()
    {
        // Si hay un arma configurada en el Inspector, inicializar su visual y pool
        if (HasWeapon)
        {
            CreateWeaponVisual();
            InitializeBulletPool();
        }
    }
    
    /// <summary>
    /// Recibe daño
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (IsDead)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Cura al enemigo
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead) return;
        
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    private void Die()
    {
        OnDeath?.Invoke();

        if (HasWeapon)
        {
            DropWeapon();
        }
    }
    
    /// <summary>
    /// Equipa un arma desde sus datos. Retorna los datos del arma anterior si había.
    /// </summary>
    public WeaponData EquipWeapon(WeaponData newWeapon)
    {
        WeaponData previousWeapon = equippedWeapon;
        
        // Destruir visual anterior
        DestroyWeaponVisual();
        
        // Guardar datos del arma nueva
        equippedWeapon = newWeapon;
        
        // Crear visual del arma nueva
        if (HasWeapon)
        {
            CreateWeaponVisual();
            InitializeBulletPool();
        }

        OnWeaponChanged?.Invoke(equippedWeapon);

        return previousWeapon;
    }

    public WeaponData EquipWeapon(WeaponType type, int bullets)
    {
        return EquipWeapon(new WeaponData(type, bullets));
    }
    
    private void CreateWeaponVisual()
    {
        if (!HasWeapon || equippedWeapon.weaponType.equippedPrefab == null)
            return;
        
        WeaponType type = equippedWeapon.weaponType;
        
        // Instanciar visual como hijo del anchor
        equippedWeaponVisual = Instantiate(type.equippedPrefab, weaponAnchor);
        equippedWeaponVisual.transform.localPosition = type.equippedPositionOffset;
        equippedWeaponVisual.transform.localRotation = Quaternion.Euler(type.equippedRotationOffset);
        equippedWeaponVisual.name = $"Equipped_{type.weaponName}";
    }
    
    private void DestroyWeaponVisual()
    {
        if (equippedWeaponVisual != null)
        {
            Destroy(equippedWeaponVisual);
            equippedWeaponVisual = null;
        }
    }
    
    /// <summary>
    /// Muestra u oculta el arma equipada de tercera persona (para alternar con vista FPS)
    /// </summary>
    public void SetWeaponVisualVisible(bool visible)
    {
        if (equippedWeaponVisual != null)
        {
            equippedWeaponVisual.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Suelta el arma actual y la instancia como pickup en el suelo
    /// </summary>
    public void DropWeapon()
    {
        if (!HasWeapon) return;
        
        WeaponType type = equippedWeapon.weaponType;
        int bullets = equippedWeapon.currentBullets;
        
        // Crear pickup en el suelo
        if (type.pickupPrefab != null)
        {
            Vector3 dropPosition = transform.position + Vector3.up * 0.5f + transform.forward * 0.5f;
            GameObject pickup = Instantiate(type.pickupPrefab, dropPosition, Quaternion.identity);
            
            // Configurar el pickup con las balas actuales
            WeaponPickup pickupScript = pickup.GetComponent<WeaponPickup>();
            if (pickupScript != null)
                pickupScript.Initialize(type, bullets);
        }
        
        // Limpiar
        DestroyWeaponVisual();
        CleanupBulletPool();
        equippedWeapon = null;
        
        OnWeaponChanged?.Invoke(null);
    }
    
    /// <summary>
    /// Intenta disparar con el arma equipada
    /// </summary>
    public bool TryFire(Vector3 direction)
    {
        if (!HasWeapon)
            return false;

        WeaponType type = equippedWeapon.weaponType;

        if (Time.time < nextFireTime)
            return false;

        if (!equippedWeapon.ConsumeBullet())
            return false;
        
        nextFireTime = Time.time + type.fireRate;
        
        // Calcular origen del disparo
        Vector3 origin = weaponAnchor.position + weaponAnchor.TransformDirection(type.equippedPositionOffset + type.muzzleOffset);
        
        SpawnBullet(origin, direction);
        return true;
    }
    
    /// <summary>
    /// Obtiene la posición del cañón del arma equipada
    /// </summary>
    public Vector3 GetMuzzlePosition()
    {
        if (!HasWeapon || weaponAnchor == null) return transform.position;
        
        WeaponType type = equippedWeapon.weaponType;
        return weaponAnchor.position + weaponAnchor.TransformDirection(type.equippedPositionOffset + type.muzzleOffset);
    }
    
    private void SpawnBullet(Vector3 origin, Vector3 direction)
    {
        if (!HasWeapon || equippedWeapon.weaponType.bulletPrefab == null)
            return;
        
        GameObject bullet = GetPooledBullet();
        if (bullet == null)
        {
            Debug.LogWarning("No hay balas disponibles en el pool");
            return;
        }
        
        bullet.transform.position = origin;
        bullet.transform.rotation = Quaternion.LookRotation(direction);
        bullet.SetActive(true);
        
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            WeaponType type = equippedWeapon.weaponType;
            bulletScript.Launch(direction, type.bulletSpeed, type.bulletLifetime, type.damage);
        }
    }
    
    private void InitializeBulletPool()
    {
        CleanupBulletPool();
        
        if (!HasWeapon || equippedWeapon.weaponType.bulletPrefab == null)
            return;
        
        // Crear contenedor para las balas
        GameObject poolObj = new GameObject($"BulletPool_{equippedWeapon.weaponType.weaponName}");
        bulletPoolParent = poolObj.transform;
        
        for (int i = 0; i < bulletPoolSize; i++)
        {
            GameObject bullet = Instantiate(equippedWeapon.weaponType.bulletPrefab, bulletPoolParent);
            bullet.SetActive(false);
            bulletPool.Add(bullet);
        }
    }
    
    private void CleanupBulletPool()
    {
        bulletPool.Clear();
        if (bulletPoolParent != null)
        {
            Destroy(bulletPoolParent.gameObject);
            bulletPoolParent = null;
        }
    }
    
    private GameObject GetPooledBullet()
    {
        foreach (var bullet in bulletPool)
        {
            if (!bullet.activeInHierarchy)
                return bullet;
        }
        
        // Pool agotado, crear una bala extra
        if (HasWeapon && equippedWeapon.weaponType.bulletPrefab != null)
        {
            GameObject newBullet = Instantiate(equippedWeapon.weaponType.bulletPrefab, bulletPoolParent);
            bulletPool.Add(newBullet);
            return newBullet;
        }
        
        return null;
    }
    
    /// <summary>
    /// Añade balas al arma equipada si el cargador es compatible
    /// </summary>
    public bool AddMagazine(WeaponType magazineType)
    {
        if (!HasWeapon)
            return false;

        if (equippedWeapon.weaponType != magazineType)
            return false;

        int added = equippedWeapon.AddBullets(magazineType.bulletsPerMagazine);
        return added > 0;
    }
    
    void OnDestroy()
    {
        CleanupBulletPool();
    }
}
