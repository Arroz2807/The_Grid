using System;
using UnityEngine;

/// <summary>
/// Mueve al ciclo de luz celda por celda, a ritmo constante, aplicando el
/// giro pedido por el proveedor de input. Antes de cada paso, consulta a
/// GridManager si el camino está libre; si no lo está, muere. No sabe cómo
/// se guarda la ocupación de la grilla ni cómo se dibuja el rastro — solo
/// pide esas cosas a través de referencias a GridManager y TrailManager.
///
/// Esta clase la usan tanto el jugador como cada enemigo: no sabe ni le
/// importa si su IDirectionInputProvider es un teclado o una IA. Sí sabe
/// (porque se lo dicen al crearla) si es la entidad controlada por el
/// jugador humano — únicamente para exponerlo, nunca para comportarse
/// distinto por eso.
/// </summary>
public class LightCycleController : MonoBehaviour
{
    [Header("Dependencias")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TrailManager trailManager;

    [Header("Movimiento")]
    [Tooltip("Segundos entre cada paso de la grilla. Menor = más rápido.")]
    [SerializeField] private float moveInterval = 0.15f;

    [Header("Estado inicial")]
    [SerializeField] private Vector2Int startCell = new Vector2Int(8, 2);
    [SerializeField] private Vector2Int startDirection = Vector2Int.up;

    [Header("Rastro")]
    [Tooltip("Qué tan oscuro se ve el jugador mientras el rastro está apagado, relativo a su propio color. 1 = sin cambio, 0 = negro.")]
    [Range(0f, 1f)]
    [SerializeField] private float ghostDarkenFactor = 0.4f;

    [Tooltip("Qué tan oscuro es el rastro (y la explosión al morir) respecto al color propio. 1 = igual de brillante, 0 = negro.")]
    [Range(0f, 1f)]
    [SerializeField] private float trailDarkenFactor = 0.7f;

    public static event Action<LightCycleController> OnAnyPlayerDied;

    public event Action OnCellEntered;

    private IDirectionInputProvider inputProvider;
    private ITrailToggleInputProvider trailToggleInput;
    private SpriteRenderer spriteRenderer;

    private Color normalColor;
    private Color ghostColor;
    private Color trailColor;

    private Vector2Int currentCell;
    private Vector2Int direction;
    private Vector2Int queuedDirection;

    private float moveTimer;
    private bool isAlive = true;
    private bool trailEnabled = true;
    private bool isPlayer;

    public Vector2Int CurrentCell => currentCell;
    public Vector2Int Direction => direction;

    /// <summary>
    /// Color utilizado para generar el rastro y la explosión de muerte.
    /// Es una versión oscurecida del color propio de la entidad.
    /// </summary>
    public Color TrailColor => trailColor;

    /// <summary>
    /// Indica si el rastro está actualmente activado.
    ///
    /// Los comportamientos de IA pueden consultar este valor para saber
    /// si el rastro está prendido AHORA y decidir correctamente si
    /// necesitan solicitar un cambio.
    /// </summary>
    public bool TrailEnabled => trailEnabled;

    /// <summary>
    /// True si esta instancia es la entidad controlada por el jugador
    /// humano. GameManager es quien lo decide y lo asigna en Initialize()
    /// — esta clase nunca lo infiere por su cuenta (por ejemplo, mirando
    /// qué IDirectionInputProvider tiene), para no acoplar "quién soy" a
    /// "cómo me controlan", que son cosas conceptualmente distintas.
    /// </summary>
    public bool IsPlayer => isPlayer;

    private void Awake()
    {
        inputProvider = GetComponent<IDirectionInputProvider>();
        trailToggleInput = GetComponent<ITrailToggleInputProvider>();

        if (inputProvider == null || trailToggleInput == null)
        {
            Debug.LogError(
                $"{name}: falta un componente que implemente " +
                "IDirectionInputProvider y ITrailToggleInputProvider " +
                "(por ejemplo, KeyboardInputProvider o EnemyBrain). " +
                "Desactivando este objeto."
            );

            enabled = false;
            return;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
            ghostColor = DarkenColor(
                normalColor,
                ghostDarkenFactor
            );

            trailColor = DarkenColor(
                normalColor,
                trailDarkenFactor
            );
        }
    }

    /// <summary>
    /// Oscurece un color manteniendo su canal alpha.
    /// </summary>
    private static Color DarkenColor(Color color, float factor)
    {
        return new Color(
            color.r * factor,
            color.g * factor,
            color.b * factor,
            color.a
        );
    }

    /// <summary>
    /// Asigna el color propio de esta entidad y recalcula todos los
    /// colores derivados:
    ///
    /// - normalColor: color del cuerpo cuando el rastro está activo.
    /// - ghostColor: color del cuerpo cuando el rastro está apagado.
    /// - trailColor: color utilizado para el rastro y la explosión.
    ///
    /// GameManager llama a este método al crear cada enemigo,
    /// utilizando un color diferente según su EnemyType.
    /// </summary>
    public void SetColor(Color color)
    {
        normalColor = color;

        ghostColor = DarkenColor(
            normalColor,
            ghostDarkenFactor
        );

        trailColor = DarkenColor(
            normalColor,
            trailDarkenFactor
        );

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }

    /// <summary>
    /// Sobreescribe la celda y dirección iniciales configuradas en el
    /// prefab. La usa GameManager para repartir a los enemigos en
    /// distintas posiciones de la grilla — sin esto, todas las instancias
    /// creadas del mismo prefab arrancarían superpuestas en el mismo
    /// lugar. Debe llamarse antes de que corra Start().
    /// </summary>
    public void SetStartPosition(Vector2Int cell, Vector2Int dir)
    {
        startCell = cell;
        startDirection = dir;
    }

    /// <summary>
    /// Llamado por GameManager inmediatamente después de instanciar este
    /// jugador o enemigo.
    ///
    /// El parámetro isPlayer se decide en el momento de la creación —
    /// GameManager es el único lugar que sabe cuál de las instancias
    /// que crea es la del jugador humano.
    /// </summary>
    public void Initialize(
        GridManager grid,
        TrailManager trail,
        bool isPlayer)
    {
        gridManager = grid;
        trailManager = trail;
        this.isPlayer = isPlayer;
    }

    private void Start()
    {
        currentCell = startCell;
        direction = startDirection;
        queuedDirection = startDirection;

        transform.localScale =
            Vector3.one * gridManager.CellSize;

        transform.position =
            gridManager.GridToWorld(currentCell);

        gridManager.SetCellOccupied(currentCell);

        ApplyTrailVisualFeedback();

        OnCellEntered?.Invoke();
    }

    private void Update()
    {
        if (!isAlive) return;

        HandleTurnInput();
        HandleTrailToggleInput();

        moveTimer += Time.deltaTime;

        if (moveTimer >= moveInterval)
        {
            moveTimer -= moveInterval;
            Step();
        }
    }

    private void HandleTurnInput()
    {
        TurnInput turn = inputProvider.GetTurnInput();

        if (turn == TurnInput.None)
            return;

        Vector2Int candidateDirection =
            turn == TurnInput.Left
                ? GridDirectionUtils.RotateLeft(direction)
                : GridDirectionUtils.RotateRight(direction);

        // Nunca se permite girar 180 grados.
        if (candidateDirection != -direction)
        {
            queuedDirection = candidateDirection;
        }
    }

    private void HandleTrailToggleInput()
    {
        if (!trailToggleInput.WasTrailToggleRequested())
            return;

        trailEnabled = !trailEnabled;

        ApplyTrailVisualFeedback();
    }

    /// <summary>
    /// Actualiza visualmente el cuerpo de la entidad según el estado
    /// actual del rastro.
    ///
    /// Si el rastro está activo:
    ///     normalColor
    ///
    /// Si el rastro está apagado:
    ///     ghostColor
    /// </summary>
    private void ApplyTrailVisualFeedback()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = trailEnabled
            ? normalColor
            : ghostColor;
    }

