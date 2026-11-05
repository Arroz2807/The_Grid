using UnityEngine;

/// <summary>
/// Define cómo progresan los enemigos oleada a oleada. Contiene una tabla
/// explícita de oleadas predefinidas (editables desde el Inspector sin
/// tocar código) y reglas de escalado para cuando se superan las
/// predefinidas. Separada de WaveSystemConfig porque esta es la
/// configuración que se ajusta con más frecuencia durante el balance.
/// </summary>
[CreateAssetMenu(fileName = "WaveProgressionConfig",
                 menuName = "The Grid/Waves/Wave Progression Config")]
public class WaveProgressionConfig : ScriptableObject
{
    [Header("Oleadas predefinidas (Waves 1-8)")]
    [Tooltip("Configuración explícita de cada oleada. Índice 0 = Wave 1. Se edita directamente desde el Inspector sin tocar código.")]
    public WaveEnemyConfig[] predefinedWaves;

    [Header("Escalado para oleadas 9+ (automático)")]
    [Tooltip("Cantidad de enemigos base para la primera oleada de escalado.")]
    [Min(1)]
    public int scalingBaseEnemyCount = 4;

    [Tooltip("Cantidad máxima de enemigos simultáneos. Nunca se supera.")]
    [Min(1)]
    public int maxEnemyCount = 4;

    [Tooltip("Velocidad base para la primera oleada de escalado (multiplicador sobre el prefab).")]
    [Range(1f, 3f)]
    public float scalingBaseSpeedMultiplier = 1.3f;

    [Tooltip("Factor de incremento logarítmico de velocidad. Valores entre 0.03-0.08 dan curvas suaves.")]
    [Range(0.01f, 0.15f)]
    public float scalingSpeedLogFactor = 0.05f;

    [Tooltip("Velocidad máxima absoluta. Evita valores absurdos en oleadas muy avanzadas.")]
    [Range(1f, 3f)]
    public float maxSpeedMultiplier = 2f;

    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve la configuración para la oleada indicada (1-based). Usa
    /// la tabla predefinida si existe, o genera una config de escalado
    /// automático con curvas no lineales para oleadas más avanzadas.
    /// </summary>
    public WaveEnemyConfig GetWaveConfig(int waveNumber)
    {
        int index = waveNumber - 1;

        if (predefinedWaves != null && index < predefinedWaves.Length)
        {
            return predefinedWaves[index];
        }

        return BuildScaledConfig(waveNumber);
    }

    private WaveEnemyConfig BuildScaledConfig(int waveNumber)
    {
        int predefinedCount = predefinedWaves != null ? predefinedWaves.Length : 0;
        // cuántas oleadas llevamos en la fase de escalado (empieza en 1)
        int scalingIndex = waveNumber - predefinedCount;

        // ── Cantidad de enemigos: raíz cuadrada truncada ──────────────────
        // Crece rápido al principio y se estabiliza, nunca superando el máximo.
        // scalingIndex=1 → base, scalingIndex=4 → base+1, scalingIndex=9 → base+2...
        int enemyCount = Mathf.Min(
            scalingBaseEnemyCount + Mathf.FloorToInt(Mathf.Sqrt(scalingIndex - 1)),
            maxEnemyCount);

        // ── Velocidad: logarítmica ────────────────────────────────────────
        // Mathf.Log(x+1) crece rápido al principio y cada vez más lento.
        // scalingIndex=1 → base, scalingIndex=10 → base + log(10)*factor...
        float speed = Mathf.Min(
            scalingBaseSpeedMultiplier + scalingSpeedLogFactor * Mathf.Log(scalingIndex + 1),
            maxSpeedMultiplier);

        // ── Composición de IA: rota para no ser siempre igual ────────────
        EnemyType[] types = BuildEnemyTypes(enemyCount, waveNumber);

        return new WaveEnemyConfig
        {
            label = $"Wave {waveNumber} (auto-scaled)",
            enemyTypes = types,
            speedMultiplier = speed,
            powerUpAIConfig = null
        };
    }

    /// <summary>
    /// Genera una composición de IA que rota cada 3 oleadas para que el
    /// jugador no memorice siempre el mismo patrón. La composición se
    /// vuelve progresivamente más agresiva según la oleada.
    /// </summary>
    private EnemyType[] BuildEnemyTypes(int count, int waveNumber)
    {
        // El patrón rota cada 3 oleadas: 0, 1 o 2
        int pattern = ((waveNumber - 1) / 3) % 3;

        EnemyType[] types = new EnemyType[count];

        for (int i = 0; i < count; i++)
        {
            if (i == 0)
            {
                // El primer enemigo siempre es el más agresivo disponible
                types[i] = EnemyType.Chase;
            }
            else if (i == 1)
            {
                // El segundo rota: Chase, Predict, Ambush
                types[i] = pattern == 0 ? EnemyType.Predict
                         : pattern == 1 ? EnemyType.Ambush
                         : EnemyType.Chase;
            }
            else if (i == 2)
            {
                // El tercero rota distinto al segundo para variedad
                types[i] = pattern == 0 ? EnemyType.Ambush
                         : pattern == 1 ? EnemyType.Chase
                         : EnemyType.Predict;
            }
            else
            {
                // El cuarto (máximo) siempre Random — agrega imprevisibilidad
                // sin subir la dificultad de forma injusta
                types[i] = EnemyType.Random;
            }
        }

        return types;
    }
}