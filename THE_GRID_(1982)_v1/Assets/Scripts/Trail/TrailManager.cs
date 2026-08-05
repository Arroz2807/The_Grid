using UnityEngine;

/// <summary>
/// Encargado de la parte VISUAL del rastro: instanciar una sección
/// permanente en una celda dada. No decide si hubo colisión ni cuándo hay
/// que dejar rastro — eso lo decide LightCycleController, que es quien lo
/// llama.
/// </summary>
public class TrailManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject trailSegmentPrefab;
    [SerializeField] private Transform trailContainer;

    /// <summary>
    /// Instancia una sección de rastro en la celda indicada. Nunca se
    /// destruye ni se recicla (sin pooling): el requisito es que el rastro
    /// sea permanente, así que no hay ningún mecanismo de limpieza acá a
    /// propósito.
    /// </summary>
    public void SpawnTrailSegment(Vector2Int cell)
    {
        Vector3 worldPos = gridManager.GridToWorld(cell);
        GameObject segment = Instantiate(trailSegmentPrefab, worldPos, Quaternion.identity, trailContainer);

        // Igual que con el jugador: la escala visual depende de CellSize,
        // no de un valor fijo en el prefab.
        segment.transform.localScale = Vector3.one * gridManager.CellSize;
    }
}