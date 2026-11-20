using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// No persigue al jugador ni intenta interceptarlo en un punto de su
/// trayectoria como Predict. Busca terreno: una celda cercana al jugador
/// que, si el enemigo la ocupa, le deje menos rutas de escape.
///
/// El criterio principal es medir cuánto espacio pierde el jugador si esa
/// celda queda bloqueada. La medición se hace en el espacio de estados
/// (celda, dirección), respetando que el jugador tampoco puede girar 180°.
/// Por eso bloquear un pasillo detrás de él no cuenta como "mejora": no le
/// quita ninguna opción real. El analizador recibe la celda como bloqueo
/// virtual, sin modificar la grilla.
///
/// Como refuerzo se mide también el área libre (sin dirección) que le
/// queda al jugador, y se prefieren gargantas (pocas salidas) y celdas por
/// delante de su rumbo. El resultado es una posición de emboscada, no un
/// perseguidor: se queda en esa garganta durante ambushHoldSteps decisiones
/// antes de replantear, lo que hace legible su patrón.
///
/// Sigue siendo falible: sólo mira la celda y dirección actuales del
/// jugador, y su mapa de libertad es una aproximación local. Si el jugador
/// cambia de rumbo, la emboscada puede quedar en el lugar equivocado.
/// </summary>
public class AmbushBehavior : IEnemyBehavior
{
    // Parámetros del comportamiento anterior, usados sólo sin config.
    private const int MinLookahead = 2;
    private const int MaxLookahead = 8;
    private const int LookaheadStep = 2;

