using UnityEngine;

/// <summary>
/// Es el árbitro de la partida: instancia al jugador y a los enemigos que
/// indique MatchConfig, y decide qué significa que alguien haya muerto
/// (derrota si es el jugador; un enemigo menos si no, y victoria si era
/// el último).
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TrailManager trailManager;

    public static event System.Action<bool> OnMatchEnded;

    private int aliveEnemyCount;

    private void OnEnable()
    {
        LightCycleController.OnAnyPlayerDied += HandleEntityDied;
    }

    private void OnDisable()
    {
        LightCycleController.OnAnyPlayerDied -= HandleEntityDied;
    }

    private void Start()
    {
        GameObject playerInstance = Instantiate(playerPrefab);

        LightCycleController playerController =
            playerInstance.GetComponent<LightCycleController>();

        playerController.Initialize(
            gridManager,
            trailManager,
            isPlayer: true
        );

        SpawnEnemies(playerController);
    }

    private void SpawnEnemies(LightCycleController playerController)
    {
        var types = MatchConfig.EnemyTypes;

        for (int i = 0; i < types.Count; i++)
        {
            SpawnEnemy(i, types[i], playerController);
        }
    }

    private void SpawnEnemy(
        int index,
        EnemyType type,
        LightCycleController playerController)
    {
        GameObject enemyInstance = Instantiate(enemyPrefab);

        LightCycleController enemyController =
            enemyInstance.GetComponent<LightCycleController>();

        // Se pisa la posición inicial ANTES de Initialize/Start.
        // Esto permite que cada enemigo aparezca en una posición diferente.
        enemyController.SetStartPosition(
            GetEnemySpawnCell(index),
            GetEnemySpawnDirection(index)
        );

        enemyController.Initialize(
            gridManager,
            trailManager,
            isPlayer: false
        );

        // Asigna el color correspondiente al tipo de enemigo.
        // LightCycleController se encarga de calcular automáticamente
        // el color normal, el color oscurecido y el color del rastro.
        enemyController.SetColor(
            GetEnemyColor(type)
        );

        EnemyBrain enemyBrain =
            enemyInstance.GetComponent<EnemyBrain>();

        // El objetivo, por ahora, siempre es el jugador.
        // El comportamiento concreto depende del EnemyType.
        enemyBrain.Initialize(
            gridManager,
            EnemyBehaviorFactory.Create(type),
            playerController
        );

        aliveEnemyCount++;
    }

    /// <summary>
    /// Devuelve el color correspondiente al tipo de enemigo.
    /// Este color se asigna al LightCycleController y, a partir de ahí,
    /// también se utiliza para generar el rastro y la explosión de muerte
    /// con el mismo color.
    /// </summary>
    private Color GetEnemyColor(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Chase:
                return Color.red;

            case EnemyType.Predict:
                return Color.green;

            case EnemyType.Ambush:
                return Color.magenta;

            case EnemyType.Random:
                return Color.yellow;

            default:
                return Color.white;
        }
    }

    // Reparte hasta 4 enemigos cerca de las esquinas de la grilla,
    // calculadas a partir de Columns/Rows — nunca con números fijos, para
    // que siga funcionando sin importar el tamaño de grilla configurado.
    private Vector2Int GetEnemySpawnCell(int index)
    {
        const int margin = 2;

        int maxX = gridManager.Columns - 1 - margin;
        int maxY = gridManager.Rows - 1 - margin;

        switch (index)
        {
            case 0:
                return new Vector2Int(margin, margin);

            case 1:
                return new Vector2Int(maxX, margin);

            case 2:
                return new Vector2Int(margin, maxY);

            default:
                return new Vector2Int(maxX, maxY);
        }
    }

    private Vector2Int GetEnemySpawnDirection(int index)
    {
        return index < 2
            ? Vector2Int.up
            : Vector2Int.down;
    }

    private void HandleEntityDied(LightCycleController died)
    {
        if (died.IsPlayer)
        {
            EndMatch(playerWon: false);
            return;
        }

        aliveEnemyCount--;

        if (aliveEnemyCount <= 0)
        {
            EndMatch(playerWon: true);
        }
    }

    private void EndMatch(bool playerWon)
    {
        Debug.Log(
            playerWon
                ? "GameManager: victoria — todos los enemigos fueron eliminados."
                : "GameManager: derrota — el jugador chocó."
        );

        OnMatchEnded?.Invoke(playerWon);
    }
}