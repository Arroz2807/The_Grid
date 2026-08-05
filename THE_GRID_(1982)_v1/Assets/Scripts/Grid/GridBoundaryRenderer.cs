using UnityEngine;

/// <summary>
/// Dibuja los límites de la grilla como cuatro paredes visibles durante el
/// juego. El Gizmo de GridManager sólo se ve en la vista Scene del Editor
/// — nunca en el Game view ni en una build — por eso hacía falta esto
/// aparte. Es puramente visual: no participa de ninguna decisión de
/// colisión, esa sigue siendo responsabilidad exclusiva de
/// GridManager.IsInsideGrid().
/// </summary>
public class GridBoundaryRenderer : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    [Tooltip("Sprite cuadrado, igual que el usado para el rastro.")]
    [SerializeField] private GameObject wallSegmentPrefab;

    [SerializeField] private Color wallColor = Color.white;

    [Tooltip("Grosor visual de la pared, en unidades del mundo.")]
    [SerializeField] private float wallThickness = 0.15f;

    private void Awake()
    {
        // Si el script está en el mismo GameObject que GridManager, se
        // autocompleta la referencia — no hace falta arrastrarla a mano.
        if (gridManager == null)
        {
            gridManager = GetComponent<GridManager>();
        }
    }

    private void Start()
    {
        DrawBoundary();
    }

    private void DrawBoundary()
    {
        Vector3 origin = gridManager.transform.position;
        float cellSize = gridManager.CellSize;
        float width = gridManager.Columns * cellSize;
        float height = gridManager.Rows * cellSize;
        float t = wallThickness;

        // Las paredes horizontales se extienden "t" de más a cada lado
        // para cubrir las esquinas; las verticales encajan justo entre
        // ellas. Es un detalle cosmético, no afecta la lógica de colisión.
        CreateWall(origin + new Vector3(width / 2f, -t / 2f, 0f), new Vector3(width + t * 2f, t, 1f));
        CreateWall(origin + new Vector3(width / 2f, height + t / 2f, 0f), new Vector3(width + t * 2f, t, 1f));
        CreateWall(origin + new Vector3(-t / 2f, height / 2f, 0f), new Vector3(t, height, 1f));
        CreateWall(origin + new Vector3(width + t / 2f, height / 2f, 0f), new Vector3(t, height, 1f));
    }

    private void CreateWall(Vector3 position, Vector3 size)
    {
        GameObject wall = Instantiate(wallSegmentPrefab, position, Quaternion.identity, transform);
        wall.name = "Wall";
        wall.transform.localScale = size;

        if (wall.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.color = wallColor;
        }
    }
}