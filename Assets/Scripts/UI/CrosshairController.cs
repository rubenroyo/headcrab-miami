using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Crosshair dinámico que muestra la dispersión actual del arma.
/// La dispersión real la calcula WeaponType.GetDispersion() — el crosshair
/// solo la recibe y la visualiza, para que siempre sea coherente con el disparo.
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
    [SerializeField] private float minGap = 5f;
    [SerializeField] private float gapMultiplier = 100f;
    [SerializeField] private Color crosshairColor = Color.white;

    [Header("Transición")]
    [SerializeField] private float transitionSpeed = 10f;

    // Dispersión actual (en grados) — la muestra el crosshair
    private float currentDispersion = 0f;
    private float targetDispersion = 0f;
    private bool isVisible = false;

    private Camera mainCamera;

    public static CrosshairController Instance { get; private set; }

    /// <summary>
    /// Dispersión actual en grados.
    /// Nota: este valor es SOLO visual. La dispersión real del disparo
    /// la calcula WeaponType.GetDispersion() en InventoryHolder.TryFire().
    /// </summary>
    public float CurrentDispersion => currentDispersion;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (crosshairContainer == null)
            CreateCrosshairUI();

        SetVisible(false);
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!isVisible) return;

        currentDispersion = Mathf.Lerp(currentDispersion, targetDispersion,
            Time.deltaTime * transitionSpeed);

        UpdateCrosshairSize();
    }

    /// <summary>
    /// Actualiza la dispersión visual del crosshair.
    /// Llamar desde PlayerController cada frame durante la posesión,
    /// pasando el resultado de weaponType.GetDispersion(state, handTremor).
    /// </summary>
    public void SetDispersion(float dispersionDegrees)
    {
        targetDispersion = dispersionDegrees;
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (crosshairContainer != null)
            crosshairContainer.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Añade dispersión temporal por recoil (visual únicamente).
    /// </summary>
    public void AddRecoilDispersion(float amount)
    {
        currentDispersion += amount;
    }

    private void UpdateCrosshairSize()
    {
        float gap = CalculateScreenRadius(currentDispersion);

        if (lineTop != null)
            lineTop.rectTransform.anchoredPosition    = new Vector2(0,    gap + lineLength / 2f);
        if (lineBottom != null)
            lineBottom.rectTransform.anchoredPosition = new Vector2(0,  -(gap + lineLength / 2f));
        if (lineLeft != null)
            lineLeft.rectTransform.anchoredPosition   = new Vector2(-(gap + lineLength / 2f), 0);
        if (lineRight != null)
            lineRight.rectTransform.anchoredPosition  = new Vector2( (gap + lineLength / 2f), 0);
    }

    private float CalculateScreenRadius(float dispersionDegrees)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return minGap;

        // Convertir dispersión angular a radio en píxeles
        float halfFOVRad      = mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float dispersionRad   = dispersionDegrees * Mathf.Deg2Rad;
        float screenHalfHeight = Screen.height * 0.5f * 0.5f;

        float radiusPixels = Mathf.Tan(dispersionRad) / Mathf.Tan(halfFOVRad) * screenHalfHeight;

        // minGap garantiza que nunca sea cero aunque la dispersión sea 0
        return Mathf.Max(minGap, radiusPixels);
    }

    private void CreateCrosshairUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CrosshairCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        GameObject container = new GameObject("Crosshair");
        container.transform.SetParent(canvas.transform, false);
        crosshairContainer = container.AddComponent<RectTransform>();
        crosshairContainer.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairContainer.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairContainer.anchoredPosition = Vector2.zero;
        crosshairContainer.sizeDelta = new Vector2(200, 200);

        lineTop    = CreateLine("LineTop",    true);
        lineBottom = CreateLine("LineBottom", true);
        lineLeft   = CreateLine("LineLeft",   false);
        lineRight  = CreateLine("LineRight",  false);

        SetColor(crosshairColor);
    }

    private Image CreateLine(string lineName, bool vertical)
    {
        GameObject lineObj = new GameObject(lineName);
        lineObj.transform.SetParent(crosshairContainer, false);

        RectTransform rect = lineObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = vertical
            ? new Vector2(lineWidth, lineLength)
            : new Vector2(lineLength, lineWidth);

        Image img = lineObj.AddComponent<Image>();
        img.color = crosshairColor;
        return img;
    }

    public void SetColor(Color color)
    {
        crosshairColor = color;
        if (lineTop != null)    lineTop.color    = color;
        if (lineBottom != null) lineBottom.color = color;
        if (lineLeft != null)   lineLeft.color   = color;
        if (lineRight != null)  lineRight.color  = color;
    }
}
