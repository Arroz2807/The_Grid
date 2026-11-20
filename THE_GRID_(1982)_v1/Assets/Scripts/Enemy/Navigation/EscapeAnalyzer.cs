using UnityEngine;

/// <summary>
/// Resultado de un análisis de escape: cuánto espacio real le queda a un
/// ciclo si se mueve a una celda con una orientación determinada.
///
/// ReachableCells es la métrica principal: cantidad de celdas distintas a
/// las que puede llegar respetando la regla de "no se puede girar 180°".
/// Un pasillo de ancho uno da un número bajo; una zona abierta da un
/// número alto.
/// </summary>
public readonly struct EscapeResult
{
    public static readonly EscapeResult Empty =
        new EscapeResult(0, 0, -1, false);

    /// <summary>Estados (celda, dirección) distintos alcanzables.</summary>
    public readonly int ReachableStates;

    /// <summary>Celdas distintas alcanzables (incluye la celda inicial).</summary>
    public readonly int ReachableCells;

    /// <summary>
    /// Pasos hasta la primera celda desde la que se puede elegir más de un
    /// movimiento. -1 = no hay ninguna dentro del horizonte.
    /// </summary>
    public readonly int NearestJunctionSteps;

    /// <summary>
    /// True si la búsqueda se cortó por el horizonte (todavía había espacio
    /// por explorar). False = el conjunto de movimientos se agotó antes, es
    /// decir, la zona está cerrada.
    /// </summary>
    public readonly bool Truncated;

    public bool HasJunction => NearestJunctionSteps >= 0;

    public EscapeResult(
        int reachableStates,
        int reachableCells,
        int nearestJunctionSteps,
        bool truncated)
    {
        ReachableStates = reachableStates;
        ReachableCells = reachableCells;
        NearestJunctionSteps = nearestJunctionSteps;
        Truncated = truncated;
    }
}

/// <summary>
/// Vista de sólo lectura sobre un mapa de distancias en espacio de estados
/// producido por <see cref="EscapeAnalyzer.TryBuildDistanceField"/>.
///
/// Es una estructura liviana que apunta a los buffers internos del
/// analizador: no copia nada. Por eso incluye una generación — si el
/// analizador se vuelve a usar, las consultas sobre este campo empiezan a
/// devolver false en lugar de datos de otra búsqueda.
/// </summary>
public readonly struct EscapeDistanceField
{
    private readonly int[] depth;
    private readonly int[] firstStep;
    private readonly int[] stamp;
    private readonly int generation;
    private readonly int columns;
    private readonly int rows;

    internal EscapeDistanceField(
        int[] depth,
        int[] firstStep,
        int[] stamp,
        int generation,
        int columns,
        int rows)
    {
        this.depth = depth;
        this.firstStep = firstStep;
        this.stamp = stamp;
        this.generation = generation;
        this.columns = columns;
        this.rows = rows;
    }

    public bool IsValid => depth != null && generation != 0;

    /// <summary>True si la celda fue alcanzada por esta búsqueda.</summary>
    public bool Contains(Vector2Int cell)
    {
        if (!IsValid || !IsInside(cell))
            return false;

        return stamp[cell.y * columns + cell.x] == generation;
    }

    /// <summary>Pasos mínimos desde el origen hasta la celda.</summary>
    public bool TryGetSteps(Vector2Int cell, out int steps)
    {
        if (!Contains(cell))
        {
            steps = -1;
            return false;
        }

        steps = depth[cell.y * columns + cell.x];
        return true;
    }

    /// <summary>
    /// Primer movimiento de un camino óptimo desde el origen hasta la celda.
    /// Es lo que necesita un comportamiento para "dar el primer paso" sin
    /// reconstruir el camino completo.
    /// </summary>
    public bool TryGetFirstStep(Vector2Int cell, out Vector2Int direction)
    {
        if (!Contains(cell))
        {
            direction = Vector2Int.zero;
            return false;
        }

        int directionIndex = firstStep[cell.y * columns + cell.x];

        if (directionIndex < 0)
        {
            direction = Vector2Int.zero;
            return false;
        }

        direction = EscapeAnalyzer.DirectionFromIndex(directionIndex);
        return true;
    }

    private bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < columns
            && cell.y >= 0 && cell.y < rows;
    }
}

