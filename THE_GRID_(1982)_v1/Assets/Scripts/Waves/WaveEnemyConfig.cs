using UnityEngine;

/// <summary>
/// Define la composición de una oleada específica: cuántos enemigos, de
/// qué tipos y con qué modificadores de velocidad e IA. Lo usa
/// WaveProgressionConfig para construir la tabla de oleadas. Clase
/// serializable (no ScriptableObject) porque no necesita existir como
/// asset independiente — vive embebida dentro de WaveProgressionConfig.
/// </summary>
[System.Serializable]
public class WaveEnemyConfig
{
    [Tooltip("Etiqueta descriptiva para el Inspector (no se usa en código).")]
    public string label = "Wave";

    [Tooltip("Tipos de enemigos de esta oleada, en orden. La longitud de esta lista define la cantidad total.")]
    public EnemyType[] enemyTypes = { EnemyType.Random };

    [Tooltip("Multiplicador de velocidad aplicado a todos los enemigos de esta oleada (1 = velocidad base del prefab).")]
    [Range(0.5f, 3f)]
    public float speedMultiplier = 1f;

    [Tooltip("Config de IA de power-ups para los enemigos de esta oleada. Null = sin búsqueda de power-ups.")]
    public PowerUpAIConfig powerUpAIConfig;
}