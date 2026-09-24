using UnityEngine;

/// <summary>
/// Única fuente de verdad sobre el estado de la grilla: dimensiones, tamaño
/// de celda, y qué celdas están ocupadas. No sabe nada de jugadores, del
/// rastro, ni de cómo se dibuja nada — solo responde preguntas sobre la
/// grilla. Esto la hace reutilizable por cualquier sistema (jugador, IA,
/// futuros power-ups u obstáculos) sin acoplarla a una entidad en particular.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Configuración de la grilla")]
    [Tooltip("Cantidad de columnas (eje X) de la grilla.")]
    [SerializeField] private int columns = 16;

    [Tooltip("Cantidad de filas (eje Y) de la grilla.")]
    [SerializeField] private int rows = 16;

    [Tooltip("Tamaño de una celda en unidades de mundo de Unity.")]
    [SerializeField] private float cellSize = 1f;

    // Grilla lógica de ocupación. true = la celda está bloqueada — ya sea
    // por rastro permanente, o porque una entidad viva está parada ahí en
    // este momento.
    private bool[,] occupiedCells;

    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;

    private void Awake()
    {
        occupiedCells = new bool[columns, rows];
    }

    public Vector3 GridToWorld(Vector2Int cell)
    {
        float worldX = cell.x * cellSize + cellSize * 0.5f;
        float worldY = cell.y * cellSize + cellSize * 0.5f;
        return transform.position + new Vector3(worldX, worldY, 0f);
    }

    public bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
    }

    public bool IsCellOccupied(Vector2Int cell)
    {
        if (!IsInsideGrid(cell)) return false;
        return occupiedCells[cell.x, cell.y];
    }

    public void SetCellOccupied(Vector2Int cell)
    {
        if (!IsInsideGrid(cell)) return;
        occupiedCells[cell.x, cell.y] = true;
    }

    public void ClearCellOccupied(Vector2Int cell)
    {
        if (!IsInsideGrid(cell)) return;
        occupiedCells[cell.x, cell.y] = false;
    }

    /// <summary>
    /// Intenta encontrar una celda libre al azar en hasta maxAttempts
    /// intentos. Devuelve true si encontró una; false si no hubo ninguna
    /// celda libre disponible en esa cantidad de intentos. Usada por
    /// PowerUpSpawner para encontrar posiciones válidas de aparición sin
    /// necesitar conocer la lógica de power-ups ni de participantes.
    /// </summary>
    public bool TryGetRandomFreeCell(int maxAttempts, out Vector2Int result)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector2Int candidate = new Vector2Int(
                Random.Range(0, columns),
                Random.Range(0, rows)
            );

            if (!IsCellOccupied(candidate))
            {
                result = candidate;
                return true;
            }
        }

        result = Vector2Int.zero;
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        for (int x = 0; x <= columns; x++)
        {
            Vector3 from = transform.position + new Vector3(x * cellSize, 0, 0);
            Vector3 to = transform.position + new Vector3(x * cellSize, rows * cellSize, 0);
            Gizmos.DrawLine(from, to);
        }
        for (int y = 0; y <= rows; y++)
        {
            Vector3 from = transform.position + new Vector3(0, y * cellSize, 0);
            Vector3 to = transform.position + new Vector3(columns * cellSize, y * cellSize, 0);
            Gizmos.DrawLine(from, to);
        }
    }
#endif
}