/// <summary>
/// Percepción del tablero para la IA. Responde la pregunta que las
/// decisiones actuales no pueden responder: "si entro acá, ¿me queda
/// espacio para seguir moviéndome?".
///
/// La clave es que el ciclo NO puede girar 180°, así que el estado real de
/// un enemigo no es una celda sino un par (celda, dirección). Desde
/// (c, d) sólo existen tres sucesores: seguir recto, girar izquierda y
/// girar derecha. El BFS de esta clase recorre exactamente ese espacio de
/// estados, así que detecta lo que una mirada de un paso (HasAnyExit) no
/// puede ver: los pasillos de ancho uno, que celda a celda parecen seguros
/// pero no tienen ninguna salida posible.
///
/// Es una percepción LOCAL e IMPERFECTA a propósito: el horizonte
/// (maxSteps) acota cuánto ve. Un pasillo más largo que el horizonte, o un
/// cierre que el jugador produce después, siguen siendo trampas válidas.
///
/// Todo es estático y usa buffers reutilizados: después de la primera
/// llamada no hay allocaciones por decisión. El costo está acotado por
/// columnas * filas * 4 estados (1024 en una grilla de 16x16).
/// </summary>
public static class EscapeAnalyzer
{
    /// <summary>Arriba, derecha, abajo, izquierda — en ese orden.</summary>
    private const int DirectionCount = 4;

    private static readonly Vector2Int[] Neighbours4 =
    {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left,
    };

    private static int[] stateStamp = new int[0];
    private static int[] stateDepth = new int[0];
    private static int[] stateFirstStep = new int[0];
    private static int[] stateQueue = new int[0];

    private static int[] cellStamp = new int[0];
    private static int[] cellDepth = new int[0];
    private static int[] cellFirstStep = new int[0];
    private static int[] cellQueue = new int[0];

    private static int generation;
    private static int cachedCells = -1;

    // ── API pública ──────────────────────────────────────────────────────

    /// <summary>
    /// Analiza el espacio alcanzable desde (startCell, facing) en hasta
    /// maxSteps pasos. startCell es la celda a la que se acaba de entrar y
    /// facing la dirección con la que se entró.
    /// </summary>
    public static EscapeResult Analyze(
        GridManager grid,
        Vector2Int startCell,
        Vector2Int facing,
        int maxSteps,
        Vector2Int? virtualBlocked = null)
    {
        if (!TryExplore(grid, startCell, facing, maxSteps, virtualBlocked,
                        out Exploration exploration))
            return EscapeResult.Empty;

        return new EscapeResult(
            exploration.ReachableStates,
            exploration.ReachableCells,
            exploration.NearestJunctionSteps,
            exploration.Truncated);
    }

    /// <summary>
    /// Construye un mapa de pasos mínimos desde (startCell, facing) hacia
    /// todas las celdas alcanzables dentro del horizonte. Lo usa Predict
    /// para comparar tiempos de llegada.
    /// </summary>
    public static bool TryBuildDistanceField(
        GridManager grid,
        Vector2Int startCell,
        Vector2Int facing,
        int maxSteps,
        out EscapeDistanceField field)
    {
        if (!TryExplore(grid, startCell, facing, maxSteps, null,
                        out Exploration exploration))
        {
            field = default;
            return false;
        }

        field = new EscapeDistanceField(
            cellDepth,
            cellFirstStep,
            cellStamp,
            exploration.Generation,
            grid.Columns,
            grid.Rows);

        return true;
    }

