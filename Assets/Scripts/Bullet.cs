using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float speed;
    private float lifetime;
    private float spawnTime;
    private bool launched = false;

    private Rigidbody rb;
    private int wallLayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        wallLayer = LayerMask.NameToLayer("Wall");
    }

    void OnEnable()
    {
        spawnTime = Time.time;
        // Restablecer flag por si la bala venía desactivada del pool
        launched = false;
    }

    void Update()
    {
        if (!launched) return;

        // Mover hacia adelante
        transform.position += transform.forward * (speed * Time.deltaTime);

        // Desactivar por tiempo
        if (Time.time - spawnTime >= lifetime)
            gameObject.SetActive(false);
    }

    public void Launch(Vector3 direction, float newSpeed, float lifeTime)
    {
        transform.forward = direction.normalized;
        speed = newSpeed;
        lifetime = lifeTime;
        spawnTime = Time.time;
        launched = true;
        Debug.Log($"Bullet launch dir {direction.normalized} speed {speed} lifetime {lifetime}");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Desactivar al tocar paredes; dejamos enemigos para más adelante
        int otherLayer = other.gameObject.layer;
        if (otherLayer == wallLayer)
        {
            gameObject.SetActive(false);
        }
    }
}
