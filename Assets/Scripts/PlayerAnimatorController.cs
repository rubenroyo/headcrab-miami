using UnityEngine;

/// <summary>
/// Controla el Animator del player y los efectos de partículas
/// en función de su estado de movimiento.
/// Coloca este componente en el mismo GameObject que PlayerController.
/// </summary>
public class PlayerAnimatorController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Animator del hijo que contiene el modelo. Si es null, se busca automáticamente.")]
    [SerializeField] private Animator animator;

    [Header("Umbrales")]
    [Tooltip("Velocidad horizontal mínima para considerar que el player está corriendo.")]
    [SerializeField] private float runThreshold = 0.1f;

    [Header("Velocidad de animación — Run")]
    [Tooltip("Velocidad del Animator cuando el personaje se mueve a velocidad máxima.")]
    [SerializeField] private float maxAnimationSpeed = 1f;

    [Tooltip("Velocidad inicial del Animator al empezar a moverse (efecto Mario: piernas rápidas al arrancar).")]
    [SerializeField] private float initialAnimationSpeed = 2.5f;

    [Tooltip("Velocidad a la que la animación se ajusta hacia la velocidad proporcional al movimiento.")]
    [SerializeField] private float animSpeedSettleRate = 3f;

    [Header("Salto")]
    [Tooltip("Tiempo mínimo en el aire antes de que isGrounded pueda volver a true. Evita falsos positivos al despegar.")]
    [SerializeField] private float minAirTime = 0.15f;

    [Header("Air Roll")]
    [Tooltip("Duración del clip de AirRoll en segundos. Léela en el editor desde el clip de animación. " +
             "La velocidad del Animator se calculará para que la voltereta ocupe exactamente la mitad del AirRollDuration.")]
    [SerializeField] private float airRollClipDuration = 0.5f;

    [Header("Partículas — Run")]
    [SerializeField] private ParticleSystem runSmokePS;
    [SerializeField] private float smokeMinSpeed     = 1f;
    [SerializeField] private float smokeMaxSpeed     = 8f;
    [SerializeField] private float smokeMaxEmissionRate = 20f;
    [SerializeField] private float smokeMinSize      = 0.2f;
    [SerializeField] private float smokeMaxSize      = 0.8f;

    [Header("Partículas — Jump")]
    [SerializeField] private ParticleSystem jumpSmokePS;

    [Header("Partículas — Long Jump")]
    [SerializeField] private ParticleSystem longJumpSmokePS;

    [Header("Partículas — Fall")]
    [SerializeField] private ParticleSystem fallSmokePS;

    [Header("Partículas — Ground Pound")]
    [SerializeField] private ParticleSystem groundPoundSmokePS;

    [Header("Partículas — Dive Hit")]
    [SerializeField] private ParticleSystem diveHitPS;

    // ─────────────────────────────────────────────
    //  PRIVADOS
    // ─────────────────────────────────────────────

    private PlayerController playerController;
    private float currentAnimSpeed = 1f;
    private bool wasRunning = false;
    private float airTimer  = 0f;

    private ParticleSystem.EmissionModule smokeEmission;
    private ParticleSystem.MainModule     smokeMain;
    private bool smokeModulesCached = false;

    // ─────────────────────────────────────────────
    //  INIT
    // ─────────────────────────────────────────────

    void Awake()
    {
        playerController = GetComponent<PlayerController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (playerController == null)
            Debug.LogWarning("[PlayerAnimatorController] No se encontró PlayerController en el mismo GameObject.");
        if (animator == null)
            Debug.LogWarning("[PlayerAnimatorController] No se encontró Animator en los hijos del Player.");

        CacheSmokeModules();
    }

    void CacheSmokeModules()
    {
        if (runSmokePS == null) return;
        smokeEmission      = runSmokePS.emission;
        smokeMain          = runSmokePS.main;
        smokeModulesCached = true;
        runSmokePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // ─────────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────────

    void Update()
    {
        if (animator == null || playerController == null) return;

        UpdateGrounded();
        UpdateCrouch();
        UpdateRun();
    }

    // ─────────────────────────────────────────────
    //  GROUNDED
    // ─────────────────────────────────────────────

    void UpdateGrounded()
    {
        bool physicsGrounded = playerController.IsGrounded;

        if (!physicsGrounded)
            airTimer += Time.deltaTime;
        else
            airTimer = 0f;

        bool animatorGrounded = physicsGrounded && airTimer == 0f;
        animator.SetBool("isGrounded", animatorGrounded);
    }

    // ─────────────────────────────────────────────
    //  CROUCH
    // ─────────────────────────────────────────────

    void UpdateCrouch()
    {
        bool isCrouching = playerController.IsCrouching;
        animator.SetBool("isCrouching", isCrouching);

        if (isCrouching)
        {
            animator.SetBool("isRunning", false);
            animator.speed = 1f;
            wasRunning     = false;
        }
    }

    // ─────────────────────────────────────────────
    //  RUN
    // ─────────────────────────────────────────────

    void UpdateRun()
    {
        if (playerController.IsCrouching)
        {
            SetSmokeActive(false);
            return;
        }

        float horizontalSpeed = playerController.CurrentHorizontalSpeed;
        bool grounded         = playerController.IsGrounded;
        bool isRunning        = horizontalSpeed > runThreshold;

        animator.SetBool("isRunning", isRunning);

        float targetAnimSpeed;
        if (isRunning)
        {
            float maxSpeed   = playerController.MoveSpeed;
            float speedRatio = maxSpeed > 0f ? Mathf.Clamp01(horizontalSpeed / maxSpeed) : 0f;
            targetAnimSpeed  = speedRatio * maxAnimationSpeed;

            if (!wasRunning)
                currentAnimSpeed = initialAnimationSpeed;
        }
        else
        {
            targetAnimSpeed = 1f;
        }

        currentAnimSpeed = Mathf.MoveTowards(currentAnimSpeed, targetAnimSpeed, animSpeedSettleRate * Time.deltaTime);
        animator.speed   = currentAnimSpeed;

        bool smokeActive = isRunning && grounded;
        SetSmokeActive(smokeActive);
        if (smokeActive && smokeModulesCached)
            UpdateSmokeIntensity(horizontalSpeed);

        wasRunning = isRunning;
    }

    // ─────────────────────────────────────────────
    //  HUMO RUN — helpers
    // ─────────────────────────────────────────────

    void SetSmokeActive(bool active)
    {
        if (!smokeModulesCached) return;

        if (active && !runSmokePS.isPlaying)
            runSmokePS.Play();
        else if (!active && runSmokePS.isPlaying)
            runSmokePS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void UpdateSmokeIntensity(float speed)
    {
        float t = Mathf.InverseLerp(smokeMinSpeed, smokeMaxSpeed, speed);
        smokeEmission.rateOverTime = Mathf.Lerp(0f, smokeMaxEmissionRate, t);
        smokeMain.startSize        = Mathf.Lerp(smokeMinSize, smokeMaxSize, t);
    }

    // ─────────────────────────────────────────────
    //  BURST — helper genérico
    // ─────────────────────────────────────────────

    void PlayBurst(ParticleSystem ps)
    {
        if (ps == null) return;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.Play();
    }

    // ─────────────────────────────────────────────
    //  TRIGGERS — llamados desde PlayerController
    // ─────────────────────────────────────────────

    public void TriggerJump()
    {
        airTimer = 0f;
        animator.speed = 1f;
        animator.SetTrigger("Jump");
        PlayBurst(jumpSmokePS);
    }

    public void TriggerLongJump()
    {
        airTimer = 0f;
        animator.speed = 1f;
        animator.SetTrigger("LongJump");
        PlayBurst(longJumpSmokePS);
    }

    public void TriggerAirRoll()
    {
        airTimer = 0f;

        float targetDuration = playerController.AirRollDuration * 0.5f;
        animator.speed = (targetDuration > 0f && airRollClipDuration > 0f)
            ? airRollClipDuration / targetDuration
            : 1f;

        animator.SetTrigger("AirRoll");
    }

    public void TriggerDive()
    {
        animator.speed = 1f;
        animator.SetTrigger("Dive");
        PlayBurst(diveHitPS);
    }

    /// <summary>
    /// Llamar desde PlayerController.LandOnGround() para el humo de aterrizaje normal.
    /// </summary>
    public void TriggerFallSmoke()
    {
        PlayBurst(fallSmokePS);
    }

    /// <summary>
    /// Llamar desde PlayerController.EnterGroundPoundLand() para el humo de ground pound.
    /// </summary>
    public void TriggerGroundPoundSmoke()
    {
        PlayBurst(groundPoundSmokePS);
    }
}
