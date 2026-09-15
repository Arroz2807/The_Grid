using UnityEngine;

/// <summary>
/// Evalúa varios puntos a lo largo de la trayectoria actual del objetivo
/// y elige el más cercano a un borde de la grilla — intentando empujarlo
/// contra un límite del tablero en vez de simplemente adelantársele en
/// espacio abierto.
/// </summary>
public class AmbushBehavior : IEnemyBehavior
{
    private const int MinLookahead = 2;
    private const int MaxLookahead = 8;
    private const int LookaheadStep = 2;

    public Vector2Int ChooseDirection(EnemyDecisionContext context)
    {
        if (context.Target == null)
        {
            return GridDirectionUtils.PickRandomDirection(context.ValidDirections);
        }

        Vector2Int aimCell = FindBestCorneringPoint(context);
        return GridDirectionUtils.PickClosestDirection(context.Self.CurrentCell, context.ValidDirections, aimCell);
    }

    private static Vector2Int FindBestCorneringPoint(EnemyDecisionContext context)
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

    private static int DistanceToNearestEdge(GridManager gridManager, Vector2Int point)
    {
        int distanceToEdge = Mathf.Min(
            Mathf.Min(point.x, point.y),
            Mathf.Min(gridManager.Columns - 1 - point.x, gridManager.Rows - 1 - point.y));

        return Mathf.Max(distanceToEdge, 0);
    }
}