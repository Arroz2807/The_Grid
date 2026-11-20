
/// <summary>
/// Conserva temporalmente los resultados de Waves Game Over
/// cuando se abre la escena Ranking.
///
/// No utiliza PlayerPrefs porque estos datos no son una
/// preferencia permanente: solamente deben sobrevivir a
/// una transición entre escenas durante la sesión actual.
/// </summary>
public static class WavesGameOverSession
{
    private static bool hasPendingSnapshot;
    private static bool skipNextMatchStart;

    private static int waveReached;
    private static int finalScore;
    private static int bestScore;
    private static int rankingPosition;

    /// <summary>
    /// Guarda los resultados antes de abandonar Game
    /// para abrir Ranking.
    /// </summary>
    public static void PrepareReturn(
        int wave,
        int score,
        int best,
        int position)
    {
        waveReached = wave;
        finalScore = score;
        bestScore = best;
        rankingPosition = position;

        hasPendingSnapshot = true;
        skipNextMatchStart = true;
    }

    /// <summary>
    /// Permite consultar y recuperar los resultados guardados.
    /// Consume únicamente el snapshot, no la orden de omitir
    /// el inicio de partida.
    /// </summary>
    public static bool TryRestore(
        out int wave,
        out int score,
        out int best,
        out int position)
    {
        wave = 0;
        score = 0;
        best = 0;
        position = -1;

        if (!hasPendingSnapshot)
            return false;

        wave = waveReached;
        score = finalScore;
        best = bestScore;
        position = rankingPosition;

        hasPendingSnapshot = false;

        return true;
    }

    /// <summary>
    /// MatchStarter consulta este método al iniciar la escena.
    /// Devuelve true una sola vez cuando debe evitar el countdown.
    /// </summary>
    public static bool ConsumeSkipNextMatchStart()
    {
        if (!skipNextMatchStart)
            return false;

        skipNextMatchStart = false;
        return true;
    }

    /// <summary>
    /// Descarta cualquier estado pendiente.
    /// Se utiliza al iniciar Retry o regresar al menú.
    /// </summary>
    public static void Clear()
    {
        hasPendingSnapshot = false;
        skipNextMatchStart = false;

        waveReached = 0;
        finalScore = 0;
        bestScore = 0;
        rankingPosition = -1;
    }
}
