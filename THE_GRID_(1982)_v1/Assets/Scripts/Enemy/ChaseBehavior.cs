/// <summary>
/// Persigue directamente la posición actual del objetivo: en cada
/// decisión, elige la dirección que más acorta la distancia Manhattan
/// hasta donde el objetivo está parado AHORA. No anticipa nada — es el
/// más simple de los tres comportamientos "informados".
/// </summary>
public class ChaseBehavior : IEnemyBehavior
{
    public UnityEngine.Vector2Int ChooseDirection(EnemyDecisionContext context)
    {
        if (context.Target == null)
        {
            return GridDirectionUtils.PickRandomDirection(context.ValidDirections);
        }

        return GridDirectionUtils.PickClosestDirection(context.Self.CurrentCell, context.ValidDirections, context.Target.CurrentCell);
    }
}