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

    // ── Colisión ──────────────────────────────────────────────────────────
    [Header("Colisión")]
    [Tooltip("Collider que bloquea el paso cuando la puerta está cerrada. "
           + "Ponlo en un hijo del DoorRoot (ej. BoxCollider que cubre el hueco de la puerta).")]
    [SerializeField] private Collider doorBlocker;

    [Tooltip("Layers de personajes que pueden quedar atrapados dentro del blockeador (Player, Enemy...).")]
    [SerializeField] private LayerMask characterLayers = ~0;

    [Tooltip("Margen extra (metros) al empujar un personaje fuera del collider.")]
    [SerializeField] private float pushOutMargin = 0.15f;

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

        // Sincronizar collider con el estado inicial
        if (doorBlocker != null)
            doorBlocker.enabled = !isOpen;
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

        // Al abrir: desactivar bloqueador inmediatamente para poder cruzar mientras anima
        if (isOpen && doorBlocker != null)
            doorBlocker.enabled = false;

        StartCoroutine(AnimationCooldown());
    }

    private IEnumerator AnimationCooldown()
    {
        isAnimating = true;
        yield return new WaitForSeconds(animationDuration);

        // Al cerrar: expulsar personajes solapados y reactivar el bloqueador
        if (!isOpen && doorBlocker != null)
        {
            PushOutCharacters();
            doorBlocker.enabled = true;
        }

        isAnimating = false;
    }

    /// <summary>
    /// Detecta personajes dentro del área del doorBlocker y los empuja al lado más cercano.
    /// Se llama justo antes de reactivar el collider al cerrar la puerta.
    /// </summary>
    private void PushOutCharacters()
    {
        if (doorBlocker == null) return;

        Bounds b = doorBlocker.bounds;
        // Usa transform.forward como eje de paso (la dirección en la que se cruza la puerta)
        Vector3 fwd = transform.forward;

        // Half-extent del bloqueador proyectada sobre el eje de paso
        float halfDepth = Mathf.Abs(fwd.x) * b.extents.x
                        + Mathf.Abs(fwd.y) * b.extents.y
                        + Mathf.Abs(fwd.z) * b.extents.z;

        Collider[] inside = Physics.OverlapBox(
            b.center, b.extents, doorBlocker.transform.rotation,
            characterLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider col in inside)
        {
            if (col == doorBlocker) continue;

            // Determinar en qué lado del eje de paso está el personaje
            Vector3 toChar = col.bounds.center - b.center;
            float side = Vector3.Dot(toChar, fwd) >= 0f ? 1f : -1f;

            // Posición segura: justo fuera del bloqueador en ese lado
            Vector3 safePos = b.center + fwd * (halfDepth + pushOutMargin) * side;
            safePos.y = col.transform.position.y;

            CharacterController cc = col.GetComponent<CharacterController>();
            if (cc != null)
                cc.Move(safePos - col.transform.position);
            else
                col.transform.position = safePos;

            Debug.Log($"[DoorController] Empujado '{col.name}' fuera de la puerta al cerrar.");
        }
    }

    // ── Gizmos ───────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
