using UnityEngine;

/// <summary>
/// Maneja la visualización del arma en primera persona durante la posesión.
/// Se añade automáticamente al punto de ojos creado por CinemachineCameraController.
/// </summary>
public class FPSWeaponView : MonoBehaviour
{
    [Header("Referencias")]
    private InventoryHolder inventoryHolder;
    private GameObject fpsWeaponInstance;
    private Transform eyePoint;
    
    [Header("Animación (configurables)")]
    [SerializeField] private float bobFrequency = 10f;
    [SerializeField] private float bobAmplitude = 0.02f;
    [SerializeField] private float sprintBobMultiplier = 1.5f;
    
    [Header("Weapon Sway (lag al girar cámara)")]
    [Tooltip("Cuánto se desplaza el arma al girar la cámara")]
    [SerializeField] private float swayAmount = 90f;
    [Tooltip("Cuánto rota el arma al girar la cámara")]
    [SerializeField] private float swayRotationAmount = 10f;
    [Tooltip("Qué tan rápido vuelve el arma a su posición central")]
    [SerializeField] private float swaySmooth = 3f;
    [Tooltip("Desplazamiento máximo horizontal")]
    [SerializeField] private float maxSwayX = 0.3f;
    [Tooltip("Desplazamiento máximo vertical")]
    [SerializeField] private float maxSwayY = 0.3f;
    [Tooltip("Rotación máxima (grados)")]
    [SerializeField] private float maxSwayRotation = 8f;
    
    [Header("ADS (Apuntado)")]
    [Tooltip("Velocidad de transición al apuntar")]
    [SerializeField] private float adsTransitionSpeed = 10f;
    
    // Estado de movimiento
    private bool isActive = false;
    private bool isSprinting = false;
    private bool isMoving = false;
    private bool isADS = false;  // Estado de apuntado
    private float bobTimer = 0f;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 adsLocalPosition;  // Posición centrada para apuntar
    
    // Estado de sway
    private Vector2 currentSway = Vector2.zero;
    private float currentSwayRotationZ = 0f;
    
    // Estado de recoil
    private bool isRecoiling = false;
    private float recoilTimer = 0f;
    private float currentRecoilAngle = 0f;
    private float targetRecoilAngle = 0f;
    private float recoilDuration = 0.05f;
    private float recoilRecoveryDuration = 0.15f;
    private bool isRecovering = false;
    
    /// <summary>
    /// Activa la vista FPS del arma
    /// </summary>
    public void Activate(InventoryHolder inventory, Transform eye)
    {
        inventoryHolder = inventory;
        eyePoint = eye;
        
        if (inventoryHolder != null && inventoryHolder.HasWeapon)
        {
            CreateFPSWeapon();
        }
        
        // Suscribirse a cambios de arma
        if (inventoryHolder != null)
        {
            inventoryHolder.OnWeaponChanged += OnWeaponChanged;
        }
        
        isActive = true;
    }
    
    /// <summary>
    /// Desactiva la vista FPS del arma
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        
        // Desuscribirse
        if (inventoryHolder != null)
        {
            inventoryHolder.OnWeaponChanged -= OnWeaponChanged;
        }
        
