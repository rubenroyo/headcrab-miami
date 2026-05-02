using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sistema de disparo hitscan.
/// Colócalo en el mismo GameObject que InventoryHolder.
/// 
/// Flujo de disparo (modo jugador FPS):
///   1. Raycast desde el centro de la cámara → hitPoint (o punto lejano si skybox)
///   2. Dirección desde muzzle → hitPoint, dispersada según WeaponType + WeaponState
///   3. Segundo raycast desde muzzle en esa dirección → impacto real + daño
///   4. BulletTracer de muzzle a impacto real + BulletHole si aplica
///
/// Flujo de disparo (modo IA):
///   1. Dirección precalculada por EnemyAI → dispersión aplicada desde muzzle
///   2. Raycast desde muzzle → impacto real + daño + tracer
/// </summary>
public class HitscanShooter : MonoBehaviour
{
    [Header("Pool de Tracers")]
    [Tooltip("Tamaño inicial del pool. Se expande automáticamente si se agota.")]
    [SerializeField] private int tracerPoolSize = 16;

    // ─────────────────────────────────────────────
    //  PRIVADOS
    // ─────────────────────────────────────────────

    private InventoryHolder inventory;

    private readonly List<GameObject> tracerPool = new List<GameObject>();
    private GameObject tracerPoolParent;
    private GameObject currentTracerPrefab;

    private int wallLayer;
    private int enemyLayer;

    // Campo privado a añadir al inicio de la clase:
    private CinemachineCameraController cameraController;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Awake()
    {
        inventory  = GetComponent<InventoryHolder>();
        wallLayer  = LayerMask.NameToLayer("Wall");
        enemyLayer = LayerMask.NameToLayer("Enemy");
        cameraController = FindFirstObjectByType<CinemachineCameraController>();

        if (inventory == null)
            Debug.LogWarning("[HitscanShooter] No se encontró InventoryHolder en el mismo GameObject.");
    }

    void OnDestroy() => CleanupPool();

    // ─────────────────────────────────────────────
    //  API PÚBLICA — Jugador FPS (con cámara)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Dispara desde el centro de la cámara indicada.
    /// Uso exclusivo del jugador en modo posesión FPS.
    /// </summary>
    public bool Fire(Camera shooterCamera, WeaponState state, float handTremor = 0f)
    {
        if (inventory == null || !inventory.HasWeapon) return false;
        if (shooterCamera == null)
        {
            Debug.LogWarning("[HitscanShooter] Camera es null.");
            return false;
        }

        WeaponType weaponType = inventory.EquippedWeapon.weaponType;

        // 1. Raycast central desde cámara → hitPoint
        Ray     centerRay = shooterCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 hitPoint;

        if (Physics.Raycast(centerRay, out RaycastHit cameraHit,
            weaponType.maxHitscanDistance, weaponType.impactLayerMask,
            QueryTriggerInteraction.Ignore))
        {
            hitPoint = cameraHit.point;
        }
        else
        {
            hitPoint = centerRay.GetPoint(weaponType.maxHitscanDistance);
        }

        // 2–4. Un raycast por pellet (pelletsPerShot=1 normal, >1 escopeta)
        Vector3 muzzlePos     = inventory.GetMuzzlePosition();
        Vector3 baseDirection = (hitPoint - muzzlePos).normalized;
        float   dispersion    = weaponType.GetDispersion(state, handTremor);
        int     pellets       = Mathf.Max(1, weaponType.pelletsPerShot);

        for (int i = 0; i < pellets; i++)
        {
            Vector3 finalDir = ApplyDispersion(baseDirection, dispersion);
            Vector3 impact   = muzzlePos + finalDir * weaponType.maxHitscanDistance;

            if (Physics.Raycast(muzzlePos, finalDir, out RaycastHit muzzleHit,
                weaponType.maxHitscanDistance, weaponType.impactLayerMask,
                QueryTriggerInteraction.Ignore))
            {
                impact = muzzleHit.point;
                ProcessImpact(muzzleHit, finalDir, weaponType.damage);
            }

            SpawnTracer(muzzlePos, impact, weaponType);
        }

        TriggerFireShake(weaponType.fireShakeIntensity);

        return true;
    }

