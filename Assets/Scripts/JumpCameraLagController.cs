using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;

/// <summary>
/// Controlador de lag dinámico para la cámara de salto.
/// Efecto: La cámara se queda atrás brevemente y luego acelera para alcanzar al jugador.
/// Esto crea un efecto dramático de "retroceso y persecución".
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class JumpCameraLagController : MonoBehaviour
{
    [Header("Lag Effect Settings")]
    [Tooltip("Damping máximo inicial (cámara se queda atrás)")]
    [SerializeField] private float initialDamping = 3f;
    
    [Tooltip("Damping mínimo cuando alcanza al jugador")]
    [SerializeField] private float targetDamping = 0.3f;
    
    [Tooltip("Duración del lag inicial antes de acelerar")]
    [SerializeField] private float lagDuration = 0.2f;
    
    [Tooltip("Duración de la aceleración (de lag a catch-up)")]
    [SerializeField] private float catchUpDuration = 0.4f;
    
    [Header("Vertical Offset")]
    [Tooltip("Offset vertical adicional durante el salto")]
    [SerializeField] private float jumpVerticalBoost = 1f;
    
    [Tooltip("Curva de animación para el boost vertical")]
    [SerializeField] private AnimationCurve boostCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private CinemachineCamera vcam;
    private CinemachineOrbitalFollow orbitalFollow;
    private CinemachinePositionComposer positionComposer;
    
    // Estado
    private bool isActive = false;
    private float activationTime;
    private Vector3 originalTargetOffset;
    private Vector3 originalPositionDamping;
    
    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        orbitalFollow = vcam?.GetComponent<CinemachineOrbitalFollow>();
        positionComposer = vcam?.GetComponent<CinemachinePositionComposer>();
        
        if (orbitalFollow != null)
        {
            // Guardar valores originales de TrackerSettings
            originalPositionDamping = orbitalFollow.TrackerSettings.PositionDamping;
        }
        
        if (positionComposer != null)
        {
            originalTargetOffset = positionComposer.TargetOffset;
        }
    }
    
    void OnEnable()
    {
        // Cuando la cámara se activa, iniciar el efecto de lag
        StartLagEffect();
    }
    
    void OnDisable()
    {
        // Restaurar valores originales
        ResetToOriginal();
        isActive = false;
    }
    
    void Update()
    {
        if (!isActive) return;
        
        float elapsed = Time.unscaledTime - activationTime;
        
        UpdateDamping(elapsed);
        UpdateVerticalBoost(elapsed);
        
        if (showDebugInfo && orbitalFollow != null)
        {
            Debug.Log($"[JumpCameraLag] elapsed={elapsed:F2}, damping={orbitalFollow.TrackerSettings.PositionDamping.z:F2}");
        }
    }
    
    private void StartLagEffect()
    {
        isActive = true;
        activationTime = Time.unscaledTime;
        
        // Establecer damping inicial alto (cámara se queda atrás)
        SetPositionDamping(new Vector3(initialDamping, initialDamping * 0.5f, initialDamping));
        
        if (showDebugInfo)
        {
            Debug.Log("[JumpCameraLag] StartLagEffect - high damping activated");
        }
    }
    
    private void UpdateDamping(float elapsed)
    {
        if (orbitalFollow == null) return;
        
        float damping;
        
        if (elapsed < lagDuration)
        {
            // Fase 1: Mantener damping alto (cámara se queda atrás)
            damping = initialDamping;
        }
        else if (elapsed < lagDuration + catchUpDuration)
        {
            // Fase 2: Reducir damping gradualmente (cámara acelera para alcanzar)
            float catchUpProgress = (elapsed - lagDuration) / catchUpDuration;
            
            // Ease-out para que la aceleración sea más natural
            float smoothT = 1f - (1f - catchUpProgress) * (1f - catchUpProgress);
            
            damping = Mathf.Lerp(initialDamping, targetDamping, smoothT);
        }
        else
        {
            // Fase 3: Damping bajo (seguimiento cercano)
            damping = targetDamping;
        }
        
        SetPositionDamping(new Vector3(damping, damping * 0.5f, damping));
    }
    
    private void SetPositionDamping(Vector3 damping)
    {
        if (orbitalFollow == null) return;
        
        // TrackerSettings es un struct, hay que reasignarlo completo
        var settings = orbitalFollow.TrackerSettings;
        settings.PositionDamping = damping;
        orbitalFollow.TrackerSettings = settings;
    }
    
    private void UpdateVerticalBoost(float elapsed)
    {
        if (positionComposer == null || jumpVerticalBoost <= 0f) return;
        
        // El boost vertical sigue una curva durante todo el salto
        float totalDuration = lagDuration + catchUpDuration + 0.5f; // Un poco más para el boost
        float progress = Mathf.Clamp01(elapsed / totalDuration);
        
        float boostAmount = boostCurve.Evaluate(progress) * jumpVerticalBoost;
        positionComposer.TargetOffset = originalTargetOffset + Vector3.up * boostAmount;
    }
    
    private void ResetToOriginal()
    {
        SetPositionDamping(originalPositionDamping);
        
        if (positionComposer != null)
        {
            positionComposer.TargetOffset = originalTargetOffset;
        }
    }
    
    /// <summary>
    /// Actualiza el progreso del salto (llamado externamente por CinemachineCameraController)
    /// </summary>
    public void UpdateJumpProgress(float progress)
    {
        // El progreso del salto puede usarse para ajustes adicionales
        // Por ahora, el efecto se basa en tiempo desde activación
        
        if (positionComposer != null && jumpVerticalBoost > 0f)
        {
            // Boost vertical basado en progreso de salto (arco parabólico)
            // Sin(PI * progress) da un arco natural: 0 -> 1 -> 0
            float boostAmount = Mathf.Sin(progress * Mathf.PI) * jumpVerticalBoost;
            positionComposer.TargetOffset = originalTargetOffset + Vector3.up * boostAmount;
        }
    }
    
    #region Inspector Helpers
    
    [ContextMenu("Test Lag Effect")]
    private void TestLagEffect()
    {
        StartLagEffect();
    }
    
    [ContextMenu("Reset to Original")]
    private void EditorReset()
    {
        ResetToOriginal();
        isActive = false;
    }
    
    #endregion
}