        DestroyFPSWeapon();
        inventoryHolder = null;
        eyePoint = null;
        

    }
    
    /// <summary>
    /// Actualiza el estado de movimiento para animación de bob
    /// </summary>
    public void SetMovementState(bool moving, bool sprinting)
    {
        isMoving = moving;
        isSprinting = sprinting;
    }
    
    /// <summary>
    /// Actualiza el estado de apuntado ADS
    /// </summary>
    public void SetADSState(bool aiming)
    {
        isADS = aiming;
    }
    
    /// <summary>
    /// Inicia la animación de recoil (retroceso al disparar)
    /// </summary>
    public void PlayRecoil()
    {
        if (!isActive || fpsWeaponInstance == null)
        {
            Debug.LogWarning($"[FPSWeaponView] PlayRecoil ignorado: isActive={isActive}, fpsWeaponInstance={(fpsWeaponInstance != null)}");
            return;
        }
        
        isRecoiling = true;
        isRecovering = false;
        recoilTimer = 0f;
        currentRecoilAngle = 0f;
    }
    
    void Update()
    {
        if (!isActive || fpsWeaponInstance == null) return;
        
        UpdateWeaponSway();
        UpdateWeaponBob();
        UpdateRecoil();
    }
    
    private void CreateFPSWeapon()
    {
        if (inventoryHolder == null || !inventoryHolder.HasWeapon) return;
        
        WeaponType weaponType = inventoryHolder.EquippedWeapon.weaponType;
        
        // Usar fpsPrefab si existe, sino usar equippedPrefab como fallback
        GameObject prefab = weaponType.fpsPrefab != null ? weaponType.fpsPrefab : weaponType.equippedPrefab;
        
        if (prefab == null)
        {
            Debug.LogWarning($"[FPSWeaponView] No FPS prefab configured for {weaponType.weaponName}");
            return;
        }
        
        // Crear como hijo del eye point
        fpsWeaponInstance = Instantiate(prefab, eyePoint);
        fpsWeaponInstance.name = $"FPS_{weaponType.weaponName}";
        
        // Aplicar offsets del WeaponType
        if (weaponType.fpsPrefab != null)
        {
            fpsWeaponInstance.transform.localPosition = weaponType.fpsPositionOffset;
            fpsWeaponInstance.transform.localRotation = Quaternion.Euler(weaponType.fpsRotationOffset);
        }
        else
        {
            // Fallback: usar posición por defecto para equippedPrefab
            fpsWeaponInstance.transform.localPosition = new Vector3(0.25f, -0.15f, 0.35f);
            fpsWeaponInstance.transform.localRotation = Quaternion.identity;
        }
        
        baseLocalPosition = fpsWeaponInstance.transform.localPosition;
        baseLocalRotation = fpsWeaponInstance.transform.localRotation;
        
        // Calcular posición ADS (centrada, X=0)
        adsLocalPosition = new Vector3(0f, baseLocalPosition.y, baseLocalPosition.z);
        
        // Guardar parámetros de recoil del WeaponType
        targetRecoilAngle = weaponType.recoilAngle;
        recoilDuration = weaponType.recoilDuration;
        recoilRecoveryDuration = weaponType.recoilRecoveryDuration;
        
        Debug.Log($"[FPSWeaponView] Created FPS weapon: {weaponType.weaponName}");
    }
    
    private void DestroyFPSWeapon()
    {
        if (fpsWeaponInstance != null)
        {
            Destroy(fpsWeaponInstance);
            fpsWeaponInstance = null;
        }
    }
    
    private void OnWeaponChanged(WeaponData newWeapon)
    {
        // Recrear el arma FPS cuando cambia el arma equipada
        DestroyFPSWeapon();
        
        if (newWeapon != null && newWeapon.weaponType != null)
        {
            CreateFPSWeapon();
        }
    }
    
    private void UpdateWeaponSway()
    {
        // Capturar input del ratón
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        
        // Calcular sway objetivo (invertido para efecto de lag)
        Vector2 targetSway = new Vector2(
            Mathf.Clamp(-mouseX * swayAmount, -maxSwayX, maxSwayX),
            Mathf.Clamp(-mouseY * swayAmount, -maxSwayY, maxSwayY)
        );
        
        // Rotación Z basada en movimiento horizontal (como si el arma tuviera inercia)
        float targetSwayRotZ = Mathf.Clamp(-mouseX * swayRotationAmount, -maxSwayRotation, maxSwayRotation);
        
        // Suavizar hacia el objetivo
        currentSway = Vector2.Lerp(currentSway, targetSway, Time.deltaTime * swaySmooth);
        currentSwayRotationZ = Mathf.Lerp(currentSwayRotationZ, targetSwayRotZ, Time.deltaTime * swaySmooth);
    }
    
    private void UpdateWeaponBob()
    {
        Vector3 bobOffset = Vector3.zero;
        
        // Reducir bob cuando está apuntando
        float bobMultiplier = isADS ? 0.3f : 1f;
        
        if (isMoving && !isADS)  // Sin bob mientras apuntas
        {
            // Calcular bob
            bobTimer += Time.deltaTime * bobFrequency * (isSprinting ? sprintBobMultiplier : 1f);
            
            float bobX = Mathf.Sin(bobTimer) * bobAmplitude * (isSprinting ? sprintBobMultiplier : 1f) * bobMultiplier;
            float bobY = Mathf.Abs(Mathf.Sin(bobTimer)) * bobAmplitude * 0.5f * (isSprinting ? sprintBobMultiplier : 1f) * bobMultiplier;
            
            bobOffset = new Vector3(bobX, bobY, 0f);
        }
        
        // Determinar posición base según ADS
        Vector3 currentBasePos = isADS ? adsLocalPosition : baseLocalPosition;
        
        // Reducir sway cuando está apuntando
        float swayMultiplier = isADS ? 0.2f : 1f;
        Vector3 swayOffset = new Vector3(currentSway.x * swayMultiplier, currentSway.y * swayMultiplier, 0f);
        
        // Combinar posición base + bob + sway
        Vector3 targetPos = currentBasePos + bobOffset + swayOffset;
        
        // Interpolar suavemente hacia la posición objetivo (más suave para ADS)
        float lerpSpeed = isADS ? adsTransitionSpeed : 15f;
        fpsWeaponInstance.transform.localPosition = Vector3.Lerp(
            fpsWeaponInstance.transform.localPosition,
            targetPos,
            Time.deltaTime * lerpSpeed
        );
        
        // Aplicar rotación de sway (combinada con recoil después en UpdateRecoil)
        // Solo aplicamos sway Z aquí si no hay recoil activo
        if (!isRecoiling && !isRecovering)
        {
            float rotMultiplier = isADS ? 0.2f : 1f;
            Quaternion swayRotation = Quaternion.Euler(0f, 0f, currentSwayRotationZ * rotMultiplier);
            fpsWeaponInstance.transform.localRotation = Quaternion.Slerp(
                fpsWeaponInstance.transform.localRotation,
                baseLocalRotation * swayRotation,
                Time.deltaTime * swaySmooth
            );
        }
    }
    
    private void UpdateRecoil()
    {
        if (!isRecoiling && !isRecovering) return;
        
        recoilTimer += Time.deltaTime;
        
        if (isRecoiling)
        {
            // Fase de recoil: rotar hacia arriba rápidamente
            float t = Mathf.Clamp01(recoilTimer / recoilDuration);
            currentRecoilAngle = Mathf.Lerp(0f, targetRecoilAngle, t);
            
            if (t >= 1f)
            {
                // Transición a fase de recuperación
                isRecoiling = false;
                isRecovering = true;
                recoilTimer = 0f;
            }
        }
        else if (isRecovering)
        {
            // Fase de recuperación: volver a posición original
            float t = Mathf.Clamp01(recoilTimer / recoilRecoveryDuration);
            currentRecoilAngle = Mathf.Lerp(targetRecoilAngle, 0f, t);
            
            if (t >= 1f)
            {
                isRecovering = false;
                currentRecoilAngle = 0f;
            }
        }
        
        // Aplicar rotación de recoil en eje X negativo (el arma sube/kick hacia arriba)
        // Combinado con rotación de sway en Z
        Quaternion recoilRotation = Quaternion.Euler(-currentRecoilAngle, 0f, currentSwayRotationZ);
        fpsWeaponInstance.transform.localRotation = baseLocalRotation * recoilRotation;
    }
    
    void OnDestroy()
    {
        // Limpiar suscripciones
        if (inventoryHolder != null)
        {
            inventoryHolder.OnWeaponChanged -= OnWeaponChanged;
        }
    }
}
