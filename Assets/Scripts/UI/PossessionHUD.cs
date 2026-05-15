using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUD que se muestra mientras el jugador posee un enemigo.
/// Muestra: barra de vida, icono del arma, contador de munición (magazine / reserva).
///
/// Colócalo en el mismo Canvas que CrosshairController.
/// Asigna las referencias desde el Inspector.
///
/// Se suscribe a los eventos OnHealthChanged y OnAmmoChanged de InventoryHolder
/// para actualizar la UI solo cuando cambia algo (sin polling en Update).
/// </summary>
public class PossessionHUD : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image  healthFill;
    [SerializeField] private Color  healthColorFull    = Color.green;
    [SerializeField] private Color  healthColorLow     = Color.red;
    [SerializeField] private float  lowHealthThreshold = 0.3f;

    [Header("Arma")]
    [SerializeField] private Image weaponIcon;

    [Header("Munición")]
    [Tooltip("Texto en formato 'X / Y'  (magazine / reserva)")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Color    ammoNormalColor  = Color.white;
    [SerializeField] private Color    ammoEmptyColor   = Color.red;

    [Header("Recarga")]
    [Tooltip("Objeto/texto que se muestra mientras se recarga.")]
    [SerializeField] private GameObject reloadingIndicator;

    // ─────────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────────

    private InventoryHolder trackedInventory;

    public static PossessionHUD Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        SetVisible(false);
    }

    void Update()
    {
        // Actualizar el indicador de recarga cada frame (es barato)
        if (trackedInventory != null && reloadingIndicator != null)
            reloadingIndicator.SetActive(trackedInventory.IsReloading);
    }

    // ─────────────────────────────────────────────
    //  API PÚBLICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Activa el HUD y se engancha a los eventos del inventario poseído.
    /// Llamar desde PlayerController.PossessEnemy().
    /// </summary>
    public void Attach(InventoryHolder inventory)
    {
        Detach();

        trackedInventory = inventory;
        trackedInventory.OnHealthChanged += HandleHealthChanged;
        trackedInventory.OnAmmoChanged   += HandleAmmoChanged;
        trackedInventory.OnWeaponChanged += HandleWeaponChanged;

        // Volcar estado inicial
        HandleHealthChanged(inventory.CurrentHealth, inventory.MaxHealth);
        HandleWeaponChanged(inventory.EquippedWeapon);

        if (inventory.HasWeapon)
            HandleAmmoChanged(inventory.EquippedWeapon.bulletsInMagazine,
                              inventory.EquippedWeapon.reserveBullets);

        SetVisible(true);
    }

    /// <summary>
    /// Desactiva el HUD y desuscribe eventos.
    /// Llamar desde PlayerController.ReleaseEnemy().
    /// </summary>
    public void Detach()
    {
        if (trackedInventory != null)
        {
            trackedInventory.OnHealthChanged -= HandleHealthChanged;
            trackedInventory.OnAmmoChanged   -= HandleAmmoChanged;
            trackedInventory.OnWeaponChanged -= HandleWeaponChanged;
            trackedInventory = null;
        }

        SetVisible(false);
    }

    // ─────────────────────────────────────────────
    //  HANDLERS DE EVENTO
    // ─────────────────────────────────────────────

    private void HandleHealthChanged(float current, float max)
    {
        if (healthBar != null)
        {
            healthBar.value = max > 0f ? current / max : 0f;
            if (healthFill != null)
                healthFill.color = Color.Lerp(healthColorLow, healthColorFull, healthBar.value / lowHealthThreshold > 1f
                    ? 1f
                    : healthBar.value / lowHealthThreshold);
        }
    }

    private void HandleAmmoChanged(int magazine, int reserve)
    {
        if (ammoText == null) return;

        int magSize = trackedInventory != null && trackedInventory.HasWeapon
            ? trackedInventory.EquippedWeapon.weaponType.magazineSize
            : 0;

        ammoText.text  = $"{magazine} / {reserve}";
        ammoText.color = magazine == 0 ? ammoEmptyColor : ammoNormalColor;
    }

    private void HandleWeaponChanged(WeaponData weapon)
    {
        if (weaponIcon != null)
        {
            if (weapon != null && weapon.weaponType != null && weapon.weaponType.weaponIcon != null)
            {
                weaponIcon.sprite  = weapon.weaponType.weaponIcon;
                weaponIcon.enabled = true;
            }
            else
            {
                weaponIcon.enabled = false;
            }
        }

        if (ammoText != null)
        {
            ammoText.gameObject.SetActive(weapon != null && weapon.weaponType != null
                                          && !weapon.weaponType.isMelee);
        }

        if (weapon != null && weapon.weaponType != null && !weapon.weaponType.isMelee)
            HandleAmmoChanged(weapon.bulletsInMagazine, weapon.reserveBullets);
        else if (ammoText != null)
            ammoText.text = string.Empty;
    }

    // ─────────────────────────────────────────────
    //  VISIBILIDAD
    // ─────────────────────────────────────────────

    private void SetVisible(bool visible)
    {
        if (healthBar  != null) healthBar.gameObject.SetActive(visible);
        if (weaponIcon != null) weaponIcon.gameObject.SetActive(visible);
        if (ammoText   != null) ammoText.gameObject.SetActive(visible);
        if (reloadingIndicator != null) reloadingIndicator.SetActive(false);
    }
}
