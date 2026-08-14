using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Matemática de direcciones compartida por EnemyBrain y por cualquier
/// IEnemyBehavior. Antes la rotación estaba duplicada en
/// LightCycleController y en EnemyBrain por separado — con varios
/// comportamientos de IA necesitando la misma lógica (rotar, evaluar
/// vecinos, elegir la opción más cercana a un punto), seguir
/// duplicándola dejaba de tener sentido.
/// </summary>
public static class GridDirectionUtils
{
    public static Vector2Int RotateLeft(Vector2Int dir) => new Vector2Int(-dir.y, dir.x);
    public static Vector2Int RotateRight(Vector2Int dir) => new Vector2Int(dir.y, -dir.x);

    /// <summary>
    /// Las tres direcciones candidatas desde una dirección actual: seguir
    /// derecho, girar a la izquierda, girar a la derecha. El reverso
    /// nunca se incluye — así ningún consumidor de este método puede, por
    /// accidente, proponer un giro de 180°.
    /// </summary>
    public static Vector2Int[] GetCandidateDirections(Vector2Int current)
    {
        return new[] { current, RotateLeft(current), RotateRight(current) };
    }

    /// <summary>
    /// True si, desde "cell" mirando hacia "facing", existe al menos una
    /// dirección (derecho/izquierda/derecha) que lleve a una celda libre.
    /// Es una mirada de UN solo paso hacia adelante — no es pathfinding
    /// real, pero alcanza para detectar "esto es un callejón sin salida
    /// inmediato" sin el costo de un algoritmo de caminos completo.
    /// </summary>
    public static bool HasAnyExit(GridManager gridManager, Vector2Int cell, Vector2Int facing)
    {
        foreach (Vector2Int direction in GetCandidateDirections(facing))
        {
            Vector2Int candidate = cell + direction;
            if (gridManager.IsInsideGrid(candidate) && !gridManager.IsCellOccupied(candidate))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// De una lista de direcciones candidatas, devuelve la que deja a
    /// quien la use más cerca (distancia Manhattan) de targetCell. En caso
    /// de empate, se queda con la primera encontrada — como
    /// GetCandidateDirections siempre ordena [derecho, izquierda,
    /// derecha], esto favorece levemente seguir derecho antes que girar
    /// sin necesidad, reduciendo zigzagueos innecesarios sin tener que
    /// programarlo aparte.
    /// </summary>
    public static Vector2Int PickClosestDirection(Vector2Int fromCell, List<Vector2Int> candidates, Vector2Int targetCell)
    {
        Vector2Int best = candidates[0];
        int bestDistance = int.MaxValue;

        foreach (Vector2Int direction in candidates)
        {
            int distance = ManhattanDistance(fromCell + direction, targetCell);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = direction;
            }
        }

        return best;
    }

    public static Vector2Int PickRandomDirection(List<Vector2Int> candidates)
    {
        return candidates[Random.Range(0, candidates.Count)];
    }

    public static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}