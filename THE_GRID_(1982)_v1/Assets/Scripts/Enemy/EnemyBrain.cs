using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Es el IDirectionInputProvider de un enemigo. En cada celda nueva
/// (evento OnCellEntered de LightCycleController): calcula qué
/// direcciones son legales, consulta opcionalmente a PowerUpSeeker si hay
/// un power-up que valga la pena perseguir, y si no, delega en
/// IEnemyBehavior. EnemyBrain orquesta; no decide la estrategia ni la
/// búsqueda de power-ups — eso es trabajo de sus colaboradores.
///
/// Tampoco decide cómo se percibe el tablero: sólo transporta
/// EnemyNavigationConfig hasta el comportamiento, a través del
/// EnemyDecisionContext.
/// </summary>
[RequireComponent(typeof(LightCycleController))]
public class EnemyBrain : MonoBehaviour, IDirectionInputProvider
{
    private GridManager gridManager;
    private IEnemyBehavior behavior;
    private LightCycleController controller;
    private LightCycleController target;

    // Opcional: si se asigna, la IA busca power-ups según esta config.
    // Si es null, ignora todos los power-ups — retrocompatible.
    private PowerUpSeeker powerUpSeeker;

    // Opcional: parámetros de percepción del tablero. Si es null, los
    // comportamientos usan su lógica mínima (Random cae al filtro de un
    // paso, previo al análisis de escape).
    private EnemyNavigationConfig navigation;

    private Vector2Int? pendingDirection;

#if UNITY_EDITOR
    // Celdas recorridas por este enemigo. Sólo alimenta las métricas de
    // prueba (promedio de pasos sobrevividos por comportamiento).
    private int enteredCells;
#endif

    private void Awake()
    {
        controller = GetComponent<LightCycleController>();
    }

    /// <summary>
    /// Llamado por GameManager después de instanciar. powerUpSpawner puede
    /// ser null (sin power-ups) o config puede ser null (IA ignora power-ups).
    /// navigationConfig puede ser null: la IA funciona igual, sin análisis
    /// de escape.
    /// </summary>
    public void Initialize(
        GridManager grid,
        IEnemyBehavior chosenBehavior,
        LightCycleController initialTarget,
        PowerUpSpawner powerUpSpawner = null,
        PowerUpAIConfig powerUpConfig = null,
        EnemyNavigationConfig navigationConfig = null)
    {
        gridManager = grid;
        behavior = chosenBehavior;
        target = initialTarget;
        navigation = navigationConfig;

        if (powerUpSpawner != null && powerUpConfig != null)
        {
            powerUpSeeker = new PowerUpSeeker(
                gridManager,
                powerUpConfig,
                powerUpSpawner.ActivePickups);
        }
    }

    private void OnEnable()
    {
        controller.OnCellEntered += HandleCellEntered;

#if UNITY_EDITOR
        LightCycleController.OnAnyPlayerDied += HandleAnyEntityDied;
#endif
    }

    private void OnDisable()
    {
        controller.OnCellEntered -= HandleCellEntered;

#if UNITY_EDITOR
        LightCycleController.OnAnyPlayerDied -= HandleAnyEntityDied;
#endif
    }

    private void HandleCellEntered()
    {
#if UNITY_EDITOR
        enteredCells++;
#endif

        List<Vector2Int> validDirections = GetValidDirections();

        if (validDirections.Count == 0) return;

        Vector2Int chosen;

        // 1. Intentar ir hacia un power-up (sólo si hay un seeker activo).
        if (powerUpSeeker != null)
        {
            Vector2Int? powerUpDir = powerUpSeeker.GetDirectionTowardsBestPickup(
                controller.CurrentCell,
                controller.Direction,
                validDirections);

            if (powerUpDir.HasValue)
            {
                chosen = powerUpDir.Value;
                pendingDirection = chosen;
                return;
            }
        }

        // 2. Sin power-up de interés: comportamiento normal.
        if (validDirections.Count == 1)
        {
            chosen = validDirections[0];
        }
        else
        {
            EnemyDecisionContext context = new EnemyDecisionContext(
                controller, validDirections, target, gridManager, navigation);

            chosen = behavior.ChooseDirection(context);
        }

        pendingDirection = chosen;
    }

    private List<Vector2Int> GetValidDirections()
    {
        List<Vector2Int> valid = new List<Vector2Int>();

        foreach (Vector2Int candidate in GridDirectionUtils.GetCandidateDirections(controller.Direction))
        {
            Vector2Int nextCell = controller.CurrentCell + candidate;
            if (gridManager.IsInsideGrid(nextCell) && !gridManager.IsCellOccupied(nextCell))
            {
                valid.Add(candidate);
            }
        }

        return valid;
    }

    public Vector2Int? GetDesiredDirection()
    {
        Vector2Int? result = pendingDirection;
        pendingDirection = null;
        return result;
    }

#if UNITY_EDITOR
    private void HandleAnyEntityDied(LightCycleController died)
    {
        if (died != controller) return;

        string behaviorName = behavior != null
            ? behavior.GetType().Name
            : "SinBehavior";

        EnemyNavigationDebug.RecordDeath(behaviorName, enteredCells);
    }
#endif

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (controller == null) return;

        Gizmos.color = Color.yellow;
        Vector3 from = transform.position;
        Vector3 to = from + new Vector3(controller.Direction.x, controller.Direction.y, 0f) * 0.6f;
        Gizmos.DrawLine(from, to);
        Gizmos.DrawSphere(to, 0.06f);

        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);
        }
    }

    /// <summary>
    /// Con el enemigo seleccionado en la jerarquía, muestra cuánto espacio
    /// deja cada dirección candidata (verde = mucho, rojo = poco). Se
    /// calcula sólo al dibujar, así que no afecta el juego.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (controller == null || gridManager == null || navigation == null)
            return;

        int horizon = Mathf.Max(1, navigation.escapeHorizonSteps);
        float size = gridManager.CellSize * 0.8f;

        foreach (Vector2Int candidate in GridDirectionUtils.GetCandidateDirections(controller.Direction))
        {
            Vector2Int nextCell = controller.CurrentCell + candidate;

            if (!gridManager.IsInsideGrid(nextCell) || gridManager.IsCellOccupied(nextCell))
                continue;

            EscapeResult escape = EscapeAnalyzer.Analyze(
                gridManager, nextCell, candidate, horizon);

            bool safe = escape.ReachableCells >=
                        Mathf.Max(1, navigation.minRoomCells);

            float normalized = Mathf.Clamp01(escape.ReachableCells / 40f);

            // Rojo = por debajo del umbral de seguridad; amarillo a verde
            // = seguro, con más intensidad cuanto más espacio deja.
            Gizmos.color = safe
                ? Color.Lerp(Color.yellow, Color.green, normalized)
                : Color.red;

            Vector3 center = gridManager.GridToWorld(nextCell);
            Gizmos.DrawWireCube(center, Vector3.one * size);

            UnityEditor.Handles.Label(
                center + Vector3.up * (size * 0.6f),
                escape.ReachableCells.ToString());
        }
    }
#endif
}
