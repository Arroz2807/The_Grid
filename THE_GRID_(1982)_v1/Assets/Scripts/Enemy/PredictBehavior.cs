/// <summary>
/// Apunta a dónde va a estar el objetivo en unas pocas celdas si sigue en
/// línea recta: posición actual + dirección actual × LookaheadCells.
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
}