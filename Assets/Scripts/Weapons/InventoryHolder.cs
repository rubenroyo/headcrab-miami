using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona el inventario de un enemigo: vida y arma equipada.
/// El disparo hitscan se delega en HitscanShooter.
/// </summary>
public class InventoryHolder : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Arma Equipada")]
    [SerializeField] private WeaponData equippedWeapon;

    [Header("Punto de Anclaje del Arma")]
    [Tooltip("Transform del hueso de la mano derecha. Si es null, usa el transform del enemigo.")]
    [SerializeField] private Transform weaponAnchor;

    // Visual del arma equipada
    private GameObject equippedWeaponVisual;
    private WeaponVisual equippedWeaponVisualComponent;

    private float nextFireTime = 0f;

    // Referencia al shooter — se busca en Awake, puede ser null si no está en el GO
    private HitscanShooter hitscanShooter;

    private EnemyController enemyController;

    // Propiedades públicas
    public float MaxHealth     => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool  IsDead        => currentHealth <= 0f;
    public WeaponData EquippedWeapon => equippedWeapon;
    public bool HasWeapon => equippedWeapon != null && equippedWeapon.weaponType != null;

    // Eventos
    public System.Action<float, float> OnHealthChanged;
    public System.Action               OnDeath;
    public System.Action<WeaponData>   OnWeaponChanged;

    void Awake()
    {
        if (weaponAnchor == null)
            weaponAnchor = transform;

        hitscanShooter = GetComponent<HitscanShooter>();
        enemyController = GetComponent<EnemyController>();
    }

    void Start()
    {
        if (HasWeapon)
            CreateWeaponVisual();
    }

    // ─────────────────────────────────────────────
    //  VIDA
    // ─────────────────────────────────────────────

    public void TakeDamage(float damage)
    {
        if (IsDead) return;
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        if (IsDead) Die();
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        OnDeath?.Invoke();
        if (HasWeapon) DropWeapon();
    }

    // ─────────────────────────────────────────────
    //  DISPARO
    // ─────────────────────────────────────────────

    /// <summary>
    /// Intenta disparar. Comprueba cooldown y munición.
    /// El raycast y los efectos visuales los gestiona HitscanShooter.
    /// </summary>
    /// <param name="shooterCamera">Cámara desde la que sale el raycast central.</param>
    /// <param name="state">Estado de movimiento del portador.</param>
    /// <param name="handTremor">Temblor del enemigo portador.</param>
    public bool TryFire(Camera shooterCamera, WeaponState state = WeaponState.Idle, float handTremor = 0f)
    {
        if (!HasWeapon)                           return false;
        if (Time.time < nextFireTime)             return false;
        if (!equippedWeapon.ConsumeBullet())      return false;
        if (hitscanShooter == null)
        {
            Debug.LogWarning("[InventoryHolder] No hay HitscanShooter en el GameObject. Añádelo.");
            return false;
        }

        nextFireTime = Time.time + equippedWeapon.weaponType.fireRate;
        return hitscanShooter.Fire(shooterCamera, state, handTremor);
    }

    /// <summary>
    /// Sobrecarga legacy que acepta una dirección precalculada.
    /// Se usa para la IA que no tiene cámara propia.
    /// </summary>
    public bool TryFireInDirection(Vector3 direction, WeaponState state = WeaponState.Idle, float handTremor = 0f)
    {
        if (!HasWeapon)                       return false;
        if (Time.time < nextFireTime)         return false;
        if (!equippedWeapon.ConsumeBullet())  return false;
        if (hitscanShooter == null)
        {
            Debug.LogWarning("[InventoryHolder] No hay HitscanShooter en el GameObject.");
            return false;
        }

        nextFireTime = Time.time + equippedWeapon.weaponType.fireRate;

        // Para la IA: fingimos que la dirección ya es la dirección final dispersada.
        // HitscanShooter hace el raycast desde el muzzle en esa dirección directamente.
        return hitscanShooter.FireInDirection(direction, state, handTremor);
    }

    // ─────────────────────────────────────────────
    //  POSICIÓN DEL CAÑÓN
    // ─────────────────────────────────────────────

    public Vector3 GetMuzzlePosition()
    {
        if (enemyController != null && enemyController.MuzzlePoint != null)
            return enemyController.MuzzlePoint.position;

        if (equippedWeaponVisualComponent != null && equippedWeaponVisualComponent.IsValid)
            return equippedWeaponVisualComponent.MuzzlePosition;

        if (weaponAnchor != null)
            return weaponAnchor.position;

        return transform.position;
    }

    public Vector3 GetMuzzleForward()
    {
        if (equippedWeaponVisualComponent != null && equippedWeaponVisualComponent.IsValid)
            return equippedWeaponVisualComponent.MuzzleForward;

        return transform.forward;
    }

    // ─────────────────────────────────────────────
    //  ARMA
    // ─────────────────────────────────────────────

    public WeaponData EquipWeapon(WeaponData newWeapon)
    {
        WeaponData previousWeapon = equippedWeapon;
        DestroyWeaponVisual();
        equippedWeapon = newWeapon;

        if (HasWeapon)
            CreateWeaponVisual();

        OnWeaponChanged?.Invoke(equippedWeapon);
        return previousWeapon;
    }

    public WeaponData EquipWeapon(WeaponType type, int bullets)
        => EquipWeapon(new WeaponData(type, bullets));

    private void CreateWeaponVisual()
    {
        if (!HasWeapon || equippedWeapon.weaponType.equippedPrefab == null) return;

        WeaponType type = equippedWeapon.weaponType;
        equippedWeaponVisual = Instantiate(type.equippedPrefab, weaponAnchor);
        equippedWeaponVisual.transform.localPosition = type.equippedPositionOffset;
        equippedWeaponVisual.transform.localRotation = Quaternion.Euler(type.equippedRotationOffset);
        equippedWeaponVisual.name = $"Equipped_{type.weaponName}";

        equippedWeaponVisualComponent = equippedWeaponVisual.GetComponent<WeaponVisual>();

        if (equippedWeaponVisualComponent == null)
            Debug.LogWarning($"[{name}] El prefab '{type.equippedPrefab.name}' no tiene WeaponVisual.");
    }

    private void DestroyWeaponVisual()
    {
        if (equippedWeaponVisual != null)
        {
            Destroy(equippedWeaponVisual);
            equippedWeaponVisual = null;
            equippedWeaponVisualComponent = null;
        }
    }

    public void SetWeaponVisualVisible(bool visible)
    {
        if (equippedWeaponVisual != null)
            equippedWeaponVisual.SetActive(visible);
    }

    public void DropWeapon()
    {
        if (!HasWeapon) return;

        WeaponType type    = equippedWeapon.weaponType;
        int        bullets = equippedWeapon.currentBullets;

        if (type.pickupPrefab != null)
        {
            Vector3    dropPosition = transform.position + Vector3.up * 0.5f + transform.forward * 0.5f;
            GameObject pickup       = Instantiate(type.pickupPrefab, dropPosition, Quaternion.identity);
            WeaponPickup pickupScript = pickup.GetComponent<WeaponPickup>();
            if (pickupScript != null) pickupScript.Initialize(type, bullets);
        }

        DestroyWeaponVisual();
        equippedWeapon = null;
        OnWeaponChanged?.Invoke(null);
    }

    public bool AddMagazine(WeaponType magazineType)
    {
        if (!HasWeapon) return false;
        if (equippedWeapon.weaponType != magazineType) return false;
        return equippedWeapon.AddBullets(magazineType.bulletsPerMagazine) > 0;
    }
}
