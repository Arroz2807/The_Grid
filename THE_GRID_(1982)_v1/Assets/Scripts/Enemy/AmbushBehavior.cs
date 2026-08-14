/// <summary>
/// Misma idea que PredictBehavior, pero mirando bastante más lejos por
/// delante del objetivo — busca llegar ANTES a un punto de su camino
/// futuro, no perseguirlo por detrás. Es una aproximación deliberadamente
/// simple a "cortar camino": no calcula el mejor punto de intercepción
/// real contra el trayecto probable del objetivo (eso requeriría
/// pathfinding), sólo apunta más adelante en su trayectoria actual que
/// PredictBehavior. Suficiente para que se note un comportamiento
/// distinto; un "cornering" más preciso queda como refinamiento futuro,
/// sin que eso afecte a ninguna otra parte del sistema.
/// </summary>
public class AmbushBehavior : IEnemyBehavior
{
    private const int LookaheadCells = 6;

    public UnityEngine.Vector2Int ChooseDirection(EnemyDecisionContext context)
    {
        if (context.Target == null)
        {
            return GridDirectionUtils.PickRandomDirection(context.ValidDirections);
        }

        UnityEngine.Vector2Int aheadCell = context.Target.CurrentCell + context.Target.Direction * LookaheadCells;
        return GridDirectionUtils.PickClosestDirection(context.Self.CurrentCell, context.ValidDirections, aheadCell);
    }
}