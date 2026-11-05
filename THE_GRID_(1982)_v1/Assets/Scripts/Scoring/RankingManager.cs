using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Carga, guarda y ordena el ranking de Waves. No es un MonoBehaviour —
/// es una clase estática de servicio, igual que ScoreManager. Esto evita
/// tener que colocar un GameObject en la escena sólo para leer/escribir
/// un archivo JSON.
///
/// Regla de empate:
/// 1. Mayor score gana.
/// 2. Con mismo score, mayor oleada gana.
/// 3. Con mismo score Y misma oleada, el registro más antiguo conserva
///    su posición (el nuevo no entra si ya hay MaxEntries).
///
/// Persistencia: Application.persistentDataPath, garantizado por Unity
/// en todas las plataformas con permisos de escritura.
/// </summary>
public static class RankingManager
{
    private const string FileName = "waves_ranking.json";
    private const int MaxEntries = 10;

    private static RankingData cachedData;

    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    // ── Lectura ───────────────────────────────────────────────────────────

    public static List<RankingEntry> GetEntries()
    {
        if (cachedData == null) Load();
        return cachedData.entries;
    }

    // ── Escritura ─────────────────────────────────────────────────────────

    /// <summary>
    /// Intenta agregar una nueva entrada al ranking. Devuelve la posición
    /// 1-based donde quedó (1 = primero), o -1 si no entró al ranking.
    /// </summary>
    public static int TryAddEntry(string playerName, int score, int wave)
    {
        if (cachedData == null) Load();

        RankingEntry newEntry = new RankingEntry(playerName, score, wave);
        List<RankingEntry> entries = cachedData.entries;

        // Encontrar la posición correcta según las reglas de orden.
        int insertAt = entries.Count; // por defecto: al final

        for (int i = 0; i < entries.Count; i++)
        {
            if (IsBetter(newEntry, entries[i]))
            {
                insertAt = i;
                break;
            }
        }

        // Si no entra en el top MaxEntries, no se guarda.
        if (insertAt >= MaxEntries) return -1;

        entries.Insert(insertAt, newEntry);

        // Recortar si superamos el máximo.
        if (entries.Count > MaxEntries)
            entries.RemoveAt(entries.Count - 1);

        Save();
        return insertAt + 1; // 1-based
    }

    /// <summary>
    /// Borra el ranking completo. Usado por el botón Reset Rankings
    /// del menú de Settings.
    /// </summary>
    public static void ClearRanking()
    {
        cachedData = new RankingData();
        Save();
        Debug.Log("[RankingManager] Ranking borrado.");
    }

    // ── Persistencia ──────────────────────────────────────────────────────

    private static void Load()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                string json = File.ReadAllText(FilePath);
                cachedData = JsonUtility.FromJson<RankingData>(json);
                Debug.Log($"[RankingManager] Ranking cargado ({cachedData.entries.Count} entradas).");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[RankingManager] Error al cargar ranking: {ex.Message}. Iniciando vacío.");
                cachedData = new RankingData();
            }
        }
        else
        {
            cachedData = new RankingData();
        }
    }

    private static void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(cachedData, prettyPrint: true);
            File.WriteAllText(FilePath, json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[RankingManager] Error al guardar ranking: {ex.Message}");
        }
    }

    // ── Comparación ───────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve true si 'a' debe ir ANTES que 'b' en el ranking.
    /// Mayor score gana. Con empate, mayor oleada gana.
    /// Con empate en ambos, b conserva su posición (a NO es mejor).
    /// </summary>
    private static bool IsBetter(RankingEntry a, RankingEntry b)
    {
        if (a.score != b.score) return a.score > b.score;
        if (a.wave != b.wave) return a.wave > b.wave;
        return false; // empate total: el registro existente (b) gana
    }
}