    // ─────────────────────────────────────────────
    //  API PÚBLICA — IA (sin cámara)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Dispara en una dirección precalculada.
    /// Uso exclusivo de la IA — no tiene cámara propia.
    /// La dispersión se aplica aquí desde el muzzle.
    /// </summary>
    public bool FireInDirection(Vector3 direction, WeaponState state, float handTremor = 0f)
    {
        if (inventory == null || !inventory.HasWeapon) return false;

        WeaponType weaponType = inventory.EquippedWeapon.weaponType;
        Vector3    muzzlePos  = inventory.GetMuzzlePosition();
        float      dispersion = weaponType.GetDispersion(state, handTremor);

        // La IA dispara 1 pellet aunque el arma sea escopeta — evita que sea injugable.
        // Para N pellets en la IA, cambia 1 por weaponType.pelletsPerShot.
        for (int i = 0; i < 1; i++)
        {
            Vector3 finalDir = ApplyDispersion(direction.normalized, dispersion);
            Vector3 impact   = muzzlePos + finalDir * weaponType.maxHitscanDistance;

            if (Physics.Raycast(muzzlePos, finalDir, out RaycastHit hit,
                weaponType.maxHitscanDistance, weaponType.impactLayerMask,
                QueryTriggerInteraction.Ignore))
            {
                impact = hit.point;
                ProcessImpact(hit, finalDir, weaponType.damage);
            }

            SpawnTracer(muzzlePos, impact, weaponType);
        }

        TriggerFireShake(weaponType.fireShakeIntensity);

        return true;
    }

    // ─────────────────────────────────────────────
    //  IMPACTO
    // ─────────────────────────────────────────────

    private void ProcessImpact(RaycastHit hit, Vector3 direction, float damage)
    {
        EnemyController enemy = hit.collider.GetComponentInParent<EnemyController>();
        if (enemy != null && !enemy.IsPossessed && !enemy.IsDead)
        {
            enemy.TakeDamage(damage, hit.point, direction);
            return;
        }

        if (hit.collider.gameObject.layer == wallLayer)
        {
            if (BulletHoleManager.Instance != null)
                BulletHoleManager.Instance.SpawnBulletHole(hit.point, hit.normal);
        }
    }

    // ─────────────────────────────────────────────
    //  DISPERSIÓN
    // ─────────────────────────────────────────────

    private Vector3 ApplyDispersion(Vector3 direction, float degrees)
    {
        if (degrees <= 0f) return direction;

        float angle  = Random.Range(0f, degrees);
        float rotate = Random.Range(0f, 360f);

        Vector3 perp = Vector3.Cross(direction, Vector3.up);
        if (perp.sqrMagnitude < 0.001f)
            perp = Vector3.Cross(direction, Vector3.right);

        Quaternion dispersion = Quaternion.AngleAxis(rotate, direction) *
                                Quaternion.AngleAxis(angle,  perp.normalized);
        return dispersion * direction;
    }

    // ─────────────────────────────────────────────
    //  TRACERS — pool
    // ─────────────────────────────────────────────

    private void SpawnTracer(Vector3 origin, Vector3 destination, WeaponType weaponType)
    {
        if (weaponType.tracerPrefab == null) return;

        if (weaponType.tracerPrefab != currentTracerPrefab)
            RebuildPool(weaponType.tracerPrefab);

        GameObject tracerGO = GetPooledTracer();
        if (tracerGO == null) return;

        tracerGO.transform.position = origin;
        tracerGO.SetActive(true);

        BulletTracer tracer = tracerGO.GetComponent<BulletTracer>();
        if (tracer != null)
            tracer.Play(origin, destination, weaponType.tracerDuration);
    }

    private void RebuildPool(GameObject prefab)
    {
        CleanupPool();
        currentTracerPrefab = prefab;

        tracerPoolParent = new GameObject($"TracerPool_{prefab.name}");

        for (int i = 0; i < tracerPoolSize; i++)
        {
            GameObject go = Instantiate(prefab, tracerPoolParent.transform);
            go.SetActive(false);
            tracerPool.Add(go);
        }
    }

    private GameObject GetPooledTracer()
    {
        foreach (var go in tracerPool)
            if (!go.activeInHierarchy) return go;

        if (currentTracerPrefab != null)
        {
            GameObject go = Instantiate(currentTracerPrefab, tracerPoolParent.transform);
            tracerPool.Add(go);
            return go;
        }

        return null;
    }

    private void CleanupPool()
    {
        tracerPool.Clear();
        if (tracerPoolParent != null)
        {
            Destroy(tracerPoolParent);
            tracerPoolParent = null;
        }
        currentTracerPrefab = null;
    }

    private void TriggerFireShake(float intensity)
    {
        if (intensity <= 0f) return;
        
        // Buscar el CinemachineCameraController en escena
        // (se puede cachear en Awake para evitar FindFirstObjectByType cada frame)
        if (cameraController == null)
            cameraController = FindFirstObjectByType<CinemachineCameraController>();
        
        if (cameraController != null)
            cameraController.TriggerShake(intensity);
    }
}
