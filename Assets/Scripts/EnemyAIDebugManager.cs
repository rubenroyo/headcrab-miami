using UnityEngine;

/// <summary>
/// Manager global para debug de la IA de enemigos.
/// Permite activar/desactivar la visualización de los conos de visión.
/// </summary>
public class EnemyAIDebugManager : MonoBehaviour
{
    private static EnemyAIDebugManager instance;
    public static EnemyAIDebugManager Instance => instance;

    [Header("Tecla de Debug")]
    [SerializeField] private KeyCode toggleVisionKey = KeyCode.F1;
    
    [Header("Estado")]
    [SerializeField] private bool showAllVisionCones = true;

    public bool ShowAllVisionCones => showAllVisionCones;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleVisionKey))
        {
            ToggleAllVisionCones();
        }
    }

    /// <summary>
    /// Activa/desactiva la visualización de todos los conos de visión.
    /// </summary>
    public void ToggleAllVisionCones()
    {
        showAllVisionCones = !showAllVisionCones;
        
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.ShowVisionCone = showAllVisionCones;
        }

        Debug.Log($"Conos de visión: {(showAllVisionCones ? "ACTIVADOS" : "DESACTIVADOS")} (Tecla: {toggleVisionKey})");
    }

    /// <summary>
    /// Establece el estado de visibilidad de todos los conos.
    /// </summary>
    public void SetAllVisionCones(bool visible)
    {
        showAllVisionCones = visible;
        
        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.ShowVisionCone = visible;
        }
    }
}
