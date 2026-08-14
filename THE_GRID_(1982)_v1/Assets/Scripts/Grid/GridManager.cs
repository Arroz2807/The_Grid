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
    // este momento. Este array no distingue entre esos dos casos a
    // propósito: para la pregunta "¿puedo entrar acá?", da exactamente lo
    // mismo por qué está ocupada.
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

    /// <summary>
    /// Marca una celda como ocupada. La llama LightCycleController tanto
    /// al aparecer en su celda inicial como al entrar a cada celda nueva
    /// — con rastro encendido o apagado, siempre: una entidad viva ocupa
    /// físicamente el lugar donde está parada.
    /// </summary>
    public void SetCellOccupied(Vector2Int cell)
    {
        if (!IsInsideGrid(cell)) return;
        occupiedCells[cell.x, cell.y] = true;
    }

    /// <summary>
    /// Libera una celda. Se usa únicamente cuando una entidad abandona una
    /// celda CON el rastro apagado — si el rastro está encendido, la celda
    /// debe seguir ocupada para siempre, así que esto nunca se llama en
    /// ese caso.
    /// </summary>
    public void ClearCellOccupied(Vector2Int cell)
    {
        if (!IsInsideGrid(cell)) return;
        occupiedCells[cell.x, cell.y] = false;
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