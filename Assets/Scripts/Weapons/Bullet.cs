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
            // Crear agujero de bala
            SpawnBulletHole(other);
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
    
    private void SpawnBulletHole(Collider hitCollider)
    {
        if (BulletHoleManager.Instance == null) return;
        
        // Hacer raycast para obtener el punto exacto y la normal
        Vector3 rayOrigin = transform.position - transform.forward * 0.5f;
        Vector3 rayDirection = transform.forward;
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, 2f, 1 << wallLayer))
        {
            BulletHoleManager.Instance.SpawnBulletHole(hit.point, hit.normal);
        }
        else
        {
            // Fallback: usar posición de la bala y normal aproximada
            Vector3 closestPoint = hitCollider.ClosestPoint(transform.position);
            Vector3 normal = (transform.position - closestPoint).normalized;
            if (normal == Vector3.zero) normal = -transform.forward;
            
            BulletHoleManager.Instance.SpawnBulletHole(closestPoint, normal);
        }
    }
    
    private void Deactivate()
    {
        isLaunched = false;
        gameObject.SetActive(false);
    }
}
