using UnityEngine;

/// <summary>
/// Acumula el score de la partida actual. Clase estática — sobrevive el
/// cambio de escena sin DontDestroyOnLoad. Se resetea al llamar a Reset().
/// </summary>
public static class ScoreManager
{
    // ── Configuración ─────────────────────────────────────────────────────
    public static int EnemyKillScore = 100;
    public static int PowerUpPickupScore = 25;
    public static int MovementScorePerCell = 5;
    public static int MovementScoreCellInterval = 5;
    public static int BaseWaveScore = 100;


    // ── Estado ────────────────────────────────────────────────────────────
    public static int TotalScore { get; private set; }
    public static int LastGain { get; private set; }
    private static int cellStepCounter = 0;

    // ── Eventos ───────────────────────────────────────────────────────────
    public static event System.Action<int> OnScoreChanged;
    public static event System.Action<int, string> OnGainEvent;

    // ─────────────────────────────────────────────────────────────────────

    public static void Reset()
    {
        TotalScore = 0;
        LastGain = 0;
        cellStepCounter = 0;
    }

    // ── Oleada ────────────────────────────────────────────────────────────

    public static void AddWaveCompletionScore(int waveNumber, int baseScore)
    {
        int points = baseScore * waveNumber;
        LastGain = points;
        Add(points, BaseWaveScore, "Wave complete");
    }

    // ── Kill ──────────────────────────────────────────────────────────────

    public static void AddKillScore()
    {
        LastGain = EnemyKillScore;
        Add(EnemyKillScore, EnemyKillScore, "Enemy killed");
    }

    // ── Movimiento ────────────────────────────────────────────────────────

    /// <summary>
    /// Silencioso — actualiza el marcador cada N celdas sin disparar
    /// OnGainEvent para no generar ruido visual.
    /// </summary>
    public static void AddMovementScore()
    {
        cellStepCounter++;
        if (cellStepCounter >= MovementScoreCellInterval)
        {
            cellStepCounter = 0;
            TotalScore += MovementScorePerCell;
            OnScoreChanged?.Invoke(TotalScore);
        }
    }

    // ── Power-up ──────────────────────────────────────────────────────────

    public static void AddPowerUpScore()
    {
        LastGain = PowerUpPickupScore;
        Add(PowerUpPickupScore, PowerUpPickupScore, "Power-up collected");
    }

    // ── Interno ───────────────────────────────────────────────────────────

    /// <summary>
    /// displayPoints es lo que se muestra en el Gain_Text (puede diferir
    /// del total sumado — por ejemplo, la oleada suma base×wave al total
    /// pero muestra sólo base en la transición).
    /// </summary>
    private static void Add(int scorePoints, int displayPoints, string label)
    {
        TotalScore += scorePoints;
        OnScoreChanged?.Invoke(TotalScore);
        OnGainEvent?.Invoke(displayPoints, label);
    }
}