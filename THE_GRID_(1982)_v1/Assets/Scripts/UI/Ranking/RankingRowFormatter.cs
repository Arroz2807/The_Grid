using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reglas de presentación de una fila del ranking, compartidas por la escena
/// Ranking (RankingSceneController) y por el segundo monitor
/// (RankingBoardView). Es estática y sin estado: recibe datos y devuelve el
/// texto o el color. No lee RankingManager ni toca ningún componente de UI,
/// así que el formato queda definido en un único lugar.
/// </summary>
public static class RankingRowFormatter
{
    /// <summary>
    /// Texto de la fila 'index' (0-based). Si no hay entrada para esa
    /// posición devuelve la fila vacía con guiones.
    /// </summary>
    public static string FormatRow(int index, IReadOnlyList<RankingEntry> entries)
    {
        if (index < entries.Count)
        {
            RankingEntry e = entries[index];
            return $"{(index + 1):D2}   {e.playerName}   {e.score:D6}   W{e.wave}";
        }

        return $"{(index + 1):D2}   ---   ------   --";
    }

    /// <summary>
    /// Color de la fila 'index' (0-based). Una fila vacía siempre usa el color
    /// de vacío. Entre las filas con datos, el resaltado de la última
    /// puntuación tiene prioridad sobre los colores de oro, plata y bronce.
    /// </summary>
    public static Color GetColor(int index, bool hasEntry, bool isHighlight, RankingPalette palette)
    {
        if (!hasEntry) return palette.Empty;
        if (isHighlight) return palette.Highlight;
        if (index == 0) return palette.Gold;
        if (index == 1) return palette.Silver;
        if (index == 2) return palette.Bronze;
        return palette.Normal;
    }
}