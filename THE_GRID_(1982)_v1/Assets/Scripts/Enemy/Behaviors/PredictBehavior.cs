using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Anticipa el movimiento probable del objetivo y busca un punto de
/// intercepción, en vez de perseguir su posición actual como Chase.
///
/// La predicción es deliberadamente simple y falible: se proyecta al
/// jugador en línea recta (celda actual + dirección actual × i). No se
/// consulta ningún estado futuro ni todo el mapa; si el jugador dobla, la
/// predicción falla. Eso mantiene a Predict engañable.
///
/// Para cada punto proyectado se compara el tiempo de llegada del enemigo
/// (medido con un campo de distancias en espacio de estados, que respeta
/// la regla de no girar 180°) contra el del jugador (i pasos). Sólo se
/// acepta un punto si el enemigo llega con la ventaja mínima pedida por
/// interceptTolerance. El primer punto que cumple es el objetivo: es el
/// más cercano al jugador y el que menos tiempo le da para reaccionar.
///
/// Si no hay ninguna intercepción posible, no se rinde: apunta al punto
/// proyectado más lejano que sí puede alcanzar, es decir, sigue cortando
/// camino en lugar de perseguir a la cola. Y si el paso elegido sería
/// suicida (pasillo sin salida), EnemyNavigationSafety lo corrige.
/// </summary>
public class PredictBehavior : IEnemyBehavior
{
    // Ventana del comportamiento anterior, usada sólo cuando no hay
    // EnemyNavigationConfig asignado.
    private const int LegacyLookaheadCells = 3;

    public Vector2Int ChooseDirection(EnemyDecisionContext context)
    {
        List<Vector2Int> valid = context.ValidDirections;

        if (valid == null || valid.Count == 0)
            return Vector2Int.zero;

        if (context.Target == null)
            return GridDirectionUtils.PickRandomDirection(valid);

        EnemyNavigationConfig config = context.Navigation;

        if (config == null)
            return ChooseLegacy(context, valid);

        GridManager grid = context.GridManager;
        Vector2Int selfCell = context.Self.CurrentCell;
        Vector2Int playerCell = context.Target.CurrentCell;
        Vector2Int playerDirection = context.Target.Direction;

        int horizon = Mathf.Max(1, config.interceptHorizon);
        int tolerance = Mathf.Max(0, config.interceptTolerance);
        int fieldHorizon = EstimateFieldHorizon(context, horizon, tolerance);

        if (!EscapeAnalyzer.TryBuildDistanceField(
                grid, selfCell, context.Self.Direction, fieldHorizon,
                out EscapeDistanceField field))
        {
            return EnemyNavigationSafety.Select(
                context,
                Vector2Int.zero,
                playerCell,
                Mathf.Max(1, config.escapeHorizonSteps),
                Mathf.Max(1, config.minRoomCells));
        }

        bool foundIntercept = false;
        Vector2Int interceptCell = playerCell;
        Vector2Int interceptDirection = Vector2Int.zero;

        Vector2Int fallbackCell = playerCell;
        Vector2Int fallbackDirection = Vector2Int.zero;
        bool hasFallback = false;

        // Proyección del jugador: una celda por paso, siempre derecho.
        for (int i = 1; i <= horizon; i++)
        {
            Vector2Int projectedCell = playerCell + playerDirection * i;

            // Si el jugador no puede seguir derecho (borde o rastro), su
            // trayectoria termina ahí. Predecir más allá sería inventar.
            if (!grid.IsInsideGrid(projectedCell) ||
                grid.IsCellOccupied(projectedCell))
                break;

            if (!field.TryGetSteps(projectedCell, out int enemySteps))
            {
                // Este punto no es alcanzable dentro del horizonte, pero
                // uno posterior podría serlo por otro camino.
                continue;
            }

            fallbackCell = projectedCell;
            hasFallback = true;

            if (field.TryGetFirstStep(projectedCell, out Vector2Int firstStep))
                fallbackDirection = firstStep;

            if (IsArrivingInTime(context, enemySteps, i, tolerance))
            {
                foundIntercept = true;
                interceptCell = projectedCell;

                if (field.TryGetFirstStep(projectedCell, out Vector2Int direction))
                    interceptDirection = direction;

                // Primer punto alcanzable a tiempo: el más cercano al
                // jugador, el que menos margen le deja para girar.
                break;
            }
        }

        Vector2Int aimCell;
        Vector2Int desiredDirection;

        if (foundIntercept)
        {
            aimCell = interceptCell;
            desiredDirection = interceptDirection;
        }
        else if (hasFallback)
        {
            aimCell = fallbackCell;
            desiredDirection = fallbackDirection;
        }
        else
        {
            // Ni siquiera se puede alcanzar la trayectoria proyectada:
            // acercarse al jugador es la única opción con información.
            aimCell = playerCell;
            desiredDirection = Vector2Int.zero;
        }

        Vector2Int chosen = EnemyNavigationSafety.Select(
            context,
            desiredDirection,
            aimCell,
            Mathf.Max(1, config.escapeHorizonSteps),
            Mathf.Max(1, config.minRoomCells));

        bool committedToIntercept =
            foundIntercept &&
            interceptDirection != Vector2Int.zero &&
            chosen == interceptDirection;

        RecordDecision(committedToIntercept);

        return chosen;
    }

