using UnityEngine;

/// <summary>
/// Encargado de la parte VISUAL del rastro: instanciar y destruir
/// secciones en una celda dada. No decide cuándo hay que dejar rastro ni
/// cuándo un segmento debe desaparecer por longitud máxima — eso lo
/// decide LightCycleController.
/// </summary>
public class TrailManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject trailSegmentPrefab;
    [SerializeField] private Transform trailContainer;

    public GameObject SpawnTrailSegment(Vector2Int cell, Color color)
    {
        Vector3 worldPos = gridManager.GridToWorld(cell);
        GameObject segment = Instantiate(trailSegmentPrefab, worldPos,
                                         Quaternion.identity, trailContainer);
        segment.transform.localScale = Vector3.one * gridManager.CellSize;

        if (segment.TryGetComponent(out SpriteRenderer sr))
        {
            sr.color = color;
        }

        return segment;
    }

    public void RemoveTrailSegment(GameObject segment)
    {
        if (segment != null) Destroy(segment);
    }

    /// <summary>
    /// Destruye todos los segmentos de rastro visibles y libera todas las
    /// celdas ocupadas en GridManager. WaveManager lo llama entre oleadas
    /// para que el tablero vuelva a estar limpio.
    /// </summary>
    public void ClearAllTrails()
    {
        // Destruir todos los segmentos visuales hijos del contenedor.
        for (int i = trailContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(trailContainer.GetChild(i).gameObject);
        }

        // Liberar toda la ocupación de la grilla. Las entidades vivas
        // volverán a marcar sus propias celdas en el siguiente Start().
        gridManager.ClearAllOccupied();

        Debug.Log("[TrailManager] Todos los rastros limpiados.");
    }
}