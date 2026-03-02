using UnityEngine;

/// <summary>
/// Datos del arma que lleva un enemigo.
/// Clase pura (no MonoBehaviour) que almacena tipo y estado.
/// </summary>
[System.Serializable]
public class WeaponData
{
    public WeaponType weaponType;
    public int currentBullets;
    
    public WeaponData(WeaponType type, int bullets)
    {
        weaponType = type;
        currentBullets = Mathf.Clamp(bullets, 0, type != null ? type.maxBullets : bullets);
    }
    
    /// <summary>
    /// Crea una copia de los datos
    /// </summary>
    public WeaponData Clone()
    {
        return new WeaponData(weaponType, currentBullets);
    }
    
    public bool IsEmpty => currentBullets <= 0;
    public bool IsFull => weaponType != null && currentBullets >= weaponType.maxBullets;
    
    /// <summary>
    /// Consume una bala. Retorna true si había balas.
    /// </summary>
    public bool ConsumeBullet()
    {
        if (currentBullets > 0)
        {
            currentBullets--;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Añade balas (de un cargador compatible)
    /// </summary>
    public int AddBullets(int amount)
    {
        if (weaponType == null) return 0;
        
        int before = currentBullets;
        currentBullets = Mathf.Min(currentBullets + amount, weaponType.maxBullets);
        return currentBullets - before;
    }
}
