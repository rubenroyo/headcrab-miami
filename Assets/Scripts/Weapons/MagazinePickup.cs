using UnityEngine;

/// <summary>
/// Cargador en el suelo que añade balas al arma compatible.
/// Se destruye al ser recogido.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MagazinePickup : MonoBehaviour
{
    [SerializeField] private WeaponType weaponType;
    
    private bool isPickedUp = false;
    
    public WeaponType WeaponType => weaponType;
    
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
        
        // Solo enemigos poseídos pueden recoger cargadores
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
}
