using UnityEngine;

/// <summary>
/// Proyectil disparado por un arma.
/// Se mueve en línea recta y se desactiva al colisionar o expirar.
/// </summary>
public class Bullet : MonoBehaviour
{
    private float speed;
    private float lifetime;
    private float spawnTime;
    private bool isLaunched = false;
    
    private int wallLayer;
    private int enemyLayer;
    
    void Awake()
    {
        wallLayer = LayerMask.NameToLayer("Wall");
        enemyLayer = LayerMask.NameToLayer("Enemy");
        
        // Asegurar que tiene Rigidbody para detección de colisiones
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }
    
    void OnEnable()
    {
        spawnTime = Time.time;
        isLaunched = false;
    }
    
    void Update()
    {
        if (!isLaunched) return;
        
        // Mover hacia adelante
        transform.position += transform.forward * speed * Time.deltaTime;
        
        // Desactivar por tiempo
        if (Time.time - spawnTime >= lifetime)
        {
            Deactivate();
        }
    }
    
    /// <summary>
    /// Lanza la bala en una dirección
    /// </summary>
    public void Launch(Vector3 direction, float bulletSpeed, float bulletLifetime)
    {
        transform.forward = direction.normalized;
        speed = bulletSpeed;
        lifetime = bulletLifetime;
        spawnTime = Time.time;
        isLaunched = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        int otherLayer = other.gameObject.layer;
        
        // Impacto con pared
        if (otherLayer == wallLayer)
        {
            Deactivate();
            return;
        }
        
        // Impacto con enemigo (solo si no está poseído)
        if (otherLayer == enemyLayer)
        {
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null && !enemy.IsPossessed)
            {
                // TODO: Aplicar daño al enemigo
                // enemy.TakeDamage(damage);
                Debug.Log($"Bala impactó en {enemy.name}");
                Deactivate();
                return;
            }
        }
    }
    
    private void Deactivate()
    {
        isLaunched = false;
        gameObject.SetActive(false);
    }
}
