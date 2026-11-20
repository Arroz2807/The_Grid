
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Carga, guarda y ordena el ranking de Waves.
///
/// Reglas:
/// 1. Mayor score primero.
/// 2. Con mismo score, mayor oleada primero.
/// 3. Con empate total, el registro existente conserva su lugar.
///
/// También centraliza el borrado del Best Score cuando se reinicia
/// el ranking.
/// </summary>
public static class RankingManager
{
    private const string FileName = "waves_ranking.json";
    private const string BestScoreKey = "WavesBestScore";
    private const int MaxEntries = 10;

    private static RankingData cachedData;

    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    // ------------------------------------------------------------
    // LECTURA
    // ------------------------------------------------------------

    public static List<RankingEntry> GetEntries()
    {
        if (cachedData == null)
            Load();

        EnsureValidData();

        return cachedData.entries;
    }

    // ------------------------------------------------------------
    // ESCRITURA
    // ------------------------------------------------------------

    /// <summary>
    /// Intenta agregar una entrada al ranking.
    /// Devuelve la posición 1-based o -1 si no entra en el Top 10.
    /// </summary>
    public static int TryAddEntry(
        string playerName,
        int score,
        int wave)
    {
        if (cachedData == null)
            Load();

        EnsureValidData();

        RankingEntry newEntry = new RankingEntry(
            playerName,
            score,
            wave
        );

        List<RankingEntry> entries = cachedData.entries;

        int insertAt = entries.Count;

        for (int i = 0; i < entries.Count; i++)
        {
            if (IsBetter(newEntry, entries[i]))
            {
                insertAt = i;
                break;
            }
        }

        if (insertAt >= MaxEntries)
            return -1;

        entries.Insert(insertAt, newEntry);

        if (entries.Count > MaxEntries)
            entries.RemoveAt(entries.Count - 1);

        Save();

        Debug.Log(
            $"[RankingManager] Nueva entrada: {playerName} - " +
            $"{score} puntos - posición #{insertAt + 1}."
        );

        return insertAt + 1;
    }

    /// <summary>
    /// Borra el ranking y el Best Score de Waves.
    /// </summary>
    public static void ClearRanking()
    {
        cachedData = new RankingData();

        Save();

        // El Best Score pertenece al mismo sistema de ranking.
        PlayerPrefs.DeleteKey(BestScoreKey);
        PlayerPrefs.Save();

        Debug.Log(
            "[RankingManager] Ranking y Best Score de Waves reiniciados."
        );
    }

    // ------------------------------------------------------------
    // PERSISTENCIA
    // ------------------------------------------------------------

    private static void Load()
    {
        if (!File.Exists(FilePath))
        {
            cachedData = new RankingData();
            return;
        }

        try
        {
            string json = File.ReadAllText(FilePath);

            cachedData = JsonUtility.FromJson<RankingData>(json);

            EnsureValidData();

            SortEntries();

            // Normalizar el archivo si tenía datos desordenados,
            // incompletos o más entradas que el máximo permitido.
            Save();

            Debug.Log(
                $"[RankingManager] Ranking cargado " +
                $"({cachedData.entries.Count} entradas)."
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning(
                $"[RankingManager] Error al cargar ranking: " +
                $"{ex.Message}. Iniciando vacío."
            );

            cachedData = new RankingData();
        }
    }

    private static void Save()
    {
        try
        {
            EnsureValidData();

            string json = JsonUtility.ToJson(
                cachedData,
                prettyPrint: true
            );

            File.WriteAllText(FilePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(
                $"[RankingManager] Error al guardar ranking: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Garantiza que el contenedor y su lista existan.
    /// También elimina entradas nulas.
    /// </summary>
    private static void EnsureValidData()
    {
        if (cachedData == null)
            cachedData = new RankingData();

        if (cachedData.entries == null)
            cachedData.entries = new List<RankingEntry>();

        cachedData.entries.RemoveAll(entry => entry == null);
    }

    // ------------------------------------------------------------
    // ORDENAMIENTO
    // ------------------------------------------------------------

    /// <summary>
    /// Ordenamiento por inserción estable.
    /// Los empates conservan el orden que tenían.
    /// </summary>
    private static void SortEntries()
    {
        EnsureValidData();

        List<RankingEntry> entries = cachedData.entries;

        for (int i = 1; i < entries.Count; i++)
        {
            RankingEntry current = entries[i];
            int j = i - 1;

            while (j >= 0 && IsBetter(current, entries[j]))
            {
                entries[j + 1] = entries[j];
                j--;
            }

            entries[j + 1] = current;
        }

        if (entries.Count > MaxEntries)
        {
            entries.RemoveRange(
                MaxEntries,
                entries.Count - MaxEntries
            );
        }
    }

    /// <summary>
    /// Devuelve true si a debe aparecer antes que b.
    /// </summary>
    private static bool IsBetter(
        RankingEntry a,
        RankingEntry b)
    {
        if (a.score != b.score)
            return a.score > b.score;

        if (a.wave != b.wave)
            return a.wave > b.wave;

        return false;
    }
}
