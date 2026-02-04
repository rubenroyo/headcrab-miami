using UnityEngine;

public class SettingsController : MonoBehaviour
{
    [Header("Configuración de Pixelación")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RenderTexture renderTexture;
    [SerializeField] private int baseResolutionX = 1920;
    [SerializeField] private int baseResolutionY = 1080;
    
    [Range(1, 10)]
    [SerializeField] private int pixelSize = 4;
    
    private int currentPixelSize = -1;

    // Propiedades públicas para acceder desde otros scripts
    public int PixelSize => pixelSize;
    public int RenderWidth => baseResolutionX / pixelSize;
    public int RenderHeight => baseResolutionY / pixelSize;
    public Camera MainCamera => mainCamera;

    // Singleton para acceso fácil
    public static SettingsController Instance { get; private set; }

    void Awake()
    {
        // Singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        UpdateRenderTexture();
    }

    void Update()
    {
        if (currentPixelSize != pixelSize)
        {
            UpdateRenderTexture();
        }
    }

    void UpdateRenderTexture()
    {
        if (renderTexture == null || mainCamera == null) return;

        currentPixelSize = pixelSize;

        int newWidth = baseResolutionX / pixelSize;
        int newHeight = baseResolutionY / pixelSize;

        if (renderTexture.IsCreated())
        {
            renderTexture.Release();
        }

        renderTexture.width = newWidth;
        renderTexture.height = newHeight;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.Create();

        Debug.Log($"Render Texture actualizada: {newWidth}x{newHeight} (Pixel Size: {pixelSize})");
    }

    public void SetPixelSize(int size)
    {
        pixelSize = Mathf.Clamp(size, 1, 10);
    }

    // Método para convertir coordenadas de pantalla a RenderTexture
    public Vector2 ScreenToRenderTexturePosition(Vector2 screenPosition)
    {
        // Convertir de coordenadas de pantalla a normalizadas [0,1]
        Vector2 normalizedPos = new Vector2(
            screenPosition.x / Screen.width,
            screenPosition.y / Screen.height
        );

        // Convertir a coordenadas de RenderTexture
        return new Vector2(
            normalizedPos.x * RenderWidth,
            normalizedPos.y * RenderHeight
        );
    }

    // Método centralizado para obtener la posición del ratón en el mundo, usando la cámara y la RenderTexture
    public Vector3 GetMouseWorldPosition(Camera camera, Vector3 mouseScreenPosition)
    {
        if (camera == null)
            camera = mainCamera;

        // Convertir a coordenadas de RenderTexture si corresponde
        Vector2 rtMousePos = ScreenToRenderTexturePosition(mouseScreenPosition);
        Vector3 adjustedMousePos = new Vector3(rtMousePos.x, rtMousePos.y, mouseScreenPosition.z);

        // Usar ViewportPointToRay con proporción de la RenderTexture
        float viewportX = adjustedMousePos.x / RenderWidth;
        float viewportY = adjustedMousePos.y / RenderHeight;
        Ray ray = camera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }
}