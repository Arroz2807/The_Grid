using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elige una dirección al azar, pero sólo entre las que dejan espacio real
/// para seguir moviéndose.
///
/// Antes el filtro era HasAnyExit: una mirada de un paso que preguntaba
/// "¿la celda siguiente tiene alguna salida?". Eso deja pasar el caso que
/// más lo mataba: un pasillo de ancho uno, donde cada celda tiene
/// exactamente una salida (seguir recto) y el ciclo no puede girar 180°
/// para volver. El filtro decía "seguro" hasta el fondo del pasillo.
///
/// Ahora cada dirección se evalúa con EscapeAnalyzer, que recorre el
/// espacio de estados (celda, dirección) respetando la regla de no girar
/// 180°. Una dirección es viable si desde la celda de destino se pueden
/// alcanzar al menos minRoomCells celdas dentro del horizonte.
///
/// La identidad se conserva: nunca mira al jugador, no ordena por "mejor"
/// y elige uniformemente entre las viables. La inteligencia es un filtro
/// de supervivencia, no una estrategia de persecución. Además, con
/// probabilidad mistakeChance ignora el filtro y elige al azar entre todas
/// las direcciones válidas: sigue pudiendo ser engañado.
/// </summary>
public class RandomBehavior : IEnemyBehavior
{
    private readonly List<Vector2Int> survivors = new List<Vector2Int>(4);

    public Vector2Int ChooseDirection(EnemyDecisionContext context)
    {
        List<Vector2Int> valid = context.ValidDirections;

        if (valid == null || valid.Count == 0)
            return Vector2Int.zero;

        EnemyNavigationConfig config = context.Navigation;

        // Sin configuración asignada se mantiene el comportamiento anterior
        // (filtro de un paso). Así el asset es opcional y no rompe nada.
        if (config == null)
            return PickWithSingleStepFilter(context, valid);

        int horizon = Mathf.Max(1, config.escapeHorizonSteps);
        int minRoom = Mathf.Max(1, config.minRoomCells);

        survivors.Clear();

        Vector2Int bestDirection = valid[0];
        int bestRoom = -1;

        for (int i = 0; i < valid.Count; i++)
        {
            Vector2Int direction = valid[i];
            Vector2Int nextCell = context.Self.CurrentCell + direction;

            EscapeResult escape = EscapeAnalyzer.Analyze(
                context.GridManager,
                nextCell,
                direction,
                horizon);

            if (escape.ReachableCells > bestRoom)
            {
                bestRoom = escape.ReachableCells;
                bestDirection = direction;
            }

            if (escape.ReachableCells >= minRoom)
                survivors.Add(direction);
        }

        Vector2Int chosen;

        if (survivors.Count == 0)
        {
            // Ninguna dirección es realmente segura: en vez de sortear a
            // ciegas, se toma la que deja más espacio. Es la diferencia
            // entre "suicida" y "sin opciones".
            chosen = bestDirection;
            RecordDecision(bestRoom, usedFallback: true, usedMistake: false);
        }
        else if (Random.value < config.mistakeChance)
        {
            // Error deliberado: mantiene el comportamiento impredecible y
            // permite que el jugador lo tienda a una trampa.
            chosen = GridDirectionUtils.PickRandomDirection(valid);
            RecordDecision(bestRoom, usedFallback: false, usedMistake: true);
        }
        else
        {
            chosen = GridDirectionUtils.PickRandomDirection(survivors);
            RecordDecision(bestRoom, usedFallback: false, usedMistake: false);
        }

        return chosen;
    }

    /// <summary>
    /// Alimenta el panel de métricas de editor. En builds no hace nada.
    /// </summary>
    private static void RecordDecision(int bestRoom, bool usedFallback, bool usedMistake)
    {
#if UNITY_EDITOR
        EnemyNavigationDebug.RecordDecision(
            nameof(RandomBehavior),
            bestRoom,
            usedFallback,
            usedMistake);
#endif
    }

    /// <summary>
    /// Comportamiento previo al análisis de escape: descarta direcciones que
    /// llevan a una celda sin salida inmediata y sortea entre el resto.
    /// Sólo se usa si no hay EnemyNavigationConfig asignado.
    /// </summary>
    private static Vector2Int PickWithSingleStepFilter(
        EnemyDecisionContext context,
        List<Vector2Int> valid)
    {
        List<Vector2Int> safeDirections = new List<Vector2Int>();

        foreach (Vector2Int direction in valid)
        {
            Vector2Int nextCell = context.Self.CurrentCell + direction;

            if (GridDirectionUtils.HasAnyExit(context.GridManager, nextCell, direction))
                safeDirections.Add(direction);
        }

        List<Vector2Int> candidates =
            safeDirections.Count > 0 ? safeDirections : valid;

        return GridDirectionUtils.PickRandomDirection(candidates);
    }
}
