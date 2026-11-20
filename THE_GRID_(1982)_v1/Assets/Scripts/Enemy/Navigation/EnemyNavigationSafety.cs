using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Política compartida de "no meterse en un callejón". Después de que
/// Predict o Ambush eligen a qué celda quieren ir, esta clase decide cuál
/// de las direcciones legales realmente conviene tomar.
///
/// El problema que resuelve es que una estrategia correcta puede exigir un
/// paso suicida: apuntar a una intercepción perfecta entrando a un pasillo
/// de ancho uno, o cerrar una garganta que después no tiene salida. Ninguno
/// de los dos comportamientos debe matarse por cumplir su objetivo.
///
/// La regla es simple, y es la misma que usa Random para sobrevivir:
///   - Si la dirección deseada deja al menos minRoomCells celdas
///     alcanzables dentro del horizonte, se respeta.
///   - Si no, se elige la dirección segura que más acerque al objetivo
///     (Manhattan). Así la estrategia se degrada, no se abandona.
///   - Si ninguna dirección es segura, se elige la que deja más espacio:
///     es el mismo criterio de "rescate" que usa Random.
///
/// Random no usa esta clase porque su objetivo no es una celda: es sortear
/// entre opciones y, con cierta probabilidad, equivocarse a propósito.
/// </summary>
public static class EnemyNavigationSafety
{
    /// <summary>
    /// Devuelve la dirección que el comportamiento debería ejecutar.
    ///
    /// desiredDirection: lo que la estrategia quería hacer.
    /// aimCell: a dónde quería llegar (se usa para desempatar).
    /// horizonSteps / minRoomCells: parámetros de EnemyNavigationConfig.
    /// </summary>
    public static Vector2Int Select(
        EnemyDecisionContext context,
        Vector2Int desiredDirection,
        Vector2Int aimCell,
        int horizonSteps,
        int minRoomCells)
    {
        List<Vector2Int> valid = context.ValidDirections;

        if (valid == null || valid.Count == 0)
            return Vector2Int.zero;

        int safeIndex = -1;
        int safeScore = int.MaxValue;

        int bestRoomIndex = 0;
        int bestRoom = -1;

        for (int i = 0; i < valid.Count; i++)
        {
            Vector2Int direction = valid[i];
            Vector2Int nextCell = context.Self.CurrentCell + direction;

            EscapeResult escape = EscapeAnalyzer.Analyze(
                context.GridManager,
                nextCell,
                direction,
                horizonSteps);

            if (escape.ReachableCells > bestRoom)
            {
                bestRoom = escape.ReachableCells;
                bestRoomIndex = i;
            }

            if (escape.ReachableCells < minRoomCells)
                continue;

            // La dirección deseada gana siempre que sea segura. Si no lo es,
            // se busca la segura que más acerque al punto que la estrategia
            // quería alcanzar.
            int score = direction == desiredDirection
                ? -1
                : 1 + GridDirectionUtils.ManhattanDistance(nextCell, aimCell);

            if (score < safeScore)
            {
                safeScore = score;
                safeIndex = i;
            }
        }

        if (safeIndex >= 0)
            return valid[safeIndex];

        // Ninguna opción es segura: se toma la que deja más espacio. Es la
        // diferencia entre "sin opciones" y "suicida".
        return valid[bestRoomIndex];
    }
}
