using UnityEngine;

/// <summary>
/// Componente que va en el prefab del arma (equippedPrefab y fpsPrefab).
/// Contiene la referencia al punto de salida de la bala (MuzzlePoint).
///
/// SETUP EN EDITOR:
///   1. Abre el prefab del arma.
///   2. Crea un child GameObject llamado "MuzzlePoint" en la boca del cañón.
///   3. Añade este componente al root del prefab.
///   4. Arrastra el MuzzlePoint al campo muzzlePoint.
/// </summary>
public class WeaponVisual : MonoBehaviour
{
    [Tooltip("Transform en la boca del cañón. Las balas se instancian aquí.")]
    [SerializeField] private Transform muzzlePoint;

    /// <summary>
    /// Posición del cañón en world space.
    /// </summary>
    public Vector3 MuzzlePosition => muzzlePoint != null
        ? muzzlePoint.position
        : transform.position;

    /// <summary>
    /// Forward del cañón en world space (útil para enemigos libres que no apuntan con raycast).
    /// </summary>
    public Vector3 MuzzleForward => muzzlePoint != null
        ? muzzlePoint.forward
        : transform.forward;

    /// <summary>
    /// True si el MuzzlePoint está correctamente asignado.
    /// </summary>
    public bool IsValid => muzzlePoint != null;

    void OnDrawGizmosSelected()
    {
        if (muzzlePoint == null) return;
        Gizmos.color = UnityEngine.Color.yellow;
        Gizmos.DrawSphere(muzzlePoint.position, 0.03f);
        Gizmos.DrawLine(muzzlePoint.position, muzzlePoint.position + muzzlePoint.forward * 0.2f);
    }
}
