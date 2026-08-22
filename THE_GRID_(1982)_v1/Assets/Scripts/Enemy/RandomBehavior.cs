using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elige una dirección al azar entre las opciones válidas, descartando
/// primero las que llevarían a un callejón sin salida en el paso
/// siguiente (ver GridDirectionUtils.HasAnyExit). Para el rastro, es la
/// única que mantiene una decisión genuinamente aleatoria a propósito —
/// los otros tres deciden con criterio, éste no.
/// </summary>
public class RandomBehavior : IEnemyBehavior
{
    private const float TrailToggleChance = 0.08f;

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

    public bool ShouldToggleTrail(EnemyDecisionContext context)
    {
        return Random.value < TrailToggleChance;
    }
}