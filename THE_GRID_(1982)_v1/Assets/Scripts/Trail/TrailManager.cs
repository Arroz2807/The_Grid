using UnityEngine;

/// <summary>
/// Encargado de la parte VISUAL del rastro: instanciar y destruir
/// secciones en una celda dada. No decide cuándo hay que dejar rastro ni
/// cuándo un segmento debe desaparecer por longitud máxima — eso lo
/// decide LightCycleController, que es quien lo llama.
/// </summary>
public class TrailManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject trailSegmentPrefab;
    [SerializeField] private Transform trailContainer;

    /// <summary>
    /// Instancia una sección de rastro en la celda indicada, con el color
    /// indicado, y devuelve el GameObject creado — quien lo llama es
    /// responsable de guardar esa referencia si más adelante necesita
    /// eliminarlo (por ejemplo, al superar la longitud máxima de rastro).
    /// </summary>
    public GameObject SpawnTrailSegment(Vector2Int cell, Color color)
    {
        Vector3 worldPos = gridManager.GridToWorld(cell);
        GameObject segment = Instantiate(trailSegmentPrefab, worldPos, Quaternion.identity, trailContainer);

        segment.transform.localScale = Vector3.one * gridManager.CellSize;

        if (segment.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            spriteRenderer.color = color;
        }

        return segment;
    }

    /// <summary>
    /// Elimina un segmento de rastro creado previamente con
    /// SpawnTrailSegment. Sólo se ocupa de la parte visual — liberar la
    /// celda en GridManager sigue siendo responsabilidad de quien pidió
    /// la eliminación.
    /// </summary>
    public void RemoveTrailSegment(GameObject segment)
    {
        if (segment != null)
        {
            Destroy(segment);
        }
    }
}