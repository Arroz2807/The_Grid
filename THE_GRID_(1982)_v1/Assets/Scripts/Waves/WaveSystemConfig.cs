using UnityEngine;

/// <summary>
/// Parámetros globales del sistema de oleadas: intervalos, power-ups,
/// score y pantalla de transición. Se separa de WaveProgressionConfig
/// porque estos valores son más estables durante el balance — la
/// progresión de enemigos cambia con frecuencia, estos no tanto.
/// </summary>
[CreateAssetMenu(fileName = "WaveSystemConfig",
                 menuName = "The Grid/Waves/Wave System Config")]
public class WaveSystemConfig : ScriptableObject
{
    [Header("Oleadas")]
    [Tooltip("Número de la primera oleada. Normalmente 1.")]
    [Min(1)]
    public int startingWave = 1;

    [Tooltip("Límite de oleadas. 0 = sin límite (el juego termina cuando muere el jugador).")]
    [Min(0)]
    public int maximumWaves = 0;

    // [Tooltip("Segundos de espera entre que una oleada termina y comienza la siguiente.")]
    // [Min(0)]
    // public float timeBetweenWaves = 3f;

    [Tooltip("Segundos que el jugador tiene para prepararse antes de que aparezcan los enemigos de la primera oleada (aplica también tras cada transición).")]
    [Min(0)]
    public float preparationTime = 2f;

    [Header("Power-ups por oleada")]
    [Tooltip("Intervalo mínimo de aparición de power-ups en las oleadas iniciales.")]
    [Min(0)]
    public float initialMinSpawnInterval = 6f;

    [Tooltip("Intervalo máximo de aparición de power-ups en las oleadas iniciales.")]
    [Min(0)]
    public float initialMaxSpawnInterval = 14f;

    [Tooltip("Intervalo mínimo de aparición en oleadas avanzadas (el sistema interpola hacia este valor).")]
    [Min(0)]
    public float lateMinSpawnInterval = 14f;

    [Tooltip("Intervalo máximo de aparición en oleadas avanzadas.")]
    [Min(0)]
    public float lateMaxSpawnInterval = 28f;

    [Tooltip("A partir de qué oleada se considera que el juego está en la fase 'avanzada' para los intervalos de power-up.")]
    [Min(1)]
    public int lateGameWaveThreshold = 6;

    [Tooltip("Cantidad máxima de power-ups simultáneos en oleadas iniciales.")]
    [Min(1)]
    public int initialMaxSimultaneous = 3;

    [Tooltip("Cantidad máxima de power-ups simultáneos en oleadas avanzadas. Reducir hace el tablero menos 'ruidoso' sin eliminar la herramienta estratégica.")]
    [Min(1)]
    public int lateMaxSimultaneous = 2;

    [Header("Score")]
    [Tooltip("Puntos base por completar una oleada. Se multiplica por el número de oleada.")]
    [Min(0)]
    public int baseWaveCompletionScore = 100;

    [Tooltip("Puntos por eliminar un enemigo (primera muerte). Con racha activa se duplica.")]
    [Min(0)]
    public int enemyKillScore = 100;

    [Tooltip("Puntos por recoger un power-up (sólo jugador).")]
    [Min(0)]
    public int powerUpPickupScore = 25;

    [Tooltip("Puntos por cada celda recorrida exitosamente (sólo jugador, sólo modo Waves).")]
    [Min(0)]
    public int movementScorePerCell = 5;

    [Tooltip("Cada cuántas celdas se suman los puntos de movimiento. 5 = cada 5 celdas se suman movementScorePerCell puntos.")]
    [Min(1)]
    public int movementScoreCellInterval = 5;



    [Header("Seguridad")]
    [Tooltip("Segundos máximos que puede durar una oleada antes de forzar su finalización. Cubre el caso de enemigos encerrados que no pueden morir. 0 = sin timeout.")]
    [Min(0)]
    public float waveTimeoutSeconds = 120f;
}