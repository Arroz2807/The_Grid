using System;
using UnityEngine;

/// <summary>
/// Mueve al ciclo de luz celda por celda, a ritmo constante, aplicando la
/// dirección absoluta pedida por el proveedor de input. Antes de cada
/// paso, consulta a GridManager si el camino está libre; si no lo está,
/// muere. No sabe cómo se guarda la ocupación de la grilla ni cómo se
/// dibuja el rastro — solo pide esas cosas a través de referencias a
/// GridManager y TrailManager.
///
/// Esta clase la usan tanto el jugador como cada enemigo: no sabe ni le
/// importa si su IDirectionInputProvider es un teclado o una IA.
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
    public Color TrailColor => trailColor;
    public bool TrailEnabled => trailEnabled;

    /// <summary>
    /// True si esta instancia es la entidad controlada por el jugador
    /// humano. GameManager es quien lo decide y lo asigna en Initialize().
    /// </summary>
    public bool IsPlayer => isPlayer;

    private void Awake()
    {
        inputProvider = GetComponent<IDirectionInputProvider>();
        trailToggleInput = GetComponent<ITrailToggleInputProvider>();

        if (inputProvider == null || trailToggleInput == null)
        {
            Debug.LogError($"{name}: falta un componente que implemente IDirectionInputProvider y ITrailToggleInputProvider (por ejemplo, KeyboardInputProvider o EnemyBrain). Desactivando este objeto.");
            enabled = false;
            return;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;
            ghostColor = DarkenColor(normalColor, ghostDarkenFactor);
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

    /// <summary>
    /// Apenas termina la partida, cualquier entidad que siga viva deja de
    /// procesar input y de moverse. Sin esto, el jugador (si ganó) o los
    /// enemigos que sigan de pie (si perdiste) seguirían jugando de fondo
    /// mientras se muestra el panel de Game Over — y ahora que el
    /// movimiento usa las mismas cuatro teclas (WASD) que la navegación
    /// de ese panel, esas pulsaciones terminarían moviendo al personaje
    /// además de navegar el menú. Reutiliza el mismo isAlive que ya frena
    /// Update() al morir — no hace falta ningún estado nuevo.
    /// </summary>
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
    /// Asigna el color propio de esta entidad y recalcula los colores derivados:
    /// color normal, color con el rastro apagado y color del rastro/explosión.
    /// GameManager lo llama al crear cada enemigo.
    /// </summary>
    public void SetColor(Color color)
    {
        normalColor = color;
        ghostColor = DarkenColor(normalColor, ghostDarkenFactor);
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
        Vector2Int? desired = inputProvider.GetDesiredDirection();
        if (!desired.HasValue) return;

        Vector2Int candidateDirection = desired.Value;

        // Se compara contra "direction" (la última dirección YA
        // CONFIRMADA por un Step()), nunca contra "queuedDirection" —
        // así, sin importar cuántas direcciones distintas lleguen antes
        // del próximo Step(), ninguna combinación puede terminar
        // formando un giro de 180°: sólo se descarta si es EXACTAMENTE
        // la opuesta a la dirección confirmada.
        if (candidateDirection != -direction)
        {
            queuedDirection = candidateDirection;
        }
    }

    private void HandleTrailToggleInput()
    {
        if (!trailToggleInput.WasTrailToggleRequested()) return;

        trailEnabled = !trailEnabled;
        ApplyTrailVisualFeedback();
    }

    private void ApplyTrailVisualFeedback()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.color = trailEnabled
            ? normalColor
            : ghostColor;
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

        if (trailEnabled)
        {
            trailManager.SpawnTrailSegment(currentCell, trailColor);
        }
        else
        {
            gridManager.ClearCellOccupied(currentCell);
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