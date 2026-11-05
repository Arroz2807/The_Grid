using UnityEngine;

/// <summary>
/// Configuración de comportamiento de power-ups para una IA específica.
/// ScriptableObject para poder crear perfiles distintos (agresivo,
/// defensivo, aleatorio) y asignarlos a cada tipo de enemigo desde el
/// Inspector sin tocar código. Si no se asigna ninguno a EnemyBrain, la
/// IA ignora completamente los power-ups.
/// </summary>
[CreateAssetMenu(fileName = "PowerUpAIConfig", menuName = "The Grid/Power-Up AI Config")]
public class PowerUpAIConfig : ScriptableObject
{
    [Header("Activación")]
    [Tooltip("Si false, esta IA ignora todos los power-ups.")]
    public bool seekPowerUps = true;

    [Tooltip("Probabilidad (0-1) de intentar ir hacia un power-up cuando hay uno accesible. 1 = siempre intenta, 0 = nunca.")]
    [Range(0f, 1f)]
    public float seekProbability = 0.7f;

    [Header("Alcance")]
    [Tooltip("Distancia máxima en celdas (Manhattan) a la que un power-up es 'interesante'. Más allá de esto, se ignora.")]
    [Min(1)]
    public int maxInterestDistance = 6;

    [Tooltip("Profundidad máxima del BFS para verificar que existe una ruta transitable. Mayor = más preciso pero más costoso.")]
    [Min(1)]
    public int pathCheckDepth = 5;

    [Header("Prioridades (mayor número = mayor prioridad)")]
    [Min(0)] public int turboPriority = 2;
    [Min(0)] public int shieldPriority = 3;
    [Min(0)] public int trailPriority = 1;

    [Header("Riesgo")]
    [Tooltip("Si true, descarta rutas hacia power-ups que no tengan salida en el paso siguiente.")]
    public bool avoidDeadEndRoutes = true;
}