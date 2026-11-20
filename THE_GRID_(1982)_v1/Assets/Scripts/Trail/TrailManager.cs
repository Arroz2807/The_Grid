
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Encargado de la parte VISUAL del rastro:
/// instanciar, configurar y reciclar segmentos.
///
/// Cada segmento representa el recorrido entre dos centros
/// de celdas consecutivas.
///
/// Los segmentos se reutilizan mediante un pool interno: al expirar o al
/// limpiar el tablero se desactivan y vuelven al pool en vez de destruirse.
/// No decide cuándo se deja un rastro ni cuándo desaparece
/// por superar la longitud máxima. Esa responsabilidad
/// pertenece a LightCycleController.
/// </summary>
public class TrailManager : MonoBehaviour
{
    [Header("Dependencias")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject trailSegmentPrefab;
    [SerializeField] private Transform trailContainer;

    /// <summary>
    /// Se dispara después de limpiar el tablero entre oleadas. Cada
    /// LightCycleController vacía su cola local y vuelve a marcar su celda,
    /// porque los segmentos que tenía ahora pertenecen al pool.
    /// </summary>
    public static event System.Action OnAllTrailsCleared;

    // Segmentos inactivos listos para reutilizar.
    private readonly Queue<GameObject> pool = new Queue<GameObject>();

    // Evita devolver dos veces el mismo segmento al pool.
    private readonly HashSet<GameObject> pooledSegments =
        new HashSet<GameObject>();

    [Header("Sprite del rastro")]
    [SerializeField] private Sprite straightTrailSprite;

    [Tooltip("Porcentaje de la altura del PNG que ocupa el contenido visible del sprite.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float visibleLengthRatio = 0.68f;

    [Tooltip("Multiplicador de longitud para solapar ligeramente los extremos de segmentos consecutivos.")]
    [Range(1f, 1.1f)]
    [SerializeField] private float segmentLengthMultiplier = 1.02f;

    // ---------------------------------------------------------
    // GENERACIÓN DE SEGMENTOS
    // ---------------------------------------------------------

    /// <summary>
    /// Compatibilidad con llamadas anteriores.
    /// Genera un segmento desde la celda indicada hacia arriba.
    /// </summary>
    public GameObject SpawnTrailSegment(Vector2Int cell, Color color)
    {
        return SpawnTrailSegment(
            cell,
            cell + Vector2Int.up,
            color
        );
    }

    /// <summary>
    /// Compatibilidad con llamadas anteriores.
    /// Genera un segmento desde la celda indicada
    /// hacia la dirección recibida.
    /// </summary>
    public GameObject SpawnTrailSegment(
        Vector2Int cell,
        Color color,
        Vector2Int direction)
    {
        return SpawnTrailSegment(
            cell,
            cell + direction,
            color
        );
    }

    /// <summary>
    /// Compatibilidad con la versión anterior que recibía
    /// dirección de entrada y dirección de salida.
    ///
    /// Ahora solamente se utiliza la dirección de salida.
    /// </summary>
    public GameObject SpawnTrailSegment(
        Vector2Int cell,
        Color color,
        Vector2Int incomingDirection,
        Vector2Int outgoingDirection)
    {
        return SpawnTrailSegment(
            cell,
            cell + outgoingDirection,
            color
        );
    }

    /// <summary>
    /// Genera un segmento visual entre dos celdas.
    ///
    /// El sprite se posiciona en el punto medio entre
    /// sus centros y se rota según la dirección del recorrido.
    /// </summary>
    public GameObject SpawnTrailSegment(
        Vector2Int fromCell,
        Vector2Int toCell,
        Color color)
    {
        if (!ValidateDependencies())
            return null;

        if (straightTrailSprite == null)
        {
            Debug.LogError(
                "[TrailManager] Falta asignar Straight Trail Sprite."
            );

            return null;
        }

        if (visibleLengthRatio <= 0f)
        {
            Debug.LogError(
                "[TrailManager] Visible Length Ratio debe ser mayor que cero."
            );

            return null;
        }

        // -----------------------------------------------------
        // 1. OBTENER LAS POSICIONES EN EL MUNDO
        // -----------------------------------------------------

        Vector3 startWorld =
            gridManager.GridToWorld(fromCell);

        Vector3 endWorld =
            gridManager.GridToWorld(toCell);

        // Vector que representa el recorrido entre ambas celdas.
        Vector3 segmentDirection =
            endWorld - startWorld;

        // Distancia real entre los centros.
        float segmentDistance =
            segmentDirection.magnitude;

        if (segmentDistance <= 0f)
        {
            Debug.LogWarning(
                "[TrailManager] No se puede crear un segmento " +
                "entre dos posiciones iguales."
            );

            return null;
        }

        // -----------------------------------------------------
        // 2. CALCULAR EL PUNTO MEDIO
        // -----------------------------------------------------

        Vector3 midpoint =
            (startWorld + endWorld) * 0.5f;

        // -----------------------------------------------------
        // 3. CALCULAR LA ROTACIÓN
        // -----------------------------------------------------

        // El sprite original está orientado verticalmente,
        // apuntando hacia arriba.
        //
        // Por eso restamos 90 grados al ángulo del recorrido.

        float angle =
            Mathf.Atan2(
                segmentDirection.y,
                segmentDirection.x
            ) * Mathf.Rad2Deg - 90f;

        Quaternion rotation =
            Quaternion.Euler(0f, 0f, angle);

        // -----------------------------------------------------
        // 4. INSTANCIAR EL SEGMENTO
        // -----------------------------------------------------

        GameObject segment = RentSegment();

        if (segment == null)
            return null;

        segment.transform.SetParent(trailContainer, worldPositionStays: false);
        segment.transform.position = midpoint;
        segment.transform.rotation = rotation;

        SpriteRenderer sr =
            segment.GetComponent<SpriteRenderer>();

        if (sr == null)
        {
            Debug.LogError(
                "[TrailManager] TrailSegment no tiene SpriteRenderer."
            );

            Destroy(segment);
            return null;
        }

        sr.sprite = straightTrailSprite;
        sr.color = color;

        sr.flipX = false;
        sr.flipY = false;

        // -----------------------------------------------------
        // 5. CALCULAR LA ESCALA
        // -----------------------------------------------------

        // Altura original del sprite en unidades de Unity.
        float spriteHeight =
            straightTrailSprite.rect.height /
            straightTrailSprite.pixelsPerUnit;

        // Altura que ocupa realmente el contenido visible.
        float visibleHeight =
            spriteHeight * visibleLengthRatio;

        if (visibleHeight <= 0f)
        {
            Debug.LogError(
                "[TrailManager] La altura visible del sprite no es válida."
            );

            Destroy(segment);
            return null;
        }

        // Distancia que debe cubrir visualmente el segmento.
        // El pequeño multiplicador evita separaciones entre extremos.
        float targetLength =
            segmentDistance * segmentLengthMultiplier;

        // Escala necesaria para que el contenido visible
        // tenga la longitud deseada.
        float scale =
            targetLength / visibleHeight;

        // Escala uniforme para conservar las proporciones originales.
        segment.transform.localScale =
            Vector3.one * scale;

        return segment;
    }

    // ---------------------------------------------------------
    // VALIDACIONES
    // ---------------------------------------------------------

    private bool ValidateDependencies()
    {
        if (gridManager == null ||
            trailSegmentPrefab == null ||
            trailContainer == null)
        {
            Debug.LogError(
                "[TrailManager] Faltan referencias en el Inspector."
            );

            return false;
        }

        return true;
    }

    // ---------------------------------------------------------
    // ELIMINACIÓN DE SEGMENTOS
    // ---------------------------------------------------------

    /// <summary>
    /// Devuelve un segmento visual individual al pool.
    /// </summary>
    public void RemoveTrailSegment(GameObject segment)
    {
        if (segment == null)
            return;

        if (pooledSegments.Contains(segment))
            return;

        segment.SetActive(false);
        pooledSegments.Add(segment);
        pool.Enqueue(segment);
    }

    /// <summary>
    /// Toma un segmento inactivo del pool o crea uno nuevo si no hay.
    /// </summary>
    private GameObject RentSegment()
    {
        while (pool.Count > 0)
        {
            GameObject candidate = pool.Dequeue();

            if (candidate == null)
                continue;

            pooledSegments.Remove(candidate);
            candidate.SetActive(true);
            return candidate;
        }

        return Instantiate(
            trailSegmentPrefab,
            Vector3.zero,
            Quaternion.identity,
            trailContainer
        );
    }

    /// <summary>
    /// Destruye todos los segmentos visuales y libera las celdas
    /// ocupadas en GridManager.
    ///
    /// WaveManager lo utiliza para limpiar el tablero entre oleadas.
    /// </summary>
    public void ClearAllTrails()
    {
        if (trailContainer == null || gridManager == null)
        {
            Debug.LogError(
                "[TrailManager] No se pueden limpiar los rastros: " +
                "faltan referencias."
            );

            return;
        }

        // Reciclar todos los segmentos activos. Los que ya estaban en el
        // pool siguen inactivos y no se tocan.
        for (int i = trailContainer.childCount - 1; i >= 0; i--)
        {
            GameObject segment = trailContainer.GetChild(i).gameObject;

            if (!segment.activeSelf || pooledSegments.Contains(segment))
                continue;

            RemoveTrailSegment(segment);
        }

        // Liberar la ocupación de la grilla.
        gridManager.ClearAllOccupied();

        // Avisar a las entidades vivas para que vacíen sus colas locales y
        // vuelvan a marcar su celda actual (ClearAllOccupied las borró).
        OnAllTrailsCleared?.Invoke();

        Debug.Log("[TrailManager] Todos los rastros limpiados.");
    }
}
