using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Es el árbitro de la partida. En modo Custom funciona exactamente como
/// antes. En modo Waves, sólo crea al jugador y delega la creación de
/// enemigos y la condición de fin de partida en WaveManager.
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TrailManager trailManager;
    [SerializeField] private MatchStarter matchStarter;
    [SerializeField] private PowerUpSpawner powerUpSpawner;
    [SerializeField] private WaveManager waveManager;

    [Header("IA — Power-ups (opcional por tipo)")]
    [SerializeField] private PowerUpAIConfig chaseAIConfig;
    [SerializeField] private PowerUpAIConfig predictAIConfig;
    [SerializeField] private PowerUpAIConfig ambushAIConfig;
    [SerializeField] private PowerUpAIConfig randomAIConfig;

    [Header("Spawn")]
    [Tooltip("Distancia Manhattan mínima entre el jugador y cada enemigo al spawnear.")]
    [SerializeField] private int minSpawnDistanceFromPlayer = 10;
    [Tooltip("Distancia Manhattan mínima entre enemigos al spawnear.")]
    [SerializeField] private int minSpawnDistanceBetweenEnemies = 3;
    [Tooltip("Intentos máximos para encontrar una celda de spawn válida.")]
    [SerializeField] private int maxSpawnAttempts = 100;
    [Tooltip("Celdas hacia adelante que se proyectan desde la posición del jugador para calcular zona de peligro.")]
    [SerializeField] private int playerProjectionCells = 5;

    public static event System.Action<bool> OnMatchEnded;

    private int aliveEnemyCount;
    private LightCycleController playerController;

    // Todas las entidades vivas actualmente (jugador + enemigos).
    // Se usan para destruirlas todas al terminar la partida.
    private readonly List<LightCycleController> activeEntities = new List<LightCycleController>();

    // Posiciones de enemigos ya spawneados en esta tanda (para calcular
    // la distancia mínima entre ellos al spawnear el siguiente).
    private readonly List<Vector2Int> spawnedEnemyCells = new List<Vector2Int>();

    private void OnEnable()
    {
        LightCycleController.OnAnyPlayerDied += HandleEntityDied;
        MatchStarter.OnMatchReady += HandleMatchReady;
    }

    private void OnDisable()
    {
        LightCycleController.OnAnyPlayerDied -= HandleEntityDied;
        MatchStarter.OnMatchReady -= HandleMatchReady;
    }

    private void HandleMatchReady()
    {
        spawnedEnemyCells.Clear();

        GameObject playerInstance = Instantiate(playerPrefab);
        playerController = playerInstance.GetComponent<LightCycleController>();

        // En Custom el jugador arranca en el centro de la grilla.
        // En Waves también, pero los enemigos aparecen en posiciones
        // aleatorias alejadas — garantiza que el jugador siempre tenga
        // espacio inicial independientemente del modo.
        Vector2Int centerCell = new Vector2Int(
            gridManager.Columns / 2,
            gridManager.Rows / 2);
        playerController.SetStartPosition(centerCell, Vector2Int.up);

        playerController.Initialize(gridManager, trailManager, isPlayer: true);
        matchStarter?.RegisterEntity(playerController);
        powerUpSpawner?.RegisterEntity(playerController);
        activeEntities.Add(playerController);

        if (MatchConfig.GameMode == GameMode.Waves)
        {
            if (waveManager == null)
            {
                Debug.LogError("[GameManager] waveManager es null — asignalo en el Inspector.");
            }
            else
            {
                waveManager.Initialize(this, powerUpSpawner, trailManager);
                waveManager.StartWaves();
            }
        }
        else
        {
            SpawnCustomEnemies();
        }
    }

    // ── Custom ───────────────────────────────────────────────────────────

    private void SpawnCustomEnemies()
    {
        var types = MatchConfig.EnemyTypes;
        aliveEnemyCount = 0;
        spawnedEnemyCells.Clear();

        // Las 4 esquinas se mezclan aleatoriamente — cada enemigo toma
        // una esquina disponible sin repetir. Con menos de 4 enemigos
        // siempre quedan esquinas vacías.
        List<Vector2Int> corners = GetShuffledCorners();

        for (int i = 0; i < types.Count; i++)
        {
            Vector2Int spawnCell = i < corners.Count ? corners[i] : corners[0];
            SpawnEnemyAtCell(types[i], playerController, spawnCell);
        }
    }

    private List<Vector2Int> GetShuffledCorners()
    {
        const int margin = 2;
        int maxX = gridManager.Columns - 1 - margin;
        int maxY = gridManager.Rows - 1 - margin;

        List<Vector2Int> corners = new List<Vector2Int>
        {
            new Vector2Int(margin, margin),
            new Vector2Int(maxX,   margin),
            new Vector2Int(margin, maxY),
            new Vector2Int(maxX,   maxY),
        };

        // Fisher-Yates shuffle
        for (int i = corners.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            Vector2Int tmp = corners[i];
            corners[i] = corners[j];
            corners[j] = tmp;
        }

        return corners;
    }

    // ── API pública para WaveManager ─────────────────────────────────────

    public LightCycleController GetPlayerController() => playerController;

    /// <summary>
    /// Llamado por WaveManager al iniciar cada oleada. Limpia la lista de
    /// celdas ocupadas por enemigos de la oleada anterior para que el
    /// cálculo de distancia mínima parta de cero.
    /// </summary>
    public void ResetEnemySpawnTracking()
    {
        spawnedEnemyCells.Clear();
    }

    public void SpawnWaveEnemy(int index, WaveEnemyConfig waveConfig, LightCycleController player)
    {
        if (index >= waveConfig.enemyTypes.Length) return;
        EnemyType type = waveConfig.enemyTypes[index];
        SpawnEnemyInternal(type, player, waveConfig.speedMultiplier, waveConfig.powerUpAIConfig);
    }

    public void EndMatch(bool playerWon)
    {
        Debug.Log(playerWon ? "GameManager: victoria." : "GameManager: derrota.");

        // Waves: NO se limpia acá. Los sobrevivientes se congelan solos al
        // recibir OnMatchEnded (isAlive = false) y quedan visibles junto
        // con los rastros. WavesGameOverController llama a
        // CleanupWavesMatch() en el mismo frame en que aparece el selector
        // de iniciales, así nunca se ve la grilla vacía sin panel.
        // Custom: cada entidad se destruye sola en Die() con su explosión,
        // y no hay nada más que limpiar.
        if (MatchConfig.GameMode != GameMode.Waves)
        {
            activeEntities.Clear();
        }

        OnMatchEnded?.Invoke(playerWon);
    }

    /// <summary>
    /// Destruye las entidades sobrevivientes y limpia todos los rastros de
    /// una partida de Waves. Lo llama WavesGameOverController justo cuando
    /// muestra el selector de iniciales, para que la limpieza ocurra detrás
    /// del panel y no antes de que aparezca.
    /// </summary>
    public void CleanupWavesMatch()
    {
        foreach (LightCycleController entity in activeEntities)
        {
            if (entity != null)
                Destroy(entity.gameObject);
        }
        activeEntities.Clear();

        trailManager?.ClearAllTrails();
    }

    // ── Spawn centralizado ───────────────────────────────────────────────

    /// <summary>
    /// Spawnea un enemigo en una celda aleatoria válida, respetando las
    /// distancias mínimas al jugador y a los demás enemigos ya spawneados.
    /// Compartido entre Custom y Waves.
    /// </summary>
    private void SpawnEnemy(EnemyType type, LightCycleController player)
    {
        SpawnEnemyInternal(type, player, speedMultiplier: 1f,
                           powerUpAIConfig: GetPowerUpAIConfig(type),
                           overrideCell: null);
    }

    private void SpawnEnemyAtCell(EnemyType type, LightCycleController player, Vector2Int cell)
    {
        SpawnEnemyInternal(type, player, speedMultiplier: 1f,
                           powerUpAIConfig: GetPowerUpAIConfig(type),
                           overrideCell: cell);
    }

    private void SpawnEnemyInternal(
        EnemyType type,
        LightCycleController player,
        float speedMultiplier,
        PowerUpAIConfig powerUpAIConfig,
        Vector2Int? overrideCell = null)
    {
        Vector2Int spawnCell = overrideCell ?? FindValidEnemySpawnCell(player.CurrentCell);
        Vector2Int spawnDirection = GetRandomCardinalDirection();

        GameObject enemyInstance = Instantiate(enemyPrefab);
        LightCycleController ec = enemyInstance.GetComponent<LightCycleController>();

        ec.SetStartPosition(spawnCell, spawnDirection);
        ec.Initialize(gridManager, trailManager, isPlayer: false);
        ec.SetColor(GetEnemyColor(type));

        if (speedMultiplier > 1f)
        {
            float newInterval = Mathf.Max(0.05f, ec.MoveInterval / speedMultiplier);
            ec.SetMoveInterval(newInterval);
        }

        EnemyBrain brain = enemyInstance.GetComponent<EnemyBrain>();
        brain.Initialize(gridManager, EnemyBehaviorFactory.Create(type),
                         player, powerUpSpawner, powerUpAIConfig);

        matchStarter?.RegisterEntity(ec);
        powerUpSpawner?.RegisterEntity(ec);
        activeEntities.Add(ec);

        spawnedEnemyCells.Add(spawnCell);

        // Conteo sólo en Custom — en Waves lo lleva WaveManager.
        if (MatchConfig.GameMode == GameMode.Custom)
        {
            aliveEnemyCount++;
        }
    }

    /// <summary>
    /// Encuentra una celda libre con distancia Manhattan mínima al jugador
    /// y a los demás enemigos ya spawneados. En Waves, prioriza celdas
    /// lo más lejanas posible del jugador y fuera de su trayectoria.
    /// </summary>
    private Vector2Int FindValidEnemySpawnCell(Vector2Int playerCell)
    {
        System.Collections.Generic.List<Vector2Int> dangerZone =
            BuildPlayerDangerZone(playerCell);

        Vector2Int bestCandidate = Vector2Int.zero;
        int bestDistance = -1;
        bool found = false;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            if (!gridManager.TryGetRandomFreeCell(1, out Vector2Int candidate))
                continue;

            int distToPlayer = ManhattanDistance(candidate, playerCell);
            if (distToPlayer < minSpawnDistanceFromPlayer) continue;

            // Excluir celdas dentro de la zona de peligro proyectada.
            bool inDanger = false;
            foreach (Vector2Int danger in dangerZone)
            {
                if (ManhattanDistance(candidate, danger) < minSpawnDistanceFromPlayer / 2)
                {
                    inDanger = true;
                    break;
                }
            }
            if (inDanger) continue;

            // Excluir celdas demasiado cerca de otros enemigos ya spawneados.
            bool tooCloseToEnemy = false;
            foreach (Vector2Int enemyCell in spawnedEnemyCells)
            {
                if (ManhattanDistance(candidate, enemyCell) < minSpawnDistanceBetweenEnemies)
                {
                    tooCloseToEnemy = true;
                    break;
                }
            }
            if (tooCloseToEnemy) continue;

            // En Waves guardamos el candidato más lejano del jugador.
            // En Custom tomamos el primero válido.
            if (MatchConfig.GameMode == GameMode.Waves)
            {
                if (distToPlayer > bestDistance)
                {
                    bestDistance = distToPlayer;
                    bestCandidate = candidate;
                    found = true;
                }
            }
            else
            {
                return candidate;
            }
        }

        if (found) return bestCandidate;

        // Fallback: relajar distancia entre enemigos, mantener la del jugador.
        Debug.LogWarning("[GameManager] Spawn sin distancia ideal — relajando restricción entre enemigos.");
        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            if (!gridManager.TryGetRandomFreeCell(1, out Vector2Int candidate)) continue;
            if (ManhattanDistance(candidate, playerCell) >= minSpawnDistanceFromPlayer)
                return candidate;
        }

        // Último recurso.
        Debug.LogWarning("[GameManager] Usando celda libre sin restricciones.");
        if (gridManager.TryGetRandomFreeCell(maxSpawnAttempts, out Vector2Int lastResort))
            return lastResort;

        return new Vector2Int(2, 2);
    }

    /// <summary>
    /// Lista de celdas que el jugador va a ocupar próximamente según su
    /// dirección actual. Se excluyen del spawn para evitar que un enemigo
    /// aparezca justo en el camino que el jugador lleva.
    /// </summary>
    private System.Collections.Generic.List<Vector2Int> BuildPlayerDangerZone(
        Vector2Int playerCell)
    {
        var zone = new System.Collections.Generic.List<Vector2Int> { playerCell };
        if (playerController == null) return zone;

        Vector2Int dir = playerController.Direction;
        for (int i = 1; i <= playerProjectionCells; i++)
        {
            Vector2Int projected = playerCell + dir * i;
            if (gridManager.IsInsideGrid(projected))
                zone.Add(projected);
        }

        return zone;
    }


    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    private static Vector2Int GetRandomCardinalDirection()
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        return dirs[UnityEngine.Random.Range(0, dirs.Length)];
    }

    // ── Muerte de entidades ───────────────────────────────────────────────

    private void HandleEntityDied(LightCycleController died)
    {
        activeEntities.Remove(died);
        if (died.IsPlayer)
        {
            if (MatchConfig.GameMode == GameMode.Waves)
            {
                waveManager?.HandlePlayerDied();
            }
            else
            {
                EndMatch(playerWon: false);
            }
            return;
        }

        if (MatchConfig.GameMode == GameMode.Waves)
        {
            waveManager?.HandleEnemyDied();
        }
        else
        {
            aliveEnemyCount--;
            if (aliveEnemyCount <= 0)
            {
                EndMatch(playerWon: true);
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Color GetEnemyColor(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Chase: return Color.red;
            case EnemyType.Predict: return Color.green;
            case EnemyType.Ambush: return Color.magenta;
            case EnemyType.Random: return Color.yellow;
            default: return Color.white;
        }
    }

    private PowerUpAIConfig GetPowerUpAIConfig(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Chase: return chaseAIConfig;
            case EnemyType.Predict: return predictAIConfig;
            case EnemyType.Ambush: return ambushAIConfig;
            case EnemyType.Random: return randomAIConfig;
            default: return null;
        }
    }
}