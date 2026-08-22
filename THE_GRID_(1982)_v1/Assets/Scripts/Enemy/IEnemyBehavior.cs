/// <summary>
/// Define la ESTRATEGIA de un enemigo: dado un EnemyDecisionContext,
/// decide hacia dónde moverse y si conviene alternar su rastro. Ninguna
/// implementación necesita volver a chequear paredes, rastros u
/// ocupación por su cuenta — ese filtro es responsabilidad exclusiva de
/// EnemyBrain.
/// </summary>
public interface IEnemyBehavior
{
    UnityEngine.Vector2Int ChooseDirection(EnemyDecisionContext context);

    /// <summary>
    /// True si en este momento conviene PEDIR que se alterne el estado
    /// del rastro (prendido→apagado o viceversa — LightCycleController
    /// invierte lo que ya tiene, no fija un valor absoluto). Cada
    /// implementación debe consultar context.Self.TrailEnabled para saber
    /// en qué estado está antes de decidir si pedir el cambio tiene
    /// sentido.
    /// </summary>
    bool ShouldToggleTrail(EnemyDecisionContext context);
}