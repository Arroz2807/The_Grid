using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elige una dirección al azar entre las opciones válidas, descartando
/// primero las que llevarían a un callejón sin salida en el paso
/// siguiente (ver GridDirectionUtils.HasAnyExit).
/// </summary>
public class RandomBehavior : IEnemyBehavior
{
    public Vector2Int ChooseDirection(EnemyDecisionContext context)
    {
        List<Vector2Int> safeDirections = new List<Vector2Int>();

        foreach (Vector2Int direction in context.ValidDirections)
        {
            Vector2Int nextCell = context.Self.CurrentCell + direction;
            if (GridDirectionUtils.HasAnyExit(context.GridManager, nextCell, direction))
            {
                safeDirections.Add(direction);
            }
        }

        List<Vector2Int> candidates = safeDirections.Count > 0 ? safeDirections : context.ValidDirections;
        return GridDirectionUtils.PickRandomDirection(candidates);
    }
}