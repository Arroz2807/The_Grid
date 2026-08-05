using UnityEngine;

/// <summary>
/// Única fuente de verdad sobre el estado de la grilla: dimensiones, tamaño
/// de celda, y qué celdas están ocupadas. No sabe nada de jugadores, del
/// rastro, ni de cómo se dibuja nada — solo responde preguntas sobre la
/// grilla. Esto la hace reutilizable por cualquier sistema futuro (IA,
/// power-ups, obstáculos) sin acoplarla a un jugador en particular.
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

    // Grilla lógica de ocupación. true = la celda está bloqueada (por rastro).
    // Los límites del mapa se resuelven aparte, con IsInsideGrid, así que este
    // array solo necesita preocuparse por lo que hay ADENTRO de la grilla.
    private bool[,] occupiedCells;

    // Propiedades de solo lectura: cualquier otra clase puede LEER estos
    // valores (por ejemplo, para escalar un prefab según cellSize), pero
    // solo GridManager puede modificarlos. Esto evita que otra clase
    // desincronice el tamaño de la grilla "por accidente".
    public int Columns => columns;
    public int Rows => rows;
    public float CellSize => cellSize;

    private void Awake()
    {
        // El array se crea recién acá, leyendo los valores que hayas puesto
        // en el Inspector. Si cambiás columns/rows antes de darle Play, el
        // array se adapta solo — no hay ningún número de grilla hardcodeado
        // en el código.
        occupiedCells = new bool[columns, rows];
    }

    /// <summary>
    /// Convierte una coordenada de grilla (por ejemplo, celda (3,5)) en una
    /// posición de mundo, ubicada en el CENTRO de esa celda. Usamos la
    /// posición del propio transform de GridManager como origen de la
    /// grilla: así podés mover el GameObject GridManager en la escena y
    /// toda la grilla se mueve con él, sin tocar código.
    /// </summary>
    public Vector3 GridToWorld(Vector2Int cell)
    {
        float worldX = cell.x * cellSize + cellSize * 0.5f;
        float worldY = cell.y * cellSize + cellSize * 0.5f;
        return transform.position + new Vector3(worldX, worldY, 0f);
    }

    /// <summary>
    /// True si la coordenada cae dentro de los límites de la grilla.
    /// Salir de este rango es, para el jugador, chocar contra la pared.
    /// </summary>
    public bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
    }

    /// <summary>
    /// True si la celda está dentro de la grilla Y marcada como ocupada.
    /// Una celda fuera de la grilla NO se considera "ocupada" acá — ese caso
    /// ya lo cubre IsInsideGrid, y las llamamos por separado para poder
    /// distinguir en el futuro entre "moriste por pared" y "moriste por
    /// rastro" si quisieras un mensaje distinto para cada caso.
    /// </summary>
    public bool IsCellOccupied(Vector2Int cell)
    {
        if (!IsInsideGrid(cell)) return false;
        return occupiedCells[cell.x, cell.y];
    }

    /// <summary>
    /// Marca una celda como ocupada. La llama TrailManager cada vez que un
    /// ciclo de luz deja una sección de rastro atrás.
    /// </summary>
    public void SetCellOccupied(Vector2Int cell)
    {
        if (!IsInsideGrid(cell)) return;
        occupiedCells[cell.x, cell.y] = true;
    }

#if UNITY_EDITOR
    // Dibuja la grilla en la vista de Escena (no en el juego) para que
    // puedas ver de un vistazo dónde caen los límites, sin tener que darle
    // Play. Solo se compila en el Editor, así que no afecta el build final.
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