using UnityEngine;

/// <summary>
/// Configuración global del sistema de aparición de power-ups, separada
/// de las definiciones individuales. Vive como ScriptableObject para poder
/// compartirse entre escenas o cambiarse entre modos de juego sin tocar
/// código. PowerUpSpawner la lee al iniciar la partida.
/// </summary>
[CreateAssetMenu(fileName = "PowerUpSpawnConfig", menuName = "The Grid/Power-Up Spawn Config")]
public class PowerUpSpawnConfig : ScriptableObject
{
    [Header("Activación")]
    [Tooltip("Si false, no aparece ningún power-up sin importar el resto.")]
    public bool powerUpsEnabled = true;

    [Header("Cantidad")]
    [Tooltip("Máximo de power-ups simultáneos en la grilla.")]
    [Min(1)]
    public int maxSimultaneous = 3;

    [Header("Timing")]
    [Tooltip("Tiempo mínimo entre apariciones (segundos).")]
    [Min(0)]
    public float minSpawnInterval = 5f;
    [Tooltip("Tiempo máximo entre apariciones (segundos).")]
    [Min(0)]
    public float maxSpawnInterval = 12f;

    [Header("Posicionamiento")]
    [Tooltip("Distancia mínima en celdas respecto a cualquier participante vivo al spawnear.")]
    [Min(0)]
    public int minDistanceFromParticipants = 3;
    [Tooltip("Intentos máximos para encontrar una celda válida antes de cancelar el spawn.")]
    [Min(1)]
    public int maxPlacementAttempts = 30;

    [Header("Tipos habilitados")]
    [Tooltip("Lista de definiciones que pueden aparecer. El peso relativo de cada una controla la probabilidad.")]
    public PowerUpDefinition[] availableDefinitions;
}