    private void Step()
    {
        direction = queuedDirection;

        Vector2Int nextCell =
            currentCell + direction;

        // Choca contra los límites o una celda ocupada.
        if (!gridManager.IsInsideGrid(nextCell) ||
            gridManager.IsCellOccupied(nextCell))
        {
            Die(nextCell);
            return;
        }

        // Si el rastro está activo, dejamos un segmento permanente.
        if (trailEnabled)
        {
            trailManager.SpawnTrailSegment(
                currentCell,
                trailColor
            );
        }
        else
        {
            // Si el rastro está apagado, la celda anterior queda libre.
            gridManager.ClearCellOccupied(currentCell);
        }

        currentCell = nextCell;

        transform.position =
            gridManager.GridToWorld(currentCell);

        // La entidad ocupa físicamente su nueva celda.
        gridManager.SetCellOccupied(currentCell);

        OnCellEntered?.Invoke();
    }

    private void Die(Vector2Int attemptedCell)
    {
        isAlive = false;

        Debug.Log(
            $"DERROTA: {name} chocó al intentar entrar " +
            $"en la celda {attemptedCell}."
        );

        // El evento permite que GameManager sepa si murió
        // el jugador o un enemigo y que DeathExplosionSpawner
        // pueda generar la explosión usando TrailColor.
        OnAnyPlayerDied?.Invoke(this);

        Destroy(gameObject);
    }
}