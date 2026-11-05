/// <summary>
/// Define la ESTRATEGIA de un enemigo: dado un EnemyDecisionContext,
/// elige una dirección entre las ya confirmadas como válidas. Ninguna
/// implementación necesita volver a chequear paredes, rastros u
/// ocupación por su cuenta — ese filtro es responsabilidad exclusiva de
/// EnemyBrain.
/// </summary>
public interface IEnemyBehavior
{
    UnityEngine.Vector2Int ChooseDirection(EnemyDecisionContext context);
}