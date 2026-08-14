using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Todo lo que un IEnemyBehavior necesita para decidir: quién es (self),
/// entre qué direcciones puede elegir (ya filtradas como válidas por
/// EnemyBrain), a quién persigue (target, puede ser null si no tiene
/// objetivo vivo) y una referencia a la grilla para consultas propias
/// (por ejemplo, Random mirando un paso adelante). Agrupar esto en una
/// sola estructura, en vez de parámetros sueltos, permite agregarle
/// campos nuevos en el futuro sin romper la firma de ChooseDirection en
/// las implementaciones ya existentes.
/// </summary>
public readonly struct EnemyDecisionContext
{
    public readonly LightCycleController Self;
    public readonly List<Vector2Int> ValidDirections;
    public readonly LightCycleController Target;
    public readonly GridManager GridManager;

    public EnemyDecisionContext(LightCycleController self, List<Vector2Int> validDirections, LightCycleController target, GridManager gridManager)
    {
        Self = self;
        ValidDirections = validDirections;
        Target = target;
        GridManager = gridManager;
    }
}