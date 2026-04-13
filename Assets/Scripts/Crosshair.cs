using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crosshair simple en el centro de la pantalla.
/// Añadir a un Canvas con Render Mode: Screen Space - Overlay.
/// </summary>
public class Crosshair : MonoBehaviour
{
    [Header("Apariencia")]
    [SerializeField] private float size = 4f;
    [SerializeField] private Color color = Color.white;
    
    [Header("Comportamiento")]
    [Tooltip("Ocultar crosshair durante el salto")]
    [SerializeField] private bool hideWhileJumping = true;
    
    private Image crosshairImage;
    private PlayerController playerController;
    
    void Start()
    {
        CreateCrosshair();
        playerController = FindFirstObjectByType<PlayerController>();
    }
    
    void CreateCrosshair()
    {
        // Crear GameObject para el crosshair
        GameObject crosshairObj = new GameObject("CrosshairDot");
        crosshairObj.transform.SetParent(transform, false);
        
        // Añadir y configurar Image
        crosshairImage = crosshairObj.AddComponent<Image>();
        crosshairImage.color = color;
        
        // Configurar RectTransform - centrado
        RectTransform rt = crosshairObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(size, size);
    }
    
    void Update()
    {
        if (crosshairImage == null) return;
        
        // Ocultar durante el salto si está configurado
        if (hideWhileJumping && playerController != null)
        {
            bool isJumping = playerController.CurrentState == PlayerState.Jumping;
            crosshairImage.enabled = !isJumping;
        }
    }
    
    /// <summary>
    /// Cambia el color del crosshair en tiempo de ejecución
    /// </summary>
    public void SetColor(Color newColor)
    {
        color = newColor;
        if (crosshairImage != null)
            crosshairImage.color = color;
    }
    
    /// <summary>
    /// Cambia el tamaño del crosshair en tiempo de ejecución
    /// </summary>
    public void SetSize(float newSize)
    {
        size = newSize;
        if (crosshairImage != null)
        {
            RectTransform rt = crosshairImage.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
        }
    }
    
    /// <summary>
    /// Muestra u oculta el crosshair
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (crosshairImage != null)
            crosshairImage.enabled = visible;
    }
}
