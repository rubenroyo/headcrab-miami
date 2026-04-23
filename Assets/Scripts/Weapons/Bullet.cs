using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float speed;
    private float lifetime;
    private float spawnTime;
    private float damage = 30f;
    private bool isLaunched = false;
    private Vector3 previousPosition;

    private int wallLayer;
    private int enemyLayer;

    void Awake()
    {
        wallLayer = LayerMask.NameToLayer("Wall");
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    void OnEnable()
    {
        spawnTime = Time.time;
        isLaunched = false;
    }

    public void Launch(Vector3 direction, float bulletSpeed, float bulletLifetime, float bulletDamage = 30f)
    {
        transform.forward = direction.normalized;
        speed = bulletSpeed;
        lifetime = bulletLifetime;
        damage = bulletDamage;
        spawnTime = Time.time;
        previousPosition = transform.position;
        isLaunched = true;
    }

    void Update()
    {
        if (!isLaunched) return;

        if (Time.time - spawnTime >= lifetime)
        {
            Deactivate();
            return;
        }

        float stepDistance = speed * Time.deltaTime;
        if (Physics.Raycast(previousPosition, transform.forward, out RaycastHit hit, stepDistance + 0.05f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            HandleHit(hit);
            return;
        }

        transform.position += transform.forward * stepDistance;
        previousPosition = transform.position;
    }

    private void HandleHit(RaycastHit hit)
    {
        transform.position = hit.point;

        EnemyController enemy = hit.collider.GetComponentInParent<EnemyController>();
        if (enemy != null && !enemy.IsPossessed && !enemy.IsDead)
        {
            enemy.TakeDamage(damage, hit.point, transform.forward);
        }
        else if (hit.collider.gameObject.layer == wallLayer)
        {
            SpawnBulletHole(hit.point, hit.normal);
        }

        Deactivate();
    }

    private void SpawnBulletHole(Vector3 point, Vector3 normal)
    {
        if (BulletHoleManager.Instance != null)
            BulletHoleManager.Instance.SpawnBulletHole(point, normal);
    }

    private void Deactivate()
    {
        isLaunched = false;
        gameObject.SetActive(false);
    }
}