    /// <summary>
    /// Cantidad de celdas libres alcanzables desde origin (sin contar
    /// origin), moviéndose en las cuatro direcciones y tratando
    /// virtualBlocked como si estuviera ocupada.
    ///
    /// Sirve para medir "cuánta libertad le queda al jugador si el enemigo
    /// se para en tal celda", sin modificar la ocupación real del
    /// GridManager. La usa Ambush para elegir gargantas.
    /// </summary>
    public static int CountReachableCells(
        GridManager grid,
        Vector2Int origin,
        int maxCells,
        Vector2Int? virtualBlocked)
    {
        if (grid == null || maxCells <= 0 || !grid.IsInsideGrid(origin))
            return 0;

        if (virtualBlocked.HasValue && virtualBlocked.Value == origin)
            return 0;

        EnsureCapacity(grid);

        int columns = grid.Columns;
        int rows = grid.Rows;
        int gen = NextGeneration();

        int originIndex = origin.y * columns + origin.x;

        int head = 0;
        int tail = 0;
        int reached = 0;

        cellStamp[originIndex] = gen;
        cellQueue[tail++] = originIndex;

        while (head < tail)
        {
            int index = cellQueue[head++];
            int x = index % columns;
            int y = index / columns;

            for (int i = 0; i < Neighbours4.Length; i++)
            {
                int nextX = x + Neighbours4[i].x;
                int nextY = y + Neighbours4[i].y;

                if (nextX < 0 || nextX >= columns || nextY < 0 || nextY >= rows)
                    continue;

                Vector2Int nextCell = new Vector2Int(nextX, nextY);

                if (virtualBlocked.HasValue && virtualBlocked.Value == nextCell)
                    continue;

                if (grid.IsCellOccupied(nextCell))
                    continue;

                int nextIndex = nextY * columns + nextX;

                if (cellStamp[nextIndex] == gen)
                    continue;

                cellStamp[nextIndex] = gen;
                cellQueue[tail++] = nextIndex;
                reached++;

                if (reached >= maxCells)
                    return reached;
            }
        }

        return reached;
    }

    // ── Conversión de direcciones ───────────────────────────────────────

    internal static Vector2Int DirectionFromIndex(int index)
    {
        switch (index)
        {
            case 0: return Vector2Int.up;
            case 1: return Vector2Int.right;
            case 2: return Vector2Int.down;
            default: return Vector2Int.left;
        }
    }

    private static int DirectionIndex(Vector2Int direction)
    {
        if (direction.x == 0)
        {
            if (direction.y > 0) return 0;
            if (direction.y < 0) return 2;
            return -1;
        }

        if (direction.y != 0)
            return -1;

        return direction.x > 0 ? 1 : 3;
    }

    // ── Núcleo del BFS ──────────────────────────────────────────────────

    private struct Exploration
    {
        public int Generation;
        public int ReachableStates;
        public int ReachableCells;
        public int NearestJunctionSteps;
        public bool Truncated;

        public Exploration(
            int generation,
            int reachableStates,
            int reachableCells,
            int nearestJunctionSteps,
            bool truncated)
        {
            Generation = generation;
            ReachableStates = reachableStates;
            ReachableCells = reachableCells;
            NearestJunctionSteps = nearestJunctionSteps;
            Truncated = truncated;
        }
    }

