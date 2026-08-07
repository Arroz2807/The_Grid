using System;
using UnityEngine;

/// <summary>
/// Mueve al ciclo de luz celda por celda, a ritmo constante, aplicando el
/// giro pedido por el proveedor de input. Antes de cada paso, consulta a
/// GridManager si el camino está libre; si no lo está, muere. No sabe cómo
/// se guarda la ocupación de la grilla ni cómo se dibuja el rastro — solo
/// pide esas cosas a través de referencias a GridManager y TrailManager.
/// </summary>
[RequireComponent(typeof(KeyboardInputProvider))]
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

    // Evento estático: cualquier interesado (GameManager, DeathExplosionSpawner,
    // GameOverController) puede suscribirse sin que LightCycleController
    // necesite conocerlos.
    public static event Action<LightCycleController> OnAnyPlayerDied;

    private IDirectionInputProvider inputProvider;
    private ITrailToggleInputProvider trailToggleInput;
    private SpriteRenderer spriteRenderer;
    private Color normalColor;
    private Color ghostColor;

    private Vector2Int currentCell;

    // direction: la dirección CONFIRMADA, con la que el jugador se está
    // moviendo ahora mismo — sólo cambia dentro de Step().
    private Vector2Int direction;

    // queuedDirection: la dirección que se va a confirmar en el próximo
    // Step(). Cada giro nuevo se calcula a partir de "direction" (nunca a
    // partir de queuedDirection), así que no importa cuántos giros lleguen
    // antes del próximo paso: nunca se acumulan entre sí.
    private Vector2Int queuedDirection;

    private float moveTimer;
    private bool isAlive = true;
    private bool trailEnabled = true;

    private void Awake()
    {
        inputProvider = GetComponent<IDirectionInputProvider>();
        trailToggleInput = GetComponent<ITrailToggleInputProvider>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            normalColor = spriteRenderer.color;

            ghostColor = new Color(
                normalColor.r * ghostDarkenFactor,
                normalColor.g * ghostDarkenFactor,
                normalColor.b * ghostDarkenFactor,
                normalColor.a);
        }
    }

    // Llamado por GameManager inmediatamente después de instanciar este
    // jugador. Ver GameManager.Start().
    public void Initialize(GridManager grid, TrailManager trail)
    {
        gridManager = grid;
        trailManager = trail;
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
        if (turn == TurnInput.None) return;

        Vector2Int candidateDirection = turn == TurnInput.Left
            ? RotateLeft(direction)
            : RotateRight(direction);

        // Comparamos contra "direction" (la última dirección YA
        // CONFIRMADA por un Step()), nunca contra "queuedDirection". Si
        // comparáramos contra queuedDirection, dos giros de 90° pedidos
        // dentro del mismo intervalo de movimiento —antes de que el
        // próximo Step() confirme el primero— se irían acumulando entre
        // sí y podrían terminar formando un giro de 180° sin que el
        // jugador se haya movido siquiera una celda.
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
        spriteRenderer.color = trailEnabled ? normalColor : ghostColor;
    }

    private static Vector2Int RotateLeft(Vector2Int dir) => new Vector2Int(-dir.y, dir.x);
    private static Vector2Int RotateRight(Vector2Int dir) => new Vector2Int(dir.y, -dir.x);

    private void Step()
    {
        // Recién acá, en el momento exacto del paso, la dirección en cola
        // pasa a ser la dirección confirmada. Todo giro pedido antes de
        // este punto se evaluó siempre contra esta misma dirección, nunca
        // contra otro giro pendiente.
        direction = queuedDirection;

        Vector2Int nextCell = currentCell + direction;

        if (!gridManager.IsInsideGrid(nextCell) || gridManager.IsCellOccupied(nextCell))
        {
            Die(nextCell);
            return;
        }

        if (trailEnabled)
        {
            trailManager.SpawnTrailSegment(currentCell);
            gridManager.SetCellOccupied(currentCell);
        }

        currentCell = nextCell;
        transform.position = gridManager.GridToWorld(currentCell);
    }

    private void Die(Vector2Int attemptedCell)
    {
        isAlive = false;

        Debug.Log($"DERROTA: {name} chocó al intentar entrar en la celda {attemptedCell}.");

        OnAnyPlayerDied?.Invoke(this);

        Destroy(gameObject);
    }
}