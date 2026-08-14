/// <summary>
/// No apunta a dónde está el objetivo, sino a dónde va a estar en unas
/// pocas celdas si sigue en línea recta: posición actual + dirección
/// actual × LookaheadCells. El resultado es que este enemigo tiende a
/// cortar camino en vez de perseguir por detrás, especialmente notorio
/// cuando el objetivo viaja en línea recta.
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