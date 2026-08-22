/// <summary>
/// Apunta a dónde va a estar el objetivo si sigue en línea recta
/// (posición + dirección actual × LookaheadCells), no a dónde está ahora.
/// Con el rastro, aplica la misma idea de anticipación a su propia
/// seguridad: se apaga preventivamente ANTES de quedar en un aprieto, no
/// después — a diferencia de Chase, que reacciona recién cuando ya está
/// complicado.
/// </summary>
public class PredictBehavior : IEnemyBehavior
{
    private const int LookaheadCells = 3;

    public UnityEngine.Vector2Int ChooseDirection(EnemyDecisionContext context)
    {
        if (context.Target == null)
        {
            return GridDirectionUtils.PickRandomDirection(context.ValidDirections);
        }

        UnityEngine.Vector2Int predictedCell = context.Target.CurrentCell + context.Target.Direction * LookaheadCells;
        return GridDirectionUtils.PickClosestDirection(context.Self.CurrentCell, context.ValidDirections, predictedCell);
    }

    public bool ShouldToggleTrail(EnemyDecisionContext context)
    {
        UnityEngine.Vector2Int direction = context.Self.Direction;
        UnityEngine.Vector2Int nextCell = context.Self.CurrentCell + direction;

        // ¿La celda a la que voy a entrar, a su vez, no tiene ninguna
        // salida clara? Si es así, es momento de apagar el rastro AHORA,
        // un paso antes de necesitarlo de verdad.
        bool nextCellIsRisky = !GridDirectionUtils.HasAnyExit(context.GridManager, nextCell, direction);

        if (nextCellIsRisky && context.Self.TrailEnabled) return true;
        if (!nextCellIsRisky && !context.Self.TrailEnabled) return true;

        return false;
    }
}