using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lógica de detección y evaluación de power-ups para la IA. Separada de
/// EnemyBrain para que EnemyBrain no crezca en responsabilidades: esta
/// clase sabe "qué power-ups hay y cuáles valen la pena"; EnemyBrain sabe
/// "cómo moverse y cuándo consultar al comportamiento". Se comunica sólo
/// a través de PowerUpSpawner.ActivePickups (acceso de sólo lectura).
/// </summary>
public class PowerUpSeeker
{
    private readonly GridManager gridManager;
    private readonly PowerUpAIConfig config;
    private readonly IReadOnlyDictionary<Vector2Int, PowerUpPickup> activePickups;

    public PowerUpSeeker(
        GridManager gridManager,
        PowerUpAIConfig config,
        IReadOnlyDictionary<Vector2Int, PowerUpPickup> activePickups)
    {
        this.gridManager = gridManager;
        this.config = config;
        this.activePickups = activePickups;
    }

    /// <summary>
    /// Evalúa si hay algún power-up que valga la pena perseguir desde la
    /// posición y dirección actuales. Devuelve la dirección hacia el mejor
    /// candidato, o null si no hay ninguno de interés o la IA decide no
    /// buscarlo esta vez.
    /// </summary>
    public Vector2Int? GetDirectionTowardsBestPickup(
        Vector2Int currentCell,
        Vector2Int currentDirection,
        List<Vector2Int> validDirections)
    {
        if (!config.seekPowerUps) return null;
        if (validDirections.Count == 0) return null;
        if (UnityEngine.Random.value > config.seekProbability) return null;

        PowerUpPickup best = FindBestPickup(currentCell, currentDirection, validDirections);
        if (best == null) return null;

        return DirectionTowards(currentCell, best.Cell, validDirections);
    }

    private PowerUpPickup FindBestPickup(
        Vector2Int currentCell,
        Vector2Int currentDirection,
        List<Vector2Int> validDirections)
    {
        PowerUpPickup bestPickup = null;
        int bestScore = int.MinValue;

        foreach (var kvp in activePickups)
        {
            PowerUpPickup pickup = kvp.Value;
            if (pickup == null) continue;

            int dist = ManhattanDistance(currentCell, pickup.Cell);
            if (dist > config.maxInterestDistance) continue;

            // Verificar que existe una ruta sin atravesar obstáculos.
            if (!HasReachablePath(currentCell, pickup.Cell)) continue;

            // Verificar que la primera dirección hacia el pickup no sea
            // un callejón sin salida (si la config lo requiere).
            if (config.avoidDeadEndRoutes)
            {
                Vector2Int? firstDir = DirectionTowards(currentCell, pickup.Cell, validDirections);
                if (firstDir.HasValue)
                {
                    Vector2Int nextCell = currentCell + firstDir.Value;
                    if (!GridDirectionUtils.HasAnyExit(gridManager, nextCell, firstDir.Value))
                        continue;
                }
            }

            int priority = GetPriority(pickup.Definition.type);
            // Score: prioridad del tipo × 10 menos la distancia. Así un
            // pickup prioritario cercano siempre gana a uno lejano de
            // baja prioridad.
            int score = priority * 10 - dist;

            if (score > bestScore)
            {
                bestScore = score;
                bestPickup = pickup;
            }
        }

        return bestPickup;
    }

    private int GetPriority(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.Turbo: return config.turboPriority;
            case PowerUpType.Shield: return config.shieldPriority;
            case PowerUpType.TrailExtension: return config.trailPriority;
            default: return 0;
        }
    }

    /// <summary>
    /// BFS superficial hasta config.pathCheckDepth celdas. Devuelve true
    /// si existe al menos un camino libre hacia el objetivo sin atravesar
    /// celdas ocupadas. No garantiza el camino óptimo — sólo confirma que
    /// el pickup es alcanzable desde la posición actual.
    /// </summary>
    private bool HasReachablePath(Vector2Int from, Vector2Int to)
    {
        if (from == to) return true;

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(from);
        visited.Add(from);

        int depth = 0;
        int countAtCurrentDepth = 1;
        int countAtNextDepth = 0;

        while (queue.Count > 0 && depth < config.pathCheckDepth)
        {
            Vector2Int current = queue.Dequeue();
            countAtCurrentDepth--;

            Vector2Int[] neighbors =
            {
                current + Vector2Int.up,
                current + Vector2Int.down,
                current + Vector2Int.left,
                current + Vector2Int.right,
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (!gridManager.IsInsideGrid(neighbor)) continue;
                if (visited.Contains(neighbor)) continue;
                if (neighbor == to) return true;
                if (gridManager.IsCellOccupied(neighbor)) continue;

                visited.Add(neighbor);
                queue.Enqueue(neighbor);
                countAtNextDepth++;
            }

            if (countAtCurrentDepth == 0)
            {
                depth++;
                countAtCurrentDepth = countAtNextDepth;
                countAtNextDepth = 0;
            }
        }

        return false;
    }

    private Vector2Int? DirectionTowards(
        Vector2Int from,
        Vector2Int to,
        List<Vector2Int> validDirections)
    {
        Vector2Int? best = null;
        int bestDist = int.MaxValue;

        foreach (Vector2Int dir in validDirections)
        {
            int dist = ManhattanDistance(from + dir, to);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = dir;
            }
        }

        return best;
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}