    private static readonly Vector2Int[] Neighbours4 =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left,
    };

    // Buffers por instancia: cada enemigo Ambush tiene los suyos. Se
    // reutilizan entre decisiones, así que después de la primera no hay
    // allocaciones.
    private readonly List<Vector2Int> candidateCells = new List<Vector2Int>(96);
    private readonly List<int> candidateSteps = new List<int>(96);
    private readonly List<Vector2Int> candidateFirstSteps = new List<Vector2Int>(96);

    // Emboscada actual y cuántas decisiones faltan para replantear.
    private Vector2Int ambushCell;
    private bool hasAmbushCell;
    private int stepsUntilReplan;

    public Vector2Int ChooseDirection(EnemyDecisionContext context)
    {
        List<Vector2Int> valid = context.ValidDirections;

        if (valid == null || valid.Count == 0)
            return Vector2Int.zero;

        if (context.Target == null)
            return GridDirectionUtils.PickRandomDirection(valid);

        EnemyNavigationConfig config = context.Navigation;

        if (config == null)
            return ChooseLegacy(context, valid);

        int horizon = Mathf.Max(1, config.escapeHorizonSteps);
        int minRoom = Mathf.Max(1, config.minRoomCells);

        bool holding = false;
        Vector2Int desiredDirection;

        if (HasUsableTarget(context, config))
        {
            stepsUntilReplan--;
            holding = true;

            desiredDirection = FirstStepTowards(
                context, config, ambushCell, out bool reachable);

            if (!reachable)
            {
                // La ocupación cambió y la garganta ya no es alcanzable:
                // se replantea en el acto.
                PlanAmbush(
                    context, config,
                    out ambushCell, out desiredDirection, out bool planned);

                hasAmbushCell = planned;
                stepsUntilReplan = planned
                    ? Mathf.Max(1, config.ambushHoldSteps)
                    : 0;
                holding = false;
            }
        }
        else
        {
            PlanAmbush(
                context, config,
                out ambushCell, out desiredDirection, out bool planned);

            hasAmbushCell = planned;
            stepsUntilReplan = planned
                ? Mathf.Max(1, config.ambushHoldSteps)
                : 0;
        }

        Vector2Int aimCell = hasAmbushCell
            ? ambushCell
            : context.Target.CurrentCell;

        Vector2Int chosen = EnemyNavigationSafety.Select(
            context, desiredDirection, aimCell, horizon, minRoom);

        int playerFreedom = -1;

#if UNITY_EDITOR
        if (hasAmbushCell)
        {
            playerFreedom = MeasurePlayerFreedom(
                context, config, ambushCell);
        }
#endif

        RecordDecision(holding, playerFreedom);

        return chosen;
    }

    // ── Gestión de la emboscada ─────────────────────────────────────────

    /// <summary>
    /// La garganta elegida sigue sirviendo mientras esté libre, cerca del
    /// jugador y no haya expirado el tiempo de permanencia.
    /// </summary>
    private bool HasUsableTarget(
        EnemyDecisionContext context,
        EnemyNavigationConfig config)
    {
        if (!hasAmbushCell || stepsUntilReplan <= 0)
            return false;

        GridManager grid = context.GridManager;

        if (!grid.IsInsideGrid(ambushCell))
            return false;

        // La celda que ocupa el propio enemigo cuenta como válida: es el
        // caso "ya llegué a la garganta".
        if (ambushCell != context.Self.CurrentCell &&
            grid.IsCellOccupied(ambushCell))
            return false;

        int radius = Mathf.Max(1, config.ambushRadius);

        return GridDirectionUtils.ManhattanDistance(
            ambushCell, context.Target.CurrentCell) <= radius + 1;
    }

    /// <summary>
    /// Elige la mejor celda de emboscada dentro del radio del jugador y
    /// devuelve también el primer paso del camino hacia ella.
    /// </summary>
    private void PlanAmbush(
        EnemyDecisionContext context,
        EnemyNavigationConfig config,
        out Vector2Int cell,
        out Vector2Int direction,
        out bool planned)
    {
        GridManager grid = context.GridManager;
        Vector2Int playerCell = context.Target.CurrentCell;
        Vector2Int playerDirection = context.Target.Direction;

        int radius = Mathf.Max(1, config.ambushRadius);
        int horizon = Mathf.Max(1, config.escapeHorizonSteps);
        int travelHorizon = radius + horizon;

        candidateCells.Clear();
        candidateSteps.Clear();
        candidateFirstSteps.Clear();

        if (!EscapeAnalyzer.TryBuildDistanceField(
                grid,
                context.Self.CurrentCell,
                context.Self.Direction,
                travelHorizon,
                out EscapeDistanceField field))
        {
            cell = playerCell;
            direction = Vector2Int.zero;
            planned = false;
            return;
        }

        // Fase 1: recolectar candidatos alcanzables. Se hace antes de los
        // análisis de libertad porque esos reutilizan los buffers de
        // celdas del analizador e invalidarían este campo de distancias.
        int minX = Mathf.Max(0, playerCell.x - radius);
        int maxX = Mathf.Min(grid.Columns - 1, playerCell.x + radius);
        int minY = Mathf.Max(0, playerCell.y - radius);
        int maxY = Mathf.Min(grid.Rows - 1, playerCell.y + radius);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                if (Mathf.Abs(x - playerCell.x) +
                    Mathf.Abs(y - playerCell.y) > radius)
                    continue;

                Vector2Int candidate = new Vector2Int(x, y);

                if (candidate == playerCell)
                    continue;

                if (grid.IsCellOccupied(candidate))
                    continue;

                if (!field.TryGetSteps(candidate, out int steps))
                    continue;

                if (!field.TryGetFirstStep(candidate, out Vector2Int firstStep))
                    continue;

                candidateCells.Add(candidate);
                candidateSteps.Add(steps);
                candidateFirstSteps.Add(firstStep);
            }
        }

        if (candidateCells.Count == 0)
        {
            cell = playerCell;
            direction = Vector2Int.zero;
            planned = false;
            return;
        }

        // Fase 2: puntuar cada candidato por cuánto le quita al jugador.
        int freedomHorizon = Mathf.Max(
            1,
            Mathf.Max(
                config.escapeHorizonSteps * 2,
                config.ambushRadius * 2));

        int freedomCap = Mathf.Max(1, grid.Columns * grid.Rows);

        int baselineState = EscapeAnalyzer.Analyze(
            grid, playerCell, playerDirection, freedomHorizon).ReachableCells;

        int baselineFree = EscapeAnalyzer.CountReachableCells(
            grid, playerCell, freedomCap, null);

        int bestIndex = -1;
        int bestStateReduction = -1;
        int bestFreeReduction = -1;
        int bestForward = int.MinValue;
        int bestDegree = int.MaxValue;
        int bestSteps = int.MaxValue;

        for (int i = 0; i < candidateCells.Count; i++)
        {
            Vector2Int candidate = candidateCells[i];

            int blockedState = EscapeAnalyzer.Analyze(
                grid, playerCell, playerDirection, freedomHorizon,
                candidate).ReachableCells;

            int blockedFree = EscapeAnalyzer.CountReachableCells(
                grid, playerCell, freedomCap, candidate);

            int stateReduction = Mathf.Max(0, baselineState - blockedState);
            int freeReduction = Mathf.Max(0, baselineFree - blockedFree);

            // "Hacia adelante" del rumbo actual del jugador: entre dos
            // gargantas equivalentes, cerrar el camino que va a tomar es
            // más útil que cerrar uno que ya dejó atrás.
            int forward =
                (candidate.x - playerCell.x) * playerDirection.x +
                (candidate.y - playerCell.y) * playerDirection.y;

            int degree = CountFreeNeighbours(grid, candidate);

            bool better =
                stateReduction > bestStateReduction ||
                (stateReduction == bestStateReduction &&
                 freeReduction > bestFreeReduction) ||
                (stateReduction == bestStateReduction &&
                 freeReduction == bestFreeReduction &&
                 forward > bestForward) ||
                (stateReduction == bestStateReduction &&
                 freeReduction == bestFreeReduction &&
                 forward == bestForward &&
                 degree < bestDegree) ||
                (stateReduction == bestStateReduction &&
                 freeReduction == bestFreeReduction &&
                 forward == bestForward &&
                 degree == bestDegree &&
                 candidateSteps[i] < bestSteps);

            if (!better)
                continue;

            bestIndex = i;
            bestStateReduction = stateReduction;
            bestFreeReduction = freeReduction;
            bestForward = forward;
            bestDegree = degree;
            bestSteps = candidateSteps[i];
        }

        cell = candidateCells[bestIndex];
        direction = candidateFirstSteps[bestIndex];
        planned = true;
    }

    /// <summary>
    /// Primer paso del camino óptimo hacia una celda. Si la celda ya es la
    /// propia, devuelve cero y la marca como alcanzada (no fuerza un
    /// replanteo sólo por haber llegado).
    /// </summary>
    private static Vector2Int FirstStepTowards(
        EnemyDecisionContext context,
        EnemyNavigationConfig config,
        Vector2Int targetCell,
        out bool reachable)
    {
        if (targetCell == context.Self.CurrentCell)
        {
            reachable = true;
            return Vector2Int.zero;
        }

        int maxSteps =
            Mathf.Max(1, config.ambushRadius) +
            Mathf.Max(1, config.escapeHorizonSteps);

        if (EscapeAnalyzer.TryBuildDistanceField(
                context.GridManager,
                context.Self.CurrentCell,
                context.Self.Direction,
                maxSteps,
                out EscapeDistanceField field) &&
            field.TryGetFirstStep(targetCell, out Vector2Int direction))
        {
            reachable = true;
            return direction;
        }

        reachable = false;
        return Vector2Int.zero;
    }

    /// <summary>
    /// Cuántas celdas le quedarían al jugador si el enemigo ocupara
    /// blockCell. Se usa sólo para las métricas de editor.
    /// </summary>
    private static int MeasurePlayerFreedom(
        EnemyDecisionContext context,
        EnemyNavigationConfig config,
        Vector2Int blockCell)
    {
        int freedomHorizon = Mathf.Max(
            1,
            Mathf.Max(
                config.escapeHorizonSteps * 2,
                config.ambushRadius * 2));

        return EscapeAnalyzer.Analyze(
            context.GridManager,
            context.Target.CurrentCell,
            context.Target.Direction,
            freedomHorizon,
            blockCell).ReachableCells;
    }

    private static int CountFreeNeighbours(GridManager grid, Vector2Int cell)
    {
        int free = 0;

        foreach (Vector2Int offset in Neighbours4)
        {
            Vector2Int neighbour = cell + offset;

            if (grid.IsInsideGrid(neighbour) &&
                !grid.IsCellOccupied(neighbour))
            {
                free++;
            }
        }

        return free;
    }

    // ── Comportamiento anterior (sin config) ────────────────────────────

    private static Vector2Int ChooseLegacy(
        EnemyDecisionContext context,
        List<Vector2Int> valid)
    {
        Vector2Int aimCell = FindBestCorneringPoint(context);

        return GridDirectionUtils.PickClosestDirection(
            context.Self.CurrentCell, valid, aimCell);
    }

    private static Vector2Int FindBestCorneringPoint(
        EnemyDecisionContext context)
    {
        Vector2Int targetCell = context.Target.CurrentCell;
        Vector2Int targetDir = context.Target.Direction;

        Vector2Int bestPoint = targetCell + targetDir * MaxLookahead;
        int bestEdgeDistance = int.MaxValue;

        for (int steps = MinLookahead; steps <= MaxLookahead; steps += LookaheadStep)
        {
            Vector2Int candidatePoint = targetCell + targetDir * steps;
            int edgeDistance = DistanceToNearestEdge(context.GridManager, candidatePoint);

            if (edgeDistance < bestEdgeDistance)
            {
                bestEdgeDistance = edgeDistance;
                bestPoint = candidatePoint;
            }
        }

        return bestPoint;
    }

    private static int DistanceToNearestEdge(
        GridManager gridManager,
        Vector2Int point)
    {
        int distanceToEdge = Mathf.Min(
            Mathf.Min(point.x, point.y),
            Mathf.Min(
                gridManager.Columns - 1 - point.x,
                gridManager.Rows - 1 - point.y));

        return Mathf.Max(distanceToEdge, 0);
    }

    // ── Métricas ────────────────────────────────────────────────────────

    private static void RecordDecision(bool holding, int playerFreedom)
    {
#if UNITY_EDITOR
        EnemyNavigationDebug.RecordDecision(
            nameof(AmbushBehavior),
            0,
            usedFallback: playerFreedom < 0,
            usedMistake: false,
            intercepted: false,
            playerFreedom: playerFreedom,
            holding: holding);
#endif
    }
}
