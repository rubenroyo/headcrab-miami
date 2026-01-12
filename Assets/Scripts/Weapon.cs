using UnityEngine;

/// <summary>
/// Representa un arma con su munición actual.
/// Cada instancia de arma tiene su propio contador de balas.
/// Los cargadores se manejan por separado en WeaponController.
/// </summary>
public class Weapon
{
    public string weaponName;
    public int currentAmmo; // Balas en la recámara actual
    public int maxAmmoPerMagazine; // Capacidad de un cargador

    public Weapon(string name, int ammo, int maxAmmo)
    {
        weaponName = name;
        currentAmmo = ammo;
        maxAmmoPerMagazine = maxAmmo;
    }

    /// <summary>
    /// Intenta disparar (consume 1 bala).
    /// Retorna true si tuvo éxito, false si no hay munición.
    /// </summary>
    public bool TryFire()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            Debug.Log($"Weapon.TryFire OK: queda {currentAmmo}");
            return true;
        }
        Debug.Log("Weapon.TryFire sin balas");
        return false;
    }

    /// <summary>
    /// Recarga el arma con una cantidad de balas.
    /// </summary>
    public void Reload(int ammoToAdd)
    {
        currentAmmo = Mathf.Min(currentAmmo + ammoToAdd, maxAmmoPerMagazine);
    }

    /// <summary>
    /// Llena completamente el cargador.
    /// </summary>
    public void FillMagazine()
    {
        currentAmmo = maxAmmoPerMagazine;
    }

    /// <summary>
    /// Retorna true si el arma está sin munición.
    /// </summary>
    public bool IsEmpty => currentAmmo <= 0;

    /// <summary>
    /// Retorna true si el cargador está lleno.
    /// </summary>
    public bool IsFull => currentAmmo >= maxAmmoPerMagazine;

    /// <summary>
    /// Retorna cuántas balas faltan para llenar el cargador.
    /// </summary>
    public int AmmoNeededToFill => maxAmmoPerMagazine - currentAmmo;
}
