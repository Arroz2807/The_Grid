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

    // Evento estático: cualquier interesado (GameManager, DeathExplosionSpawner)
    // puede suscribirse sin que LightCycleController necesite conocerlos.
    public static event Action<LightCycleController> OnAnyPlayerDied;

    private IDirectionInputProvider inputProvider;
    private ITrailToggleInputProvider trailToggleInput;
    private SpriteRenderer spriteRenderer;
    private Color normalColor;
    private Color ghostColor;

    private Vector2Int currentCell;
    private Vector2Int direction;
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

            // El color fantasma no es un color aparte elegido a mano: se
            // calcula a partir del color propio del jugador, oscurecido.
            // Así, si cada jugador termina teniendo su propio color (por
            // ejemplo pensando en multijugador), cada uno tiene su propio
            // tono de "fantasma" automáticamente, sin configurar nada más.
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

        direction = turn == TurnInput.Left
            ? RotateLeft(direction)
            : RotateRight(direction);
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