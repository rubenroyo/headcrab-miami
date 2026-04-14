using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crosshair dinámico que muestra la dispersión actual del arma.
/// Consiste en 4 líneas (arriba, abajo, izquierda, derecha) que se separan
/// según el valor de dispersión actual.
/// </summary>
public class CrosshairController : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private RectTransform crosshairContainer;
    [SerializeField] private Image lineTop;
    [SerializeField] private Image lineBottom;
    [SerializeField] private Image lineLeft;
    [SerializeField] private Image lineRight;
    
    [Header("Apariencia")]
    [SerializeField] private float lineWidth = 2f;
    [SerializeField] private float lineLength = 10f;
    [SerializeField] private float minGap = 5f;      // Gap mínimo entre líneas (dispersión 0)
    [SerializeField] private float gapMultiplier = 100f; // Multiplicador para convertir dispersión a píxeles
    [SerializeField] private Color crosshairColor = Color.white;
    
    [Header("Transición")]
    [SerializeField] private float transitionSpeed = 10f;
    
    [Header("Dispersión por Estado")]
    [Tooltip("Dispersión al apuntar quieto (grados)")]
    [SerializeField] private float dispersionAimIdle = 0.5f;
    [Tooltip("Dispersión al apuntar andando (grados)")]
    [SerializeField] private float dispersionAimWalking = 1.5f;
    [Tooltip("Dispersión sin apuntar quieto (grados)")]
    [SerializeField] private float dispersionHipIdle = 3f;
    [Tooltip("Dispersión sin apuntar andando (grados)")]
    [SerializeField] private float dispersionHipWalking = 5f;
    [Tooltip("Dispersión sin apuntar corriendo (grados)")]
    [SerializeField] private float dispersionHipSprinting = 10f;
    
    // Estado actual
    private float currentDispersion = 0f;
    private float targetDispersion = 0f;
    private bool isVisible = false;
    
    // Estado de movimiento (recibido de PlayerController)
    private bool isAiming = false;
    private bool isMoving = false;
    private bool isSprinting = false;
    
    public static CrosshairController Instance { get; private set; }
    
    // Propiedades públicas para leer/modificar dispersión
    public float DispersionAimIdle { get => dispersionAimIdle; set => dispersionAimIdle = value; }
    public float DispersionAimWalking { get => dispersionAimWalking; set => dispersionAimWalking = value; }
    public float DispersionHipIdle { get => dispersionHipIdle; set => dispersionHipIdle = value; }
    public float DispersionHipWalking { get => dispersionHipWalking; set => dispersionHipWalking = value; }
    public float DispersionHipSprinting { get => dispersionHipSprinting; set => dispersionHipSprinting = value; }
    
    /// <summary>
    /// Obtiene la dispersión actual en grados (para usar al disparar)
    /// </summary>
    public float CurrentDispersion => currentDispersion;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Crear líneas si no están asignadas
        if (crosshairContainer == null)
        {
            CreateCrosshairUI();
        }
        
        SetVisible(false);
    }
    
    void Update()
    {
        if (!isVisible) return;
        
        // Calcular dispersión objetivo según estado
        targetDispersion = CalculateDispersion();
        
        // Transición suave
        currentDispersion = Mathf.Lerp(currentDispersion, targetDispersion, Time.deltaTime * transitionSpeed);
        
        // Actualizar posición de las líneas
        UpdateCrosshairSize();
    }
    
    /// <summary>
    /// Calcula la dispersión según el estado actual
    /// </summary>
    private float CalculateDispersion()
    {
        if (isAiming)
        {
            // Apuntando (ADS)
            return isMoving ? dispersionAimWalking : dispersionAimIdle;
        }
        else
        {
            // Sin apuntar (hip fire)
            if (isSprinting)
                return dispersionHipSprinting;
            else if (isMoving)
                return dispersionHipWalking;
            else
                return dispersionHipIdle;
        }
    }
    
    /// <summary>
    /// Actualiza el estado de movimiento (llamar desde PlayerController)
    /// </summary>
    public void UpdateState(bool aiming, bool moving, bool sprinting)
    {
        isAiming = aiming;
        isMoving = moving;
        isSprinting = sprinting;
    }
    
    /// <summary>
    /// Muestra u oculta el crosshair
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (crosshairContainer != null)
        {
            crosshairContainer.gameObject.SetActive(visible);
        }
    }
    
    /// <summary>
    /// Añade dispersión temporal (por disparo, por ejemplo)
    /// </summary>
    public void AddRecoilDispersion(float amount)
    {
        currentDispersion += amount;
    }
    
    private void UpdateCrosshairSize()
    {
        // Convertir dispersión (grados) a píxeles de gap
        float gap = minGap + currentDispersion * gapMultiplier;
        
        // Posicionar las líneas
        if (lineTop != null)
        {
            lineTop.rectTransform.anchoredPosition = new Vector2(0, gap + lineLength / 2);
        }
        if (lineBottom != null)
        {
            lineBottom.rectTransform.anchoredPosition = new Vector2(0, -(gap + lineLength / 2));
        }
        if (lineLeft != null)
        {
            lineLeft.rectTransform.anchoredPosition = new Vector2(-(gap + lineLength / 2), 0);
        }
        if (lineRight != null)
        {
            lineRight.rectTransform.anchoredPosition = new Vector2(gap + lineLength / 2, 0);
        }
    }
    
    /// <summary>
    /// Crea el UI del crosshair programáticamente
    /// </summary>
    private void CreateCrosshairUI()
    {
        // Buscar o crear Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CrosshairCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Crear contenedor
        GameObject container = new GameObject("Crosshair");
        container.transform.SetParent(canvas.transform, false);
        crosshairContainer = container.AddComponent<RectTransform>();
        crosshairContainer.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairContainer.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairContainer.anchoredPosition = Vector2.zero;
        crosshairContainer.sizeDelta = new Vector2(200, 200);
        
        // Crear las 4 líneas
        lineTop = CreateLine("LineTop", true);
        lineBottom = CreateLine("LineBottom", true);
        lineLeft = CreateLine("LineLeft", false);
        lineRight = CreateLine("LineRight", false);
        
        // Aplicar color inicial
        SetColor(crosshairColor);
    }
    
    private Image CreateLine(string name, bool vertical)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(crosshairContainer, false);
        
        RectTransform rect = lineObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        
        // Tamaño de la línea
        if (vertical)
        {
            rect.sizeDelta = new Vector2(lineWidth, lineLength);
        }
        else
        {
            rect.sizeDelta = new Vector2(lineLength, lineWidth);
        }
        
        Image img = lineObj.AddComponent<Image>();
        img.color = crosshairColor;
        
        return img;
    }
    
    /// <summary>
    /// Cambia el color del crosshair
    /// </summary>
    public void SetColor(Color color)
    {
        crosshairColor = color;
        if (lineTop != null) lineTop.color = color;
        if (lineBottom != null) lineBottom.color = color;
        if (lineLeft != null) lineLeft.color = color;
        if (lineRight != null) lineRight.color = color;
    }
}
