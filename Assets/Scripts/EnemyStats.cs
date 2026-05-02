using UnityEngine;

/// <summary>
/// Datos por tipo de enemigo: velocidades, precisión y características físicas.
/// ScriptableObject — crear uno por tipo (Goblin, Soldier, Boss…).
/// Menu: Headcrab / Enemy Stats
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Headcrab/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Velocidades de Movimiento")]
    [Tooltip("Velocidad al andar (m/s)")]
    public float walkSpeed = 3f;

    [Tooltip("Velocidad al correr (m/s)")]
    public float runSpeed = 6f;
    
    [Header("Multiplicadores de velocidad (posesión)")]
    [Tooltip("Multiplicador de velocidad al correr poseído (Shift)")]
    public float sprintMultiplier = 1.6f;

    [Tooltip("Multiplicador de velocidad al apuntar poseído (ADS)")]
    public float aimSpeedMultiplier = 0.5f;

    [Tooltip("Velocidad de rotación al girar hacia un objetivo (grados/segundo)")]
    public float rotationSpeed = 180f;

    [Tooltip("Temblor de pulso base del enemigo. 0 = puntería perfecta, ~2 = soldado nervioso.")]
    [Range(0f, 10f)]
    public float handTremor = 0.5f;
}
