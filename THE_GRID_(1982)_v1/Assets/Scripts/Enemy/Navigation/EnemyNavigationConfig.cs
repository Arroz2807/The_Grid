using UnityEngine;

/// <summary>
/// Parámetros de percepción del tablero compartidos por todos los
/// comportamientos de IA. Es un ScriptableObject por el mismo motivo que
/// PowerUpAIConfig: son valores de balance que se ajustan jugando, no
/// constantes de diseño, y no deberían requerir recompilar.
///
/// Vive separado de PowerUpAIConfig a propósito: ese asset describe
/// "cuánto me interesan los power-ups", este describe "cuánto tablero
/// percibo". Mezclarlos obligaría a duplicar configuraciones por tipo de
/// enemigo cada vez que se toca un valor de navegación.
/// </summary>
[CreateAssetMenu(
    fileName = "EnemyNavigationConfig",
    menuName = "The Grid/Enemy/Navigation Config")]
public class EnemyNavigationConfig : ScriptableObject
{
    [Header("Percepción")]
    [Tooltip("Pasos hacia adelante que analiza la IA al evaluar una celda. " +
             "Es su horizonte: más allá de esto no ve, y por eso puede ser " +
             "engañada con pasillos largos o cierres tardíos.")]
    [Range(2, 16)]
    public int escapeHorizonSteps = 8;

    [Header("Random")]
    [Tooltip("Celdas alcanzables mínimas que debe ofrecer una dirección para " +
             "considerarse segura. Debe ser mayor que escapeHorizonSteps + 1: " +
             "un pasillo de ancho uno nunca supera esa cantidad, y con un " +
             "umbral menor el filtro volvería a dejarlo entrar.")]
    [Min(1)]
    public int minRoomCells = 12;

    [Tooltip("Probabilidad de ignorar el filtro de supervivencia y elegir al " +
             "azar entre todas las direcciones válidas. Mantiene a Random " +
             "impredecible y engañable.")]
    [Range(0f, 1f)]
    public float mistakeChance = 0.15f;

    [Header("Predict")]
    [Tooltip("Celdas de la trayectoria del jugador que Predict evalúa para " +
             "buscar un punto de intercepción.")]
    [Range(2, 16)]
    public int interceptHorizon = 8;

    [Tooltip("Ventaja mínima, en pasos del jugador, que Predict exige para " +
             "dar por buena una intercepción. 0 = acepta llegar a la par " +
             "(arriesgado: puede chocar con el rastro); 1 = exige llegar " +
             "estrictamente antes; valores mayores = más conservador.")]
    [Range(0, 3)]
    public int interceptTolerance = 1;

    [Header("Ambush")]
    [Tooltip("Radio, en celdas, alrededor del jugador donde Ambush busca " +
             "gargantas que cerrar. También acota cuánto puede alejarse de " +
             "su posición sin replantear.")]
    [Range(2, 12)]
    public int ambushRadius = 6;

    [Tooltip("Decisiones que Ambush mantiene la misma garganta antes de " +
             "replantear. Es lo que hace legible su patrón: sin esto " +
             "cambiaría de objetivo en cada celda.")]
    [Min(1)]
    public int ambushHoldSteps = 3;
}
