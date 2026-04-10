using UnityEngine;

/// <summary>
/// Cargador en el suelo que añade balas al arma compatible.
/// Se destruye al ser recogido.
/// La IA puede recogerlo automáticamente con PickUpByAI().
/// </summary>
[RequireComponent(typeof(Collider))]
public class MagazinePickup : MonoBehaviour
{
    [SerializeField] private WeaponType weaponType;
    
    private bool isPickedUp = false;
    
    public WeaponType WeaponType => weaponType;
    public bool CanBePickedUp => !isPickedUp && weaponType != null;
    public bool IsPickedUp => isPickedUp;
    
    void Start()
    {
        // Asegurar que el collider es trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (isPickedUp) return;
        if (weaponType == null) return;
        
        // Solo enemigos poseídos pueden recoger cargadores por trigger
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy == null || !enemy.IsPossessed)
            return;
        
        InventoryHolder inventory = enemy.GetComponent<InventoryHolder>();
        if (inventory == null)
            return;
        
        // Intentar añadir el cargador
        if (inventory.AddMagazine(weaponType))
        {
            isPickedUp = true;
            Debug.Log($"Recogido cargador de {weaponType.weaponName}");
            Destroy(gameObject);
        }
        else
        {
            Debug.Log($"No se pudo recoger cargador de {weaponType.weaponName} (arma incompatible o sin arma)");
        }
    }
    
    /// <summary>
    /// Recoge el cargador por la IA (sin necesidad de trigger)
    /// </summary>
    public bool PickUpByAI(InventoryHolder inventory)
    {
        if (isPickedUp || weaponType == null || inventory == null) return false;
        
        if (inventory.AddMagazine(weaponType))
        {
            isPickedUp = true;
            Debug.Log($"IA recogió cargador de {weaponType.weaponName}");
            Destroy(gameObject);
            return true;
        }
        
        return false;
    }
}
