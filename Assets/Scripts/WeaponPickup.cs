using UnityEngine;

/// <summary>
/// Representa un arma o cargador en el mundo que se puede recoger.
/// Se coloca en cubos del editor de Unity.
/// El jugador se acerca y presiona E para recoger.
/// </summary>
public class WeaponPickup : MonoBehaviour
{
    public enum PickupType
    {
        Weapon,    // Un arma con munición
        Magazine   // Solo cargador (munición extra)
    }

    [SerializeField] private PickupType pickupType = PickupType.Weapon;
    [SerializeField] private string weaponName = "Pistol";
    [SerializeField] private int ammoCount = 15; // Balas que tiene si es arma, o balas por cargador si es magazine
    [SerializeField] private float pickupRange = 2f;

    private bool wasPickedUp = false;
    private PlayerController playerController;
    private WeaponController weaponController;

    void Start()
    {
        // Buscar el PlayerController en la escena
        playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
            weaponController = playerController.GetComponent<WeaponController>();

        // Cambiar color para visualizar en editor
        SetVisualType();
    }

    // Recogida automática en colisión; no se usa Update ni tecla E
    void Update() { }

    private void OnTriggerEnter(Collider other)
    {
        if (wasPickedUp) return;

        // Necesitamos un enemigo poseído y un PlayerController en estado Possessing
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy == null || !enemy.IsPossessed)
            return;

        if (playerController == null)
            playerController = FindObjectOfType<PlayerController>();

        if (playerController == null)
            return;

        var stateProp = playerController.GetType().GetProperty("CurrentState");
        if (stateProp != null)
        {
            var currentState = stateProp.GetValue(playerController, null);
            if (currentState == null || currentState.ToString() != "Possessing")
                return;
        }

        // Obtener WeaponController del jugador
        if (weaponController == null)
            weaponController = playerController.GetComponent<WeaponController>();

        if (weaponController == null)
            return;

        HandlePickup(enemy.transform);
    }

    void HandlePickup(Transform enemyTransform)
    {
        if (weaponController == null) return;

        if (pickupType == PickupType.Weapon)
        {
            // Crear arma con su munición
            Weapon newWeapon = new Weapon(weaponName, ammoCount, ammoCount);
            weaponController.EquipWeapon(newWeapon);
            weaponController.SetWeaponTransform(transform);

            // Parent al enemigo poseído para que siga su rotación
            transform.SetParent(enemyTransform);
            transform.localPosition = new Vector3(0f, 0f, 0.75f);
            transform.localRotation = Quaternion.identity;

            // Evitar recolisión pero mantener visible
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            wasPickedUp = true;
            Debug.Log($"¡Recogiste el arma {weaponName} con {ammoCount} balas!");
            return;

        }
        else if (pickupType == PickupType.Magazine)
        {
            // Agregar balas directamente al arma equipada
            int added = weaponController.AddAmmoToEquipped(ammoCount);
            Debug.Log($"¡Recogiste un cargador y añadiste {added} balas!");
            // Desaparece al recoger
            wasPickedUp = true;
            gameObject.SetActive(false);
            return;
        }

        wasPickedUp = true;
    }

    void SetVisualType()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        Material mat = new Material(renderer.material);
        
        if (pickupType == PickupType.Weapon)
        {
            mat.color = Color.cyan; // Cyan para armas
        }
        else
        {
            mat.color = Color.yellow; // Yellow para cargadores
        }
        
        renderer.material = mat;
    }

    // Accesores públicos para tests
    public PickupType GetPickupType => pickupType;
    public string GetWeaponName => weaponName;
    public int GetAmmoCount => ammoCount;
    public float GetPickupRange => pickupRange;
    public bool WasPickedUp => wasPickedUp;
}
