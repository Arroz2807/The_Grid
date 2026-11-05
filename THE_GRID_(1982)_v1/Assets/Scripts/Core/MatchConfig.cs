using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda la configuración elegida en el menú mientras dura la sesión de
/// juego. Los campos estáticos sobreviven al cambio de escena — por eso
/// el viaje MainMenu → Game funciona sin DontDestroyOnLoad ni
/// ScriptableObject. Se resetean solos al cerrar la aplicación.
/// </summary>
public static class MatchConfig
{
    // ── Modo de juego ──────────────────────────────────────────────────
    // GameManager lo lee en HandleMatchReady para decidir si usa
    // EnemyTypes (Custom) o delega en WaveManager (Waves).
    public static GameMode GameMode { get; private set; } = GameMode.Custom;

    public static void SetGameMode(GameMode mode)
    {
        GameMode = mode;
    }

    // ── Señal de UI entre escenas ──────────────────────────────────────────
    // Cuando el jugador termina una partida Waves y pide ver el ranking,
    // cargamos MainMenu y este flag le dice que abra el ranking al llegar.
    public static bool ShowRankingOnLoad { get; private set; } = false;
    public static int RankingHighlightPosition { get; private set; } = -1;

    public static void RequestRankingOnLoad(int highlightPosition = -1)
    {
        ShowRankingOnLoad = true;
        RankingHighlightPosition = highlightPosition;
    }

    public static void ClearRankingRequest()
    {
        ShowRankingOnLoad = false;
        RankingHighlightPosition = -1;
    }

    // ── Power-ups ──────────────────────────────────────────────────────
    public static bool PowerUpsEnabled { get; private set; } = true;

    public static void SetPowerUpsEnabled(bool enabled)
    {
        PowerUpsEnabled = enabled;
    }

    // ── Configuración de enemigos (sólo Custom) ────────────────────────
    private static readonly List<EnemyType> enemyTypes = new List<EnemyType>();

    public static IReadOnlyList<EnemyType> EnemyTypes => enemyTypes;

    public static void SetEnemyCount(int count)
    {
        count = Mathf.Clamp(count, 0, 4);

        while (enemyTypes.Count < count) enemyTypes.Add(EnemyType.Random);
        while (enemyTypes.Count > count) enemyTypes.RemoveAt(enemyTypes.Count - 1);
    }

    public static void SetEnemyType(int slotIndex, EnemyType type)
    {
        if (slotIndex < 0 || slotIndex >= enemyTypes.Count) return;
        enemyTypes[slotIndex] = type;
    }
}