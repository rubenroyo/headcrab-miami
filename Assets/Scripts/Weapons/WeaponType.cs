using UnityEngine;

/// <summary>
/// Define un tipo de arma (ej: Glock, Rifle).
/// ScriptableObject para configurar en el editor.
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponType", menuName = "Headcrab/Weapon Type")]
public class WeaponType : ScriptableObject
{
    [Header("Identificación")]
    public string weaponName = "Pistol";
    
    [Header("Munición")]
    [Tooltip("Balas que añade un cargador de este tipo")]
    public int bulletsPerMagazine = 10;
    
    [Tooltip("Máximo de balas que puede tener el arma")]
    public int maxBullets = 30;
    
    [Header("Disparo")]
    [Tooltip("Segundos entre disparos")]
    public float fireRate = 0.25f;
    
    [Tooltip("Velocidad de la bala")]
    public float bulletSpeed = 25f;
    
    [Tooltip("Tiempo de vida de la bala en segundos")]
    public float bulletLifetime = 3f;
    
    [Header("Prefabs")]
    [Tooltip("Prefab de la bala a instanciar")]
    public GameObject bulletPrefab;
    
    [Tooltip("Prefab del arma en el SUELO (con collider trigger para recoger)")]
    public GameObject pickupPrefab;
    
    [Tooltip("Prefab del arma EQUIPADA (visual en la mano del enemigo, sin física)")]
    public GameObject equippedPrefab;
    
    [Header("Posicionamiento Equipada")]
    [Tooltip("Offset local desde el punto de anclaje del enemigo")]
    public Vector3 equippedPositionOffset = new Vector3(0f, 0.5f, 0.75f);
    
    [Tooltip("Rotación local del arma equipada")]
    public Vector3 equippedRotationOffset = Vector3.zero;
    
    [Header("Disparo")]
    [Tooltip("Offset desde el arma equipada para spawnear balas")]
    public Vector3 muzzleOffset = new Vector3(0f, 0f, 0.5f);
}
