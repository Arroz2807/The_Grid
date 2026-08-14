using UnityEngine;

/// <summary>
/// Dibuja visualmente los bordes internos de cada celda de la grilla.
/// 
/// Es un componente puramente visual: no modifica el estado de
/// GridManager, no ocupa celdas y no participa en ninguna colisión.
/// 
/// Cada celda recibe cuatro pequeños segmentos de borde. Los bordes
/// interiores utilizan un color más oscuro que el borde exterior de
/// GridBoundaryRenderer para que la grilla tenga una jerarquía visual:
/// 
///     Borde exterior  → más claro / destacado
///     Bordes internos → más oscuros / sutiles
/// </summary>
public class GridCellsRenderer : MonoBehaviour
{
    [Header("Dependencias")]
    [SerializeField] private GridManager gridManager;

    [Tooltip("Sprite cuadrado utilizado para dibujar cada segmento de borde.")]
    [SerializeField] private GameObject cellBorderSegmentPrefab;

    [Header("Color")]
    [Tooltip("Color base de los bordes internos de las celdas.")]
    [SerializeField] private Color cellBorderColor = new Color(0.25f, 0.25f, 0.25f, 1f);

    [Header("Grosor")]
    [Tooltip("Grosor visual de los bordes internos, en unidades del mundo.")]
    [SerializeField] private float borderThickness = 0.03f;

    private void Awake()
    {
        // Si el script está en el mismo GameObject que GridManager,
        // se autocompleta la referencia.
        if (gridManager == null)
        {
            gridManager = GetComponent<GridManager>();
        }
    }

    private void Start()
    {
        if (gridManager == null)
        {
            Debug.LogError(
                "GridCellsRenderer: no hay un GridManager asignado."
            );
            return;
        }

        if (cellBorderSegmentPrefab == null)
        {
            Debug.LogError(
                "GridCellsRenderer: no hay un Cell Border Segment Prefab asignado."
            );
            return;
        }

        DrawCellBorders();
    }

    /// <summary>
    /// Genera los bordes internos de todas las celdas.
    /// 
    /// Se dibujan únicamente las líneas que separan dos celdas.
    /// No se generan bordes sobre el perímetro exterior porque esa función
    /// ya pertenece a GridBoundaryRenderer.
    /// </summary>
    private void DrawCellBorders()
    {
        Vector3 origin = gridManager.transform.position;

        float cellSize = gridManager.CellSize;
        float width = gridManager.Columns * cellSize;
        float height = gridManager.Rows * cellSize;

        // ---------------------------------------------------------
        // Líneas verticales internas
        // ---------------------------------------------------------
        //
        // Hay una línea entre cada columna.
        // Si tenemos 16 columnas, existen 15 líneas verticales internas.
        //
        for (int x = 1; x < gridManager.Columns; x++)
        {
            float worldX = x * cellSize;

            Vector3 position =
                origin + new Vector3(
                    worldX,
                    height / 2f,
                    0f
                );

            Vector3 size =
                new Vector3(
                    borderThickness,
                    height,
                    1f
                );

            CreateBorderSegment(
                position,
                size,
                $"CellBorder_Vertical_{x}"
            );
        }

        // ---------------------------------------------------------
        // Líneas horizontales internas
        // ---------------------------------------------------------
        //
        // Hay una línea entre cada fila.
        // Si tenemos 16 filas, existen 15 líneas horizontales internas.
        //
        for (int y = 1; y < gridManager.Rows; y++)
        {
            float worldY = y * cellSize;

            Vector3 position =
                origin + new Vector3(
                    width / 2f,
                    worldY,
                    0f
                );

            Vector3 size =
                new Vector3(
                    width,
                    borderThickness,
                    1f
                );

            CreateBorderSegment(
                position,
                size,
                $"CellBorder_Horizontal_{y}"
            );
        }
    }

    /// <summary>
    /// Crea un segmento visual de borde y le asigna el color configurado.
    /// </summary>
    private void CreateBorderSegment(
        Vector3 position,
        Vector3 size,
        string objectName)
    {
        GameObject border = Instantiate(
            cellBorderSegmentPrefab,
            position,
            Quaternion.identity,
            transform
        );

        border.name = objectName;

        border.transform.localScale = size;

        if (border.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.color = cellBorderColor;
        }
        else
        {
            Debug.LogWarning(
                $"GridCellsRenderer: el prefab '{cellBorderSegmentPrefab.name}' " +
                "no tiene un SpriteRenderer."
            );
        }
    }
}