using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador de puerta interactiva.
/// Registra la puerta globalmente para que PlayerController.TryInteract()
/// pueda encontrarla sin hacer FindObjectsByType cada frame.
///
/// Estructura de prefab esperada:
///   DoorRoot  ← DoorController va aquí
///     └── DoorModel  ← Animator con parámetro Bool "IsOpen" va aquí
/// </summary>
public class DoorController : MonoBehaviour
{
    // ── Registro estático ────────────────────────────────────────────────
    private static readonly List<DoorController> s_All = new();
    public  static IReadOnlyList<DoorController> All => s_All;

    // ── Animación ────────────────────────────────────────────────────────
    [Header("Animación")]
    [Tooltip("Animator de la puerta. Si se deja vacío se busca automáticamente en los hijos.")]
    [SerializeField] private Animator doorAnimator;

    [Tooltip("Nombre del parámetro Bool del Animator que controla la apertura.")]
    [SerializeField] private string openParameterName = "IsOpen";

    [Tooltip("Duración de la animación (segundos). Bloquea nuevas interacciones hasta que termine.")]
    [SerializeField] private float animationDuration = 1f;

    // ── Interacción ──────────────────────────────────────────────────────
    [Header("Interacción")]
    [Tooltip("Radio de interacción en metros.")]
    [SerializeField] private float interactRadius = 2f;

    // ── Estado ───────────────────────────────────────────────────────────
    private bool isOpen      = false;
    private bool isAnimating = false;

    // ── Propiedades ──────────────────────────────────────────────────────
    public bool  IsOpen        => isOpen;
    public bool  CanInteract   => !isAnimating;
    public float InteractRadius => interactRadius;

    // ── Ciclo de vida ────────────────────────────────────────────────────
    void Awake()
    {
        if (doorAnimator == null)
            doorAnimator = GetComponentInChildren<Animator>();
    }

    void OnEnable()  => s_All.Add(this);
    void OnDisable() => s_All.Remove(this);

    // ── API pública ──────────────────────────────────────────────────────
    /// <summary>
    /// Abre o cierra la puerta. Bloquea interacciones adicionales
    /// durante <see cref="animationDuration"/> segundos.
    /// </summary>
    public void Interact()
    {
        if (isAnimating) return;

        isOpen = !isOpen;

        if (doorAnimator != null)
            doorAnimator.SetBool(openParameterName, isOpen);

        StartCoroutine(AnimationCooldown());
    }

    private IEnumerator AnimationCooldown()
    {
        isAnimating = true;
        yield return new WaitForSeconds(animationDuration);
        isAnimating = false;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
