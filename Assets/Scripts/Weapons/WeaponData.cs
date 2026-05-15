using UnityEngine;

/// <summary>
/// Datos del arma que lleva un enemigo.
/// Separa las balas en dos bolsas:
///   bulletsInMagazine — balas cargadas actualmente (≤ magazineSize).
///   reserveBullets    — balas en el inventario pendientes de recargar (≤ maxBullets).
/// </summary>
[System.Serializable]
public class WeaponData
{
    public WeaponType weaponType;

    /// <summary>Balas cargadas en el arma (0…magazineSize).</summary>
    public int bulletsInMagazine;

    /// <summary>Balas en reserva, sin cargar (0…maxBullets).</summary>
    public int reserveBullets;

    // ── Retrocompatibilidad: currentBullets apunta al magazine ──────────
    /// <summary>Alias de bulletsInMagazine. Mantenido para no romper código existente.</summary>
    public int currentBullets
    {
        get => bulletsInMagazine;
        set => bulletsInMagazine = value;
    }

    /// <param name="bullets">Se reparten: primero llena el magazine, el resto va a reserva.</param>
    public WeaponData(WeaponType type, int bullets)
    {
        weaponType = type;
        if (type == null) { bulletsInMagazine = bullets; return; }

        bulletsInMagazine = Mathf.Min(bullets, type.magazineSize);
        reserveBullets    = Mathf.Clamp(bullets - bulletsInMagazine, 0, type.maxBullets);
    }

    public WeaponData Clone() => new WeaponData(weaponType, bulletsInMagazine + reserveBullets);

    public bool MagazineEmpty  => bulletsInMagazine <= 0;
    public bool HasReserve     => reserveBullets > 0;
    public bool CanReload      => MagazineEmpty && HasReserve || (!MagazineEmpty && HasReserve);
    public bool IsEmpty        => bulletsInMagazine <= 0 && reserveBullets <= 0;

    /// <summary>
    /// Consume una bala del magazine. Retorna true si había balas.
    /// </summary>
    public bool ConsumeBullet()
    {
        if (bulletsInMagazine <= 0) return false;
        bulletsInMagazine--;
        return true;
    }

    /// <summary>
    /// Recarga: toma balas de la reserva para llenar el magazine.
    /// Retorna el número de balas cargadas (0 si no había reserva).
    /// </summary>
    public int Reload()
    {
        if (weaponType == null || reserveBullets <= 0) return 0;

        int needed  = weaponType.magazineSize - bulletsInMagazine;
        int loaded  = Mathf.Min(needed, reserveBullets);
        bulletsInMagazine += loaded;
        reserveBullets    -= loaded;
        return loaded;
    }

    /// <summary>
    /// Añade balas a la reserva (recogida de un cargador del suelo).
    /// </summary>
    public int AddBullets(int amount)
    {
        if (weaponType == null) return 0;
        int before     = reserveBullets;
        reserveBullets = Mathf.Min(reserveBullets + amount, weaponType.maxBullets);
        return reserveBullets - before;
    }
}
