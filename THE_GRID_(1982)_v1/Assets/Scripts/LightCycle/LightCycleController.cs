
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mueve al ciclo de luz celda por celda, a ritmo constante,
/// aplicando la dirección absoluta pedida por el proveedor de input.
///
/// Antes de cada paso, consulta a GridManager si el camino está libre.
/// El rastro tiene una longitud máxima configurable.
///
/// Cada segmento conecta el centro de la celda abandonada
/// con el centro de la nueva celda.
/// </summary>
public class LightCycleController : MonoBehaviour
{
    [Header("Dependencias")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TrailManager trailManager;

    [Header("Visual")]
    [SerializeField] private Transform visualTransform;

    [Header("Movimiento")]
    [Tooltip("Segundos entre cada paso de la grilla. Menor = más rápido.")]
    [SerializeField] private float moveInterval = 0.15f;

    [Tooltip("Interpola la posición visual entre celdas. La lógica, las colisiones y el rastro siguen siendo por celda.")]
    [SerializeField] private bool smoothVisualMovement = true;

    [Header("Entrada")]
    [Tooltip("Cantidad de giros que se pueden guardar mientras el ciclo sigue en la celda actual. " +
             "Un valor de 2 permite encadenar dos giros rápidos sin perder el primero.")]
    [Range(1, 3)]
    [SerializeField] private int maxBufferedTurns = 2;

    [Header("Estado inicial")]
    [SerializeField] private Vector2Int startCell = new Vector2Int(8, 2);
    [SerializeField] private Vector2Int startDirection = Vector2Int.up;

    [Header("Rastro")]
    [Tooltip("Qué tan oscuro es el rastro respecto al color propio. 1 = igual de brillante, 0 = negro.")]
    [Range(0f, 1f)]
    [SerializeField] private float trailDarkenFactor = 0.7f;

    [Tooltip("Cantidad máxima de segmentos de rastro que esta entidad puede tener a la vez.")]
    [Min(1)]
    [SerializeField] private int maxTrailLength = 15;

    public static event Action<LightCycleController> OnAnyPlayerDied;

    public event Action OnCellEntered;

    // Cola FIFO: el segmento más antiguo es el primero en desaparecer.
    private readonly Queue<TrailEntry> trailSegments =
        new Queue<TrailEntry>();

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

    // Controla el flash temporal del ciclo y de su rastro.
    private Coroutine visualColorFlashRoutine;
    private bool isVisualColorFlashing;
    private Color activeFlashColor;

    private Vector2Int currentCell;
    private Vector2Int direction;
    private Vector2Int queuedDirection;

    // Buffer corto de giros: se llena cada frame y se consume de a uno por
    // paso, para no perder el segundo giro cuando el jugador encadena dos
    // pulsaciones dentro de la misma celda.
    private readonly Queue<Vector2Int> bufferedTurns =
        new Queue<Vector2Int>(3);

    // Posiciones entre las que se interpola el sprite. La celda lógica
    // cambia de golpe en Step(); estas dos posiciones permiten que el
    // desplazamiento se vea continuo sin tocar la simulación.
    private Vector3 visualFromPosition;
    private Vector3 visualToPosition;

    private float moveTimer;
    private bool isAlive = true;
    private bool isPlayer;

    public Vector2Int CurrentCell => currentCell;
    public Vector2Int Direction => direction;
    public Color TrailColor => trailColor;

    /// <summary>
    /// Renderer del sprite visual del LightCycle.
    /// Permite que otros sistemas utilicen el renderer correcto
    /// sin tener que buscarlo directamente en el objeto raíz.
    /// </summary>
    public SpriteRenderer VisualRenderer => spriteRenderer;

    /// <summary>
    /// True si esta instancia es la entidad controlada por el jugador.
    /// </summary>
    public bool IsPlayer => isPlayer;

    // Expuesto para MatchStarter y el sistema de ramp-up.
    public float MoveInterval => moveInterval;

    // Color original para los efectos visuales.
    public Color NormalColor => normalColor;

    public void SetMoveInterval(float interval)
    {
        moveInterval = Mathf.Max(0.01f, interval);
    }

    // ---------------------------------------------------------
    // ESCUDO
    // ---------------------------------------------------------

    private bool hasShield;

    public bool HasActiveShield => hasShield;

    public static event Action<LightCycleController> OnShieldConsumed;

    public void ActivateShield()
    {
        hasShield = true;
    }

    public void DeactivateShield()
    {
        hasShield = false;
    }

    // ---------------------------------------------------------
    // LONGITUD DEL RASTRO
    // ---------------------------------------------------------

    public int MaxTrailLength => maxTrailLength;

    /// <summary>
    /// Aumenta el límite máximo de segmentos de rastro.
    /// </summary>
    public bool TryIncreaseMaxTrailLength(int amount, int cap)
    {
        if (maxTrailLength >= cap)
            return false;

        maxTrailLength = Mathf.Min(maxTrailLength + amount, cap);
        return true;
    }

    // ---------------------------------------------------------
    // INICIALIZACIÓN
    // ---------------------------------------------------------

    private void Awake()
    {
        inputProvider = GetComponent<CombinedDirectionInputProvider>();

        // Si no existe el proveedor combinado, se mantiene
        // el sistema anterior para los enemigos.
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

        // Primero intentamos buscar el SpriteRenderer dentro
        // del objeto visual asignado en el Inspector.
        if (visualTransform != null)
        {
            spriteRenderer = visualTransform.GetComponent<SpriteRenderer>();

            if (spriteRenderer == null)
            {
                spriteRenderer =
                    visualTransform.GetComponentInChildren<SpriteRenderer>();
            }
        }

        // Si no se asignó Visual, buscamos en los hijos del objeto raíz.
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        // Si encontramos un renderer, usamos su Transform como
        // referencia visual cuando todavía no se asignó uno.
        if (visualTransform == null && spriteRenderer != null)
        {
            visualTransform = spriteRenderer.transform;
        }

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
            trailColor = DarkenColor(normalColor, trailDarkenFactor);
        }
        else
        {
            Debug.LogWarning(
                $"{name}: no se encontró ningún SpriteRenderer " +
                "en el objeto ni en sus hijos."
            );
        }
    }

