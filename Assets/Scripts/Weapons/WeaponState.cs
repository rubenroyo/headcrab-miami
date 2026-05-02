/// <summary>
/// Estado de movimiento del portador del arma en el momento del disparo.
/// Usado por InventoryHolder para calcular dispersión.
/// Compartido entre PlayerController y EnemyAI.
/// </summary>
public enum WeaponState
{
    Idle,       // Quieto sin apuntar
    Moving,     // Andando sin apuntar
    Sprinting,  // Corriendo
    Aiming      // ADS (apuntado con mira)
}
