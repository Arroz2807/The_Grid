using System.Collections.Generic;

/// <summary>
/// Elige una dirección al azar entre las opciones válidas — pero primero
/// descarta las que llevarían a un callejón sin salida en el paso
/// siguiente (ver GridDirectionUtils.HasAnyExit). Sigue siendo el
/// comportamiento menos informado de los cuatro (no usa el objetivo en
/// absoluto), pero ya no elige, por pura casualidad, opciones que se
/// autodestruyen un paso después.
/// </summary>
public class RandomBehavior : IEnemyBehavior
{
    public UnityEngine.Vector2Int ChooseDirection(EnemyDecisionContext context)
    {
        List<UnityEngine.Vector2Int> safeDirections = new List<UnityEngine.Vector2Int>();

        foreach (UnityEngine.Vector2Int direction in context.ValidDirections)
        {
            UnityEngine.Vector2Int nextCell = context.Self.CurrentCell + direction;
            if (GridDirectionUtils.HasAnyExit(context.GridManager, nextCell, direction))
            {
                safeDirections.Add(direction);
            }
        }

        // Si NINGUNA opción tiene salida al paso siguiente, no hay nada
        // "seguro" entre qué elegir — usamos las válidas de siempre, total
        // el callejón es inevitable de cualquier forma.
        List<UnityEngine.Vector2Int> candidates = safeDirections.Count > 0 ? safeDirections : context.ValidDirections;

        return GridDirectionUtils.PickRandomDirection(candidates);
    }
}