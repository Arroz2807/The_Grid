/// <summary>
/// Persigue directamente la posición actual del objetivo, sin anticipar
/// nada — reacciona a lo que ve ahora, no a hacia dónde va el objetivo.
/// Con el rastro, es el más arriesgado de los cuatro: sólo lo apaga
/// cuando YA está en un aprieto real.
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

    public bool ShouldToggleTrail(EnemyDecisionContext context)
    {
        bool isTight = context.ValidDirections.Count <= 1;

        // En aprietos y con rastro prendido: lo apaga para poder escapar
        // sin sumarse más obstáculos a sí mismo. Ya a salvo y todavía
        // fantasma: lo vuelve a prender — no se queda invisible más de lo
        // necesario para salir del apuro.
        if (isTight && context.Self.TrailEnabled) return true;
        if (!isTight && !context.Self.TrailEnabled) return true;

        return false;
    }
}