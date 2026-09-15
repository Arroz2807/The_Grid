using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mueve al ciclo de luz celda por celda, a ritmo constante, aplicando la
/// dirección absoluta pedida por el proveedor de input. Antes de cada
/// paso, consulta a GridManager si el camino está libre; si no lo está,
/// muere. El rastro que deja tiene una longitud máxima configurable: al
/// superarla, el segmento más viejo desaparece, tanto visualmente como en
/// la ocupación lógica de la grilla.
///
/// Esta clase la usan tanto el jugador como cada enemigo — cada instancia
/// (cada prefab) puede tener su propia longitud máxima de rastro.
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
    [Tooltip("Qué tan oscuro es el rastro (y la explosión al morir) respecto al color propio. 1 = igual de brillante, 0 = negro.")]
    [Range(0f, 1f)]
    [SerializeField] private float trailDarkenFactor = 0.7f;

    [Tooltip("Cantidad máxima de segmentos de rastro que esta entidad puede tener a la vez. Al superarla, el más viejo desaparece.")]
    [Min(1)]
    [SerializeField] private int maxTrailLength = 15;

    public static event Action<LightCycleController> OnAnyPlayerDied;

    public event Action OnCellEntered;

    // Guarda, en orden, cada segmento de rastro propio junto con la celda
    // que ocupa — un Queue es exactamente la estructura correcta para
    // "el próximo en desaparecer es siempre el más viejo" (FIFO).
    private readonly Queue<TrailEntry> trailSegments = new Queue<TrailEntry>();

    private readonly struct TrailEntry
    {
        public readonly GameObject Segment;
        public readonly Vector2Int Cell;

        public TrailEntry(GameObject segment, Vector2Int cell)
        {
            Segment = segment;
            Cell = cell;
        }
    }

    private IDirectionInputProvider inputProvider;
    private SpriteRenderer spriteRenderer;

    private Color normalColor;
    private Color trailColor;

    private Vector2Int currentCell;
    private Vector2Int direction;
    private Vector2Int queuedDirection;

    private float moveTimer;
    private bool isAlive = true;
    private bool isPlayer;

    public Vector2Int CurrentCell => currentCell;
    public Vector2Int Direction => direction;
    public Color TrailColor => trailColor;

    /// <summary>
    /// True si esta instancia es la entidad controlada por el jugador
    /// humano. GameManager es quien lo decide y lo asigna en Initialize().
    /// </summary>
    public bool IsPlayer => isPlayer;

    // Expuesto para que MatchStarter pueda leer el intervalo configurado
    // en el prefab y usarlo como "destino" del ramp-up, y también para
    // pisarlo durante la aceleración inicial.
    public float MoveInterval => moveInterval;

    public void SetMoveInterval(float interval)
    {
        moveInterval = Mathf.Max(0.01f, interval);
    }


    private void Awake()
    {
        inputProvider = GetComponent<IDirectionInputProvider>();

        if (inputProvider == null)
        {
            Debug.LogError($"{name}: falta un componente que implemente IDirectionInputProvider (por ejemplo, KeyboardInputProvider o EnemyBrain). Desactivando este objeto.");
            enabled = false;
            return;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
            trailColor = DarkenColor(normalColor, trailDarkenFactor);
        }
    }

    private void OnEnable()
    {
        GameManager.OnMatchEnded += HandleMatchEnded;
    }

    private void OnDisable()
    {
        GameManager.OnMatchEnded -= HandleMatchEnded;
    }

    private void HandleMatchEnded(bool playerWon)
    {
        isAlive = false;
    }

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
    /// Asigna el color propio de esta entidad y recalcula el color
    /// derivado del rastro/explosión. GameManager lo llama al crear cada
    /// enemigo.
    /// </summary>
    public void SetColor(Color color)
    {
        normalColor = color;
        trailColor = DarkenColor(normalColor, trailDarkenFactor);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }
    }

    /// <summary>
    /// Sobreescribe la celda y dirección iniciales configuradas en el
    /// prefab. La usa GameManager para repartir a los enemigos en
    /// distintas posiciones de la grilla. Debe llamarse antes de que
    /// corra Start().
    /// </summary>
    public void SetStartPosition(Vector2Int cell, Vector2Int dir)
    {
        startCell = cell;
        startDirection = dir;
    }

    /// <summary>
    /// Llamado por GameManager inmediatamente después de instanciar este
    /// jugador o enemigo.
    /// </summary>
    public void Initialize(GridManager grid, TrailManager trail, bool isPlayer)
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

        transform.localScale = Vector3.one * gridManager.CellSize;
        transform.position = gridManager.GridToWorld(currentCell);

        gridManager.SetCellOccupied(currentCell);

        OnCellEntered?.Invoke();
    }

    private void Update()
    {
        if (!isAlive) return;

        HandleTurnInput();

        moveTimer += Time.deltaTime;

        if (moveTimer >= moveInterval)
        {
            moveTimer -= moveInterval;
            Step();
        }
    }

    private void HandleTurnInput()
    {
        Vector2Int? desired = inputProvider.GetDesiredDirection();
        if (!desired.HasValue) return;

        Vector2Int candidateDirection = desired.Value;

        // Se compara contra "direction" (la última dirección YA
        // CONFIRMADA por un Step()), nunca contra "queuedDirection" —
        // así, sin importar cuántas direcciones distintas lleguen antes
        // del próximo Step(), ninguna combinación puede terminar
        // formando un giro de 180°.
        if (candidateDirection != -direction)
        {
            queuedDirection = candidateDirection;
        }
    }

    private void Step()
    {
        direction = queuedDirection;

        Vector2Int nextCell = currentCell + direction;

        if (!gridManager.IsInsideGrid(nextCell) || gridManager.IsCellOccupied(nextCell))
        {
            Die(nextCell);
            return;
        }

        // El rastro ya no se puede apagar: siempre se deja un segmento en
        // la celda que se abandona.
        GameObject segment = trailManager.SpawnTrailSegment(currentCell, trailColor);
        trailSegments.Enqueue(new TrailEntry(segment, currentCell));

        // Si con este nuevo segmento se superó la longitud máxima, el más
        // viejo desaparece — visualmente y en la ocupación lógica de su
        // celda, que vuelve a estar libre.
        if (trailSegments.Count > maxTrailLength)
        {
            TrailEntry oldest = trailSegments.Dequeue();
            trailManager.RemoveTrailSegment(oldest.Segment);
            gridManager.ClearCellOccupied(oldest.Cell);
        }

        currentCell = nextCell;
        transform.position = gridManager.GridToWorld(currentCell);

        gridManager.SetCellOccupied(currentCell);

        OnCellEntered?.Invoke();
    }

    private void Die(Vector2Int attemptedCell)
    {
        isAlive = false;

        Debug.Log($"DERROTA: {name} chocó al intentar entrar en la celda {attemptedCell}.");

        OnAnyPlayerDied?.Invoke(this);

        Destroy(gameObject);
    }
}