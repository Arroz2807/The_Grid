using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A diferencia de Predict (que apunta a UN punto fijo por delante del
/// objetivo), evalúa varios puntos a lo largo de su trayectoria actual y
/// elige el más cercano a un borde de la grilla — intentando empujarlo
/// contra un límite del tablero en vez de simplemente adelantársele en
/// espacio abierto. Por ahora sólo considera los bordes de la grilla, no
/// el rastro existente — extenderlo a "distancia al rastro más cercano"
/// es un refinamiento futuro, más costoso de calcular, que no rompería
/// nada de esta clase si se agrega después.
///
/// Con el rastro, tiene una estrategia de dos fases atada a la distancia
/// al objetivo: lejos, fantasmea para reposicionarse rápido; cerca,
/// enciende el rastro para empezar a dejar obstáculos donde importan. Los
/// dos umbrales (no uno solo) evitan que, orbitando justo en el límite,
/// el rastro parpadee encendiéndose y apagándose en cada celda.
/// </summary>
public class AmbushBehavior : IEnemyBehavior
{
    private const int MinLookahead = 2;
    private const int MaxLookahead = 8;
    private const int LookaheadStep = 2;

    private const int GhostDistanceThreshold = 6;
    private const int SolidDistanceThreshold = 3;

    public Vector2Int ChooseDirection(EnemyDecisionContext context)
    {
        if (context.Target == null)
        {
            return GridDirectionUtils.PickRandomDirection(context.ValidDirections);
        }

        Vector2Int aimCell = FindBestCorneringPoint(context);
        return GridDirectionUtils.PickClosestDirection(context.Self.CurrentCell, context.ValidDirections, aimCell);
    }

    public bool ShouldToggleTrail(EnemyDecisionContext context)
    {
        if (context.Target == null) return false;

        int distance = GridDirectionUtils.ManhattanDistance(context.Self.CurrentCell, context.Target.CurrentCell);

        if (distance > GhostDistanceThreshold && context.Self.TrailEnabled) return true;
        if (distance < SolidDistanceThreshold && !context.Self.TrailEnabled) return true;

        return false;
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

    // Qué tan lejos está "point" del borde más cercano de la grilla — una
    // aproximación simple (no exacta) para preferir puntos "contra la
    // pared" por sobre puntos en medio del espacio abierto.
    private static int DistanceToNearestEdge(GridManager gridManager, Vector2Int point)
    {
        int distanceToEdge = Mathf.Min(
            Mathf.Min(point.x, point.y),
            Mathf.Min(gridManager.Columns - 1 - point.x, gridManager.Rows - 1 - point.y));

        return Mathf.Max(distanceToEdge, 0);
    }
}