    /// <summary>
    /// Compara tiempos de llegada, no pasos crudos: si el enemigo es más
    /// rápido que el jugador (Waves puede cambiar moveInterval), puede
    /// permitirse más pasos propios en el mismo tiempo. La ventaja mínima
    /// se expresa en pasos del jugador.
    /// </summary>
    private static bool IsArrivingInTime(
        EnemyDecisionContext context,
        int enemySteps,
        int playerSteps,
        int tolerance)
    {
        float enemyInterval = Mathf.Max(0.01f, context.Self.MoveInterval);
        float playerInterval = Mathf.Max(0.01f, context.Target.MoveInterval);

        float enemyArrival = enemySteps * enemyInterval;
        float playerArrival = playerSteps * playerInterval;
        float requiredLead = tolerance * playerInterval;

        // Antes, o a la par: llegar después significa encontrar la celda
        // ocupada por el jugador o su rastro, es decir, morir.
        return enemyArrival + requiredLead <= playerArrival;
    }

    /// <summary>
    /// Cuántos pasos propios entran en el tiempo que el jugador tarda en
    /// recorrer interceptHorizon celdas. Se acota al tamaño del tablero
    /// porque el BFS no necesita más profundidad que eso.
    /// </summary>
    private static int EstimateFieldHorizon(
        EnemyDecisionContext context,
        int playerHorizon,
        int tolerance)
    {
        float enemyInterval = Mathf.Max(0.01f, context.Self.MoveInterval);
        float playerInterval = Mathf.Max(0.01f, context.Target.MoveInterval);

        float ratio = playerInterval / enemyInterval;
        int steps = Mathf.CeilToInt(playerHorizon * ratio) + tolerance + 1;

        int cap = Mathf.Max(
            1,
            context.GridManager.Columns * context.GridManager.Rows);

        return Mathf.Clamp(steps, 1, cap);
    }

    /// <summary>
    /// Comportamiento anterior: apuntar a tres celdas adelante en línea
    /// recta y elegir la dirección más cercana. Sólo se usa sin config.
    /// </summary>
    private static Vector2Int ChooseLegacy(
        EnemyDecisionContext context,
        List<Vector2Int> valid)
    {
        Vector2Int predictedCell =
            context.Target.CurrentCell +
            context.Target.Direction * LegacyLookaheadCells;

        return GridDirectionUtils.PickClosestDirection(
            context.Self.CurrentCell, valid, predictedCell);
    }

    private static void RecordDecision(bool intercepted)
    {
#if UNITY_EDITOR
        EnemyNavigationDebug.RecordDecision(
            nameof(PredictBehavior),
            0,
            usedFallback: !intercepted,
            usedMistake: false,
            intercepted: intercepted);
#endif
    }
}
