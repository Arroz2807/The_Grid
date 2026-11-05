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

    // TurboEffect necesita leer el color actual para poder restaurarlo
    // al terminar. Sólo lectura — SetColor() sigue siendo el único punto
    // donde el color cambia.
    public Color NormalColor => normalColor;

    public void SetMoveInterval(float interval)
    {
        moveInterval = Mathf.Max(0.01f, interval);
    }

    // El escudo es un estado que ShieldEffect activa y desactiva.
    // LightCycleController no sabe qué es un ShieldEffect — sólo sabe
    // si tiene escudo activo o no, y expone ese estado para que Step()
    // pueda consultarlo sin acoplarse al sistema de power-ups.
    private bool hasShield;

    public bool HasActiveShield => hasShield;

    public void ActivateShield() { hasShield = true; }
    public void DeactivateShield() { hasShield = false; }

    public int MaxTrailLength => maxTrailLength;

    /// <summary>
    /// Aumenta el límite máximo de segmentos de rastro de esta entidad.
    /// Sólo afecta a esta instancia — cada LightCycleController tiene su
    /// propia cola de rastro y su propio maxTrailLength. Si el nuevo valor
    /// supera el tope global indicado, se recorta. Devuelve false si ya
    /// está en el tope y el cambio no tuvo efecto.
    /// </summary>
    public bool TryIncreaseMaxTrailLength(int amount, int cap)
    {
        if (maxTrailLength >= cap) return false;

        maxTrailLength = Mathf.Min(maxTrailLength + amount, cap);
        return true;
    }


    private void Awake()
    {

        inputProvider = GetComponent<CombinedDirectionInputProvider>();

        // Si no existe el proveedor combinado, mantenemos el sistema anterior.
        // Esto permite que los enemigos sigan usando EnemyBrain.
        if (inputProvider == null)
        {
            inputProvider = GetComponent<IDirectionInputProvider>();
        }

        if (inputProvider == null)
        {
            Debug.LogError(
                $"{name}: falta un componente que implemente " +
                "IDirectionInputProvider. Desactivando este objeto."
            );

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
        Vector2Int? desiredDirection = inputProvider.GetDesiredDirection();

        if (!desiredDirection.HasValue)
            return;

        Vector2Int candidate = desiredDirection.Value;

        Debug.Log(
            $"[LightCycle] Giro recibido. " +
            $"Dirección actual: {direction}, " +
            $"Dirección solicitada: {candidate}"
        );

        // No permitir un giro de 180 grados.
        if (candidate == -direction)
        {
            Debug.LogWarning(
                $"[LightCycle] GIRO RECHAZADO: {candidate} es opuesto " +
                $"a la dirección actual {direction}."
            );
            return;
        }

        queuedDirection = candidate;

        Debug.Log(
            $"[LightCycle] Giro aceptado y encolado: {queuedDirection}."
        );
    }

    private void Step()
    {
        direction = queuedDirection;

        Vector2Int nextCell = currentCell + direction;

        if (!gridManager.IsInsideGrid(nextCell) || gridManager.IsCellOccupied(nextCell))
        {
            if (hasShield)
            {
                // El escudo absorbe el impacto. Se consume antes de buscar
                // una dirección alternativa, para que no se vuelva a
                // consultar si tampoco hay salida en la dirección elegida.
                hasShield = false;
                OnShieldConsumed?.Invoke(this);

                Vector2Int? fallback = FindFallbackDirection();
                if (fallback.HasValue)
                {
                    // Redirigimos y continuamos — el movimiento no se
                    // interrumpe, pero el jugador pierde el control de
                    // su dirección por este paso.
                    queuedDirection = fallback.Value;
                    direction = queuedDirection;
                    nextCell = currentCell + direction;
                }
                else
                {
                    // Sin ninguna salida válida, el escudo no puede salvar
                    // al participante — muere igual.
                    Die(nextCell);
                    return;
                }
            }
            else
            {
                Die(nextCell);
                return;
            }
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

        // Puntos por movimiento: sólo el jugador acumula score, y sólo
        // cuando realmente avanzó una celda — no puede explotarse estando
        // quieto porque sin Step() exitoso no hay puntos.
        if (isPlayer && MatchConfig.GameMode == GameMode.Waves)
        {
            ScoreManager.AddMovementScore();
        }

        OnCellEntered?.Invoke();
    }

    // Evento estático para que ShieldEffect sepa cuándo el escudo fue
    // consumido y pueda destruir el visual. Estático igual que OnAnyPlayerDied.
    public static event System.Action<LightCycleController> OnShieldConsumed;

    /// <summary>
    /// Busca la primera dirección (izquierda, derecha, o derecho) que lleve
    /// a una celda válida y libre, excluyendo el reverso de la dirección
    /// actual (para no girar 180°). Devuelve null si todas están bloqueadas.
    /// </summary>
    private Vector2Int? FindFallbackDirection()
    {
        Vector2Int[] candidates =
        {
            GridDirectionUtils.RotateLeft(direction),
            GridDirectionUtils.RotateRight(direction),
            direction,
        };

        foreach (Vector2Int candidate in candidates)
        {
            Vector2Int cell = currentCell + candidate;
            if (gridManager.IsInsideGrid(cell) && !gridManager.IsCellOccupied(cell))
            {
                return candidate;
            }
        }

        return null;
    }

    private void Die(Vector2Int attemptedCell)
    {
        isAlive = false;

        Debug.Log($"DERROTA: {name} chocó al intentar entrar en la celda {attemptedCell}.");

        OnAnyPlayerDied?.Invoke(this);

        Destroy(gameObject);
    }
}