    private void OnEnable()
    {
        GameManager.OnMatchEnded += HandleMatchEnded;
        TrailManager.OnAllTrailsCleared += HandleAllTrailsCleared;
    }

    private void OnDisable()
    {
        GameManager.OnMatchEnded -= HandleMatchEnded;
        TrailManager.OnAllTrailsCleared -= HandleAllTrailsCleared;

        // Si el objeto se desactiva durante un flash,
        // restauramos los colores para evitar estados visuales colgados.
        if (visualColorFlashRoutine != null)
        {
            StopCoroutine(visualColorFlashRoutine);
            visualColorFlashRoutine = null;
        }

        isVisualColorFlashing = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }

        SetTrailSegmentsColor(trailColor);
    }

    private void HandleMatchEnded(bool playerWon)
    {
        isAlive = false;

        // Al congelarse la partida, dejamos el sprite exactamente en la
        // celda lógica para que no quede a mitad de camino.
        if (smoothVisualMovement)
        {
            transform.position = visualToPosition;
        }
    }

    /// <summary>
    /// TrailManager limpia el tablero entre oleadas y recicla sus segmentos.
    /// La cola local queda apuntando a segmentos que ahora pertenecen al
    /// pool, así que hay que vaciarla para no devolver al pool un segmento
    /// que otra entidad ya está usando.
    /// </summary>
    private void HandleAllTrailsCleared()
    {
        trailSegments.Clear();

        // ClearAllTrails() resetea toda la ocupación de la grilla, incluida
        // la celda de esta entidad. La volvemos a marcar para que nadie
        // pueda entrar en ella durante la siguiente oleada.
        if (isAlive && gridManager != null)
        {
            gridManager.SetCellOccupied(currentCell);
        }
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
    /// Asigna el color propio de esta entidad y recalcula
    /// el color derivado del rastro.
    /// </summary>
    public void SetColor(Color color)
    {
        normalColor = color;
        trailColor = DarkenColor(normalColor, trailDarkenFactor);

        if (spriteRenderer != null)
        {
            // Si hay un flash activo, dejamos que termine.
            // Al finalizar restaurará el nuevo color normal.
            if (!isVisualColorFlashing)
            {
                spriteRenderer.color = normalColor;
            }
        }
    }

    // ---------------------------------------------------------
    // FEEDBACK VISUAL TEMPORAL
    // ---------------------------------------------------------

    /// <summary>
    /// Cambia temporalmente el color del LightCycle y de todos
    /// sus segmentos de rastro.
    ///
    /// Los segmentos nuevos también reciben el color del flash
    /// mientras el efecto permanezca activo.
    /// </summary>
    public void FlashVisualColor(Color flashColor, float duration)
    {
        if (spriteRenderer == null)
            return;

        // Evita que dos flashes se ejecuten simultáneamente.
        if (visualColorFlashRoutine != null)
        {
            StopCoroutine(visualColorFlashRoutine);
            visualColorFlashRoutine = null;
        }

        activeFlashColor = flashColor;
        isVisualColorFlashing = true;

        visualColorFlashRoutine = StartCoroutine(
            VisualColorFlashRoutine(flashColor, duration)
        );
    }

    private IEnumerator VisualColorFlashRoutine(
        Color flashColor,
        float duration)
    {
        // Aplicar el flash al ciclo.
        spriteRenderer.color = flashColor;

        // Aplicar el flash a todos los segmentos existentes.
        SetTrailSegmentsColor(flashColor);

        yield return new WaitForSeconds(Mathf.Max(0.01f, duration));

        // Restaurar el color original del ciclo.
        if (spriteRenderer != null)
        {
            spriteRenderer.color = normalColor;
        }

        // Restaurar el color habitual del rastro.
        SetTrailSegmentsColor(trailColor);

        isVisualColorFlashing = false;
        visualColorFlashRoutine = null;
    }

    /// <summary>
    /// Cambia el color de todos los segmentos que todavía
    /// pertenecen a la cola FIFO.
    /// </summary>
    private void SetTrailSegmentsColor(Color color)
    {
        foreach (TrailEntry entry in trailSegments)
        {
            if (entry.Segment == null)
                continue;

            SpriteRenderer segmentRenderer =
                entry.Segment.GetComponent<SpriteRenderer>();

            if (segmentRenderer != null)
            {
                segmentRenderer.color = color;
            }
        }
    }

    /// <summary>
    /// Aplica el color correcto a un segmento recién creado.
    /// Si hay un flash activo, utiliza ese color temporal.
    /// </summary>
    private void ApplyCurrentTrailVisualColor(GameObject segment)
    {
        if (segment == null)
            return;

        SpriteRenderer segmentRenderer =
            segment.GetComponent<SpriteRenderer>();

        if (segmentRenderer == null)
            return;

        segmentRenderer.color = isVisualColorFlashing
            ? activeFlashColor
            : trailColor;
    }

    // ---------------------------------------------------------
    // ORIENTACIÓN VISUAL DEL CICLO
    // ---------------------------------------------------------

    /// <summary>
    /// Rota únicamente el objeto visual según la dirección actual.
    /// </summary>
    private void UpdateVisualRotation()
    {
        if (visualTransform == null)
            return;

        float angle = 0f;

        if (direction == Vector2Int.up)
            angle = 0f;
        else if (direction == Vector2Int.right)
            angle = -90f;
        else if (direction == Vector2Int.down)
            angle = 180f;
        else if (direction == Vector2Int.left)
            angle = 90f;

        visualTransform.localRotation =
            Quaternion.Euler(0f, 0f, angle);
    }

    /// <summary>
    /// Sobreescribe la posición y dirección iniciales.
    /// GameManager lo utiliza al crear jugadores y enemigos.
    /// </summary>
    public void SetStartPosition(Vector2Int cell, Vector2Int dir)
    {
        startCell = cell;
        startDirection = dir;
    }

    /// <summary>
    /// Asigna las dependencias al crear una entidad.
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

        UpdateVisualRotation();

        transform.localScale = Vector3.one * gridManager.CellSize;
        transform.position = gridManager.GridToWorld(currentCell);

        visualFromPosition = transform.position;
        visualToPosition = transform.position;

        gridManager.SetCellOccupied(currentCell);

        OnCellEntered?.Invoke();
    }

    // ---------------------------------------------------------
    // ACTUALIZACIÓN Y MOVIMIENTO
    // ---------------------------------------------------------

    private void Update()
    {
        if (!isAlive)
            return;

        ReadTurnInput();

        moveTimer += Time.deltaTime;

        if (moveTimer >= moveInterval)
        {
            moveTimer -= moveInterval;
            Step();
        }

        UpdateVisualPosition();
    }

    /// <summary>
    /// Guarda los giros que llegan de la fuente de input. Se lee todos los
    /// frames, pero el buffer se consume de a un giro por paso: así dos
    /// pulsaciones rápidas dentro de la misma celda no se pisan.
    /// </summary>
    private void ReadTurnInput()
    {
        if (inputProvider == null)
            return;

        Vector2Int? desiredDirection = inputProvider.GetDesiredDirection();

        if (!desiredDirection.HasValue)
            return;

        if (bufferedTurns.Count >= maxBufferedTurns)
            return;

        bufferedTurns.Enqueue(desiredDirection.Value);
    }

    /// <summary>
    /// Toma el primer giro válido del buffer. Descarta los que sean un
    /// reverso de la dirección actual y sigue con el siguiente, para que
    /// una pulsación inválida no tape una válida detrás.
    /// </summary>
    private void ApplyBufferedTurn()
    {
        while (bufferedTurns.Count > 0)
        {
            Vector2Int candidate = bufferedTurns.Dequeue();

            // Repetir la dirección actual es válido: es seguir recto.
            if (candidate == direction)
            {
                queuedDirection = candidate;
                return;
            }

            // No permitir un giro de 180 grados.
            if (candidate == -direction)
                continue;

            queuedDirection = candidate;
            return;
        }
    }

    /// <summary>
    /// Interpola la posición del sprite entre la celda anterior y la actual.
    /// No se toca ninguna celda lógica: sólo la representación visual.
    /// </summary>
    private void UpdateVisualPosition()
    {
        if (!smoothVisualMovement)
        {
            transform.position = visualToPosition;
            return;
        }

        float progress = moveInterval > 0.0001f
            ? Mathf.Clamp01(moveTimer / moveInterval)
            : 1f;

        transform.position = Vector3.Lerp(
            visualFromPosition,
            visualToPosition,
            progress
        );
    }

    /// <summary>
    /// Ejecuta un paso de movimiento. Comprueba colisiones,
    /// genera el rastro y actualiza la posición.
    /// </summary>
    private void Step()
    {
        // Aplicar como máximo un giro del buffer en este paso.
        ApplyBufferedTurn();
        direction = queuedDirection;

        Vector2Int nextCell = currentCell + direction;

        // -----------------------------------------------------
        // COMPROBACIÓN DE COLISIONES Y ESCUDO
        // -----------------------------------------------------

        if (!gridManager.IsInsideGrid(nextCell) ||
            gridManager.IsCellOccupied(nextCell))
        {
            if (hasShield)
            {
                hasShield = false;
                OnShieldConsumed?.Invoke(this);

                Vector2Int? fallback = FindFallbackDirection();

                if (fallback.HasValue)
                {
                    queuedDirection = fallback.Value;
                    direction = queuedDirection;
                    nextCell = currentCell + direction;
                }
                else
                {
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

        // Actualizar la orientación después de resolver
        // la dirección definitiva, incluyendo el escudo.
        UpdateVisualRotation();

        // -----------------------------------------------------
        // GENERACIÓN DEL RASTRO
        // -----------------------------------------------------

        // El segmento conecta el centro de la celda actual
        // con el centro de la celda a la que nos desplazamos.
        GameObject segment = trailManager.SpawnTrailSegment(
            currentCell,
            nextCell,
            trailColor
        );

        // Si hay un flash activo, el nuevo segmento también
        // debe recibir el color temporal.
        ApplyCurrentTrailVisualColor(segment);

        trailSegments.Enqueue(
            new TrailEntry(segment, currentCell)
        );

        // -----------------------------------------------------
        // CONTROL DE LONGITUD MÁXIMA (FIFO)
        // -----------------------------------------------------

        if (trailSegments.Count > maxTrailLength)
        {
            TrailEntry oldest = trailSegments.Dequeue();

            trailManager.RemoveTrailSegment(oldest.Segment);
            gridManager.ClearCellOccupied(oldest.Cell);
        }

        // -----------------------------------------------------
        // ACTUALIZAR POSICIÓN Y OCUPACIÓN
        // -----------------------------------------------------

        // La celda lógica se actualiza de inmediato. El sprite arranca en
        // la celda anterior y se desliza hasta la nueva durante el próximo
        // intervalo, para que el movimiento se vea continuo.
        visualFromPosition = gridManager.GridToWorld(currentCell);
        visualToPosition = gridManager.GridToWorld(nextCell);

        currentCell = nextCell;

        transform.position = smoothVisualMovement
            ? visualFromPosition
            : visualToPosition;

        gridManager.SetCellOccupied(currentCell);

        // Puntos por movimiento: sólo el jugador en modo Waves.
        if (isPlayer && MatchConfig.GameMode == GameMode.Waves)
        {
            ScoreManager.AddMovementScore();
        }

        OnCellEntered?.Invoke();
    }

    // ---------------------------------------------------------
    // DIRECCIONES ALTERNATIVAS DEL ESCUDO
    // ---------------------------------------------------------

    /// <summary>
    /// Busca una dirección alternativa libre, excluyendo
    /// el reverso de la dirección actual.
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

            if (gridManager.IsInsideGrid(cell) &&
                !gridManager.IsCellOccupied(cell))
            {
                return candidate;
            }
        }

        return null;
    }

    // ---------------------------------------------------------
    // DERROTA
    // ---------------------------------------------------------

    private void Die(Vector2Int attemptedCell)
    {
        isAlive = false;

        Debug.Log(
            $"DERROTA: {name} chocó al intentar entrar en la celda {attemptedCell}."
        );

        OnAnyPlayerDied?.Invoke(this);

        Destroy(gameObject);
    }
}