    private static bool TryExplore(
        GridManager grid,
        Vector2Int startCell,
        Vector2Int facing,
        int maxSteps,
        Vector2Int? virtualBlocked,
        out Exploration exploration)
    {
        exploration = default;

        if (grid == null || maxSteps <= 0 || !grid.IsInsideGrid(startCell))
            return false;

        // No tiene sentido analizar desde una celda que se quiere tratar
        // como bloqueada: el estado inicial sería inválido.
        if (virtualBlocked.HasValue && virtualBlocked.Value == startCell)
            return false;

        int startDirection = DirectionIndex(facing);

        if (startDirection < 0)
            return false;

        EnsureCapacity(grid);

        int columns = grid.Columns;
        int rows = grid.Rows;
        int gen = NextGeneration();

        int startState =
            (startCell.y * columns + startCell.x) * DirectionCount + startDirection;

        int head = 0;
        int tail = 0;

        stateQueue[tail++] = startState;
        stateStamp[startState] = gen;
        stateDepth[startState] = 0;
        stateFirstStep[startState] = -1;

        int reachableStates = 0;
        int reachableCells = 0;
        int nearestJunction = -1;
        bool truncated = false;

        while (head < tail)
        {
            int state = stateQueue[head++];

            int depth = stateDepth[state];
            int direction = state % DirectionCount;
            int cellIndex = state / DirectionCount;

            reachableStates++;

            // La primera vez que se ve una celda es con su profundidad
            // mínima: el BFS recorre por niveles.
            if (cellStamp[cellIndex] != gen)
            {
                cellStamp[cellIndex] = gen;
                cellDepth[cellIndex] = depth;
                cellFirstStep[cellIndex] = stateFirstStep[state];
                reachableCells++;
            }

            if (depth >= maxSteps)
            {
                // Había más tablero por explorar del que el horizonte
                // permite ver: la zona no está cerrada.
                truncated = true;
                continue;
            }

            int cellX = cellIndex % columns;
            int cellY = cellIndex / columns;

            int legalMoves = 0;
            int firstStep = stateFirstStep[state];

            // offset -1 = girar izquierda, 0 = seguir recto, +1 = girar
            // derecha. El reverso nunca entra en el conjunto.
            for (int offset = -1; offset <= 1; offset++)
            {
                int nextDirection = (direction + offset + DirectionCount) % DirectionCount;

                Vector2Int step = DirectionFromIndex(nextDirection);

                int nextX = cellX + step.x;
                int nextY = cellY + step.y;

                if (nextX < 0 || nextX >= columns || nextY < 0 || nextY >= rows)
                    continue;

                Vector2Int nextCell = new Vector2Int(nextX, nextY);

                // Celda hipotéticamente bloqueada (por ejemplo, la garganta
                // que Ambush quiere cerrar): se trata como ocupada sin
                // modificar la grilla real.
                if (virtualBlocked.HasValue && virtualBlocked.Value == nextCell)
                    continue;

                if (grid.IsCellOccupied(nextCell))
                    continue;

                legalMoves++;

                int nextState =
                    (nextY * columns + nextX) * DirectionCount + nextDirection;

                if (stateStamp[nextState] == gen)
                    continue;

                stateStamp[nextState] = gen;
                stateDepth[nextState] = depth + 1;
                stateFirstStep[nextState] = firstStep < 0 ? nextDirection : firstStep;
                stateQueue[tail++] = nextState;
            }

            // Una intersección es una celda desde la que existe más de una
            // salida real: la única forma de no ser arrastrado por un
            // pasillo hasta el fondo.
            if (legalMoves >= 2 && nearestJunction < 0)
                nearestJunction = depth;
        }

        exploration = new Exploration(
            gen,
            reachableStates,
            reachableCells,
            nearestJunction,
            truncated);

        return true;
    }

    // ── Buffers ─────────────────────────────────────────────────────────

    private static void EnsureCapacity(GridManager grid)
    {
        int cells = grid.Columns * grid.Rows;

        if (cells == cachedCells)
            return;

        cachedCells = cells;

        int states = cells * DirectionCount;

        stateStamp = new int[states];
        stateDepth = new int[states];
        stateFirstStep = new int[states];
        stateQueue = new int[states];

        cellStamp = new int[cells];
        cellDepth = new int[cells];
        cellFirstStep = new int[cells];
        cellQueue = new int[cells];

        generation = 0;
    }

    /// <summary>
    /// Generación monotónica: evita limpiar los buffers entre llamadas.
    /// Una celda está "visitada" si su marca es igual a la generación
    /// actual. Cuando el contador se agota, se limpia una sola vez.
    /// </summary>
    private static int NextGeneration()
    {
        generation++;

        if (generation == int.MaxValue)
        {
            System.Array.Clear(stateStamp, 0, stateStamp.Length);
            System.Array.Clear(cellStamp, 0, cellStamp.Length);
            generation = 1;
        }

        return generation;
    }
}
