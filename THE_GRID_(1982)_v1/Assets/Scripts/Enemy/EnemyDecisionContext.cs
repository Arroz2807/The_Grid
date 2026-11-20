using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Todo lo que un IEnemyBehavior necesita para decidir: quién es (self),
/// entre qué direcciones puede elegir (ya filtradas como válidas por
/// EnemyBrain), a quién persigue (target, puede ser null si no tiene
/// objetivo vivo), una referencia a la grilla para consultas propias y los
/// parámetros de percepción del tablero.
///
/// Agrupar esto en una sola estructura, en vez de parámetros sueltos,
/// permite agregar campos sin romper la firma de ChooseDirection en las
/// implementaciones ya existentes. Navigation se agregó exactamente así:
/// el constructor de cuatro argumentos sigue existiendo y equivale a "sin
/// configuración de navegación".
/// </summary>
public readonly struct EnemyDecisionContext
{
    public readonly LightCycleController Self;
    public readonly List<Vector2Int> ValidDirections;
    public readonly LightCycleController Target;
    public readonly GridManager GridManager;

    /// <summary>
    /// Parámetros compartidos de percepción (horizonte, umbrales). Puede ser
    /// null: en ese caso los comportamientos deben caer a su lógica mínima.
    /// </summary>
    public readonly EnemyNavigationConfig Navigation;

    public EnemyDecisionContext(
        LightCycleController self,
        List<Vector2Int> validDirections,
        LightCycleController target,
        GridManager gridManager)
        : this(self, validDirections, target, gridManager, null)
    {
    }

    public EnemyDecisionContext(
        LightCycleController self,
        List<Vector2Int> validDirections,
        LightCycleController target,
        GridManager gridManager,
        EnemyNavigationConfig navigation)
    {
        Self = self;
        ValidDirections = validDirections;
        Target = target;
        GridManager = gridManager;
        Navigation = navigation;
    }
}
