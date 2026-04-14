using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gestiona la creación y pooling de agujeros de bala (decals).
/// Singleton accesible globalmente.
/// </summary>
public class BulletHoleManager : MonoBehaviour
{
    public static BulletHoleManager Instance { get; private set; }
    
    [Header("Configuración")]
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private int poolSize = 50;
    [SerializeField] private float decalLifetime = 30f;  // Tiempo antes de desvanecerse
    [SerializeField] private float fadeOutDuration = 2f;  // Duración del fade out
    
    [Header("Offset")]
    [Tooltip("Distancia mínima de la superficie para evitar z-fighting")]
    [SerializeField] private float surfaceOffset = 0.001f;
    
    private Queue<GameObject> decalPool = new Queue<GameObject>();
    private List<DecalInstance> activeDecals = new List<DecalInstance>();
    
    private class DecalInstance
    {
        public GameObject gameObject;
        public float spawnTime;
        public Renderer renderer;
        public Color originalColor;
    }
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // Actualizar decals activos (fade out y recycling)
        for (int i = activeDecals.Count - 1; i >= 0; i--)
        {
            var decal = activeDecals[i];
            float elapsed = Time.time - decal.spawnTime;
            
            if (elapsed > decalLifetime + fadeOutDuration)
            {
                // Reciclar el decal
                RecycleDecal(decal);
                activeDecals.RemoveAt(i);
            }
            else if (elapsed > decalLifetime)
            {
                // Aplicar fade out
                float fadeProgress = (elapsed - decalLifetime) / fadeOutDuration;
                Color color = decal.originalColor;
                color.a = Mathf.Lerp(1f, 0f, fadeProgress);
                decal.renderer.material.color = color;
            }
        }
    }
    
    private void InitializePool()
    {
        if (bulletHolePrefab == null)
        {
            Debug.LogError("[BulletHoleManager] No se asignó bulletHolePrefab!");
            return;
        }
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject decal = Instantiate(bulletHolePrefab, transform);
            decal.SetActive(false);
            decalPool.Enqueue(decal);
        }
    }
    
    /// <summary>
    /// Crea un agujero de bala en el punto de impacto
    /// </summary>
    /// <param name="position">Punto de impacto</param>
    /// <param name="normal">Normal de la superficie</param>
    public void SpawnBulletHole(Vector3 position, Vector3 normal)
    {
        GameObject decal = GetFromPool();
        if (decal == null) return;
        
        // Posicionar ligeramente alejado de la superficie para evitar z-fighting
        decal.transform.position = position + normal * surfaceOffset;
        
        // Rotar para que mire hacia afuera de la superficie
        decal.transform.rotation = Quaternion.LookRotation(-normal);
        
        // Rotación aleatoria en el eje Z para variedad visual
        decal.transform.Rotate(Vector3.forward, Random.Range(0f, 360f), Space.Self);
        
        decal.SetActive(true);
        
        // Registrar como activo
        Renderer rend = decal.GetComponent<Renderer>();
        if (rend != null)
        {
            // Resetear alpha
            Color color = rend.material.color;
            color.a = 1f;
            rend.material.color = color;
            
            activeDecals.Add(new DecalInstance
            {
                gameObject = decal,
                spawnTime = Time.time,
                renderer = rend,
                originalColor = color
            });
        }
    }
    
    private GameObject GetFromPool()
    {
        if (decalPool.Count > 0)
        {
            return decalPool.Dequeue();
        }
        
        // Si no hay disponibles, reciclar el más antiguo
        if (activeDecals.Count > 0)
        {
            var oldest = activeDecals[0];
            activeDecals.RemoveAt(0);
            return oldest.gameObject;
        }
        
        return null;
    }
    
    private void RecycleDecal(DecalInstance decal)
    {
        decal.gameObject.SetActive(false);
        decalPool.Enqueue(decal.gameObject);
    }
}
