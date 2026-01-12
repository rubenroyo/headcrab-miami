using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona el arma equipada y el inventario de cargadores del jugador.
/// </summary>
public class WeaponController : MonoBehaviour
{
    // Arma equipada
    private Weapon equippedWeapon;

    // Inventario de cargadores (munición extra)
    private int magazineInventory = 0;

    [Header("Disparo")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 16;
    [SerializeField] private float bulletSpeed = 25f;
    [SerializeField] private float bulletLifetime = 3f;
    [SerializeField] private float fireRate = 0.25f; // segundos entre disparos
    [SerializeField] private float muzzleOffset = 0.75f;
    [SerializeField] private Transform bulletPoolParent; // opcional: usar pool ya presente en escena

    private readonly List<GameObject> bulletPool = new List<GameObject>();
    private float nextFireTime = 0f;
    private Transform weaponTransform; // referencia al modelo/objeto del arma (para spawn)

    // Eventos para UI (opcional, por ahora es comentario)
    // public delegate void WeaponChangedHandler(Weapon weapon);
    // public event WeaponChangedHandler OnWeaponChanged;

    public Weapon EquippedWeapon => equippedWeapon;
    public int MagazineCount => magazineInventory;

    /// <summary>
    /// Añade balas directamente al arma equipada (ej. recoger magazine en el suelo).
    /// Devuelve la cantidad efectivamente añadida.
    /// </summary>
    public int AddAmmoToEquipped(int amount)
    {
        if (equippedWeapon == null || amount <= 0)
            return 0;

        int before = equippedWeapon.currentAmmo;
        equippedWeapon.Reload(amount);
        return equippedWeapon.currentAmmo - before;
    }

    /// <summary>
    /// Equipa un arma y la hace disponible para disparar.
    /// </summary>
    public void EquipWeapon(Weapon weapon)
    {
        if (weapon == null) return;

        equippedWeapon = weapon;
        Debug.Log($"Arma equipada: {weapon.weaponName} con {weapon.currentAmmo} balas");
        // OnWeaponChanged?.Invoke(weapon);
    }

    /// <summary>
    /// Establece el transform del arma física (para spawn de balas)
    /// </summary>
    public void SetWeaponTransform(Transform weapon)
    {
        weaponTransform = weapon;
        EnsurePool();
    }

    /// <summary>
    /// Intenta disparar con el arma equipada.
    /// Retorna true si disparó exitosamente, false si no hay munición.
    /// </summary>
    public bool TryFire()
    {
        if (equippedWeapon == null)
        {
            Debug.LogWarning("No hay arma equipada");
            return false;
        }

        // Cadencia
        if (Time.time < nextFireTime)
        {
            Debug.Log($"Intentando disparar pero en cooldown. Siguiente disparo: {nextFireTime - Time.time:0.00}s");
            return false;
        }

        Debug.Log($"Intento de disparo con {equippedWeapon.currentAmmo} balas en cargador");

        if (equippedWeapon.TryFire())
        {
            SpawnBullet();
            nextFireTime = Time.time + fireRate;
            Debug.Log($"¡Disparo! Munición restante: {equippedWeapon.currentAmmo}");
            return true;
        }

        Debug.Log("No hay munición. Presiona R para recargar.");
        return false;
    }

    /// <summary>
    /// Intenta recargar el arma usando un cargador del inventario.
    /// Retorna true si recargó exitosamente, false si no hay cargadores.
    /// </summary>
    public bool TryReload()
    {
        if (equippedWeapon == null)
        {
            Debug.LogWarning("No hay arma equipada");
            return false;
        }

        if (magazineInventory <= 0)
        {
            Debug.Log("No hay cargadores en el inventario");
            return false;
        }

        // Consumir un cargador
        magazineInventory--;

        // Recargar el arma
        equippedWeapon.FillMagazine();

        Debug.Log($"Recargado. Cargadores restantes: {magazineInventory}");
        return true;
    }

    /// <summary>
    /// Añade cargadores al inventario.
    /// </summary>
    public void AddMagazine(int count = 1)
    {
        magazineInventory += count;
        Debug.Log($"Cargadores recogidos. Total: {magazineInventory}");
    }

    /// <summary>
    /// Retorna información sobre el arma y munición (para debug/UI).
    /// </summary>
    public string GetWeaponStatus()
    {
        if (equippedWeapon == null)
            return "Sin arma equipada";

        return $"{equippedWeapon.weaponName} - Munición: {equippedWeapon.currentAmmo}/{equippedWeapon.maxAmmoPerMagazine} | Cargadores: {magazineInventory}";
    }

    // ---------------- BULLET POOL ----------------

    private void SpawnBullet()
    {
        if (bulletPrefab == null || weaponTransform == null)
            return;

        GameObject bullet = GetBulletFromPool();
        if (bullet == null) return;

        bullet.transform.position = weaponTransform.position + weaponTransform.forward * muzzleOffset;
        bullet.transform.rotation = weaponTransform.rotation;

        bullet.SetActive(true);
        var bulletComp = bullet.GetComponent<Bullet>();
        if (bulletComp != null)
        {
            bulletComp.Launch(weaponTransform.forward, bulletSpeed, bulletLifetime);
            Debug.Log($"Disparando bala desde {bullet.transform.position} dir {weaponTransform.forward} | pool activa: {CountActiveBullets()}");
        }
    }

    private GameObject GetBulletFromPool()
    {
        foreach (var b in bulletPool)
        {
            if (!b.activeInHierarchy)
                return b;
        }

        // Si se agota el pool, instanciar uno extra de forma controlada
        var extra = Instantiate(bulletPrefab, bulletPoolParent);
        extra.SetActive(false);
        bulletPool.Add(extra);
        return extra;
    }

    /// <summary>
    /// Prepara el pool usando balas ya existentes bajo bulletPoolParent o instanciando nuevas.
    /// </summary>
    private void EnsurePool()
    {
        if (bulletPool.Count > 0 || bulletPrefab == null)
            return;

        // Usar balas ya colocadas en escena bajo el parent
        if (bulletPoolParent != null)
        {
            foreach (Transform child in bulletPoolParent)
            {
                if (child.GetComponent<Bullet>() != null)
                {
                    child.gameObject.SetActive(false);
                    bulletPool.Add(child.gameObject);
                }
            }
        }

        // Completar hasta poolSize
        for (int i = bulletPool.Count; i < poolSize; i++)
        {
            var b = Instantiate(bulletPrefab, bulletPoolParent);
            b.SetActive(false);
            bulletPool.Add(b);
        }
    }

    private int CountActiveBullets()
    {
        int count = 0;
        foreach (var b in bulletPool)
        {
            if (b.activeInHierarchy) count++;
        }
        return count;
    }
}
