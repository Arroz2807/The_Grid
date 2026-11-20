using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Vista del ranking para el segundo monitor. Dibuja las 10 filas con el mismo
/// formato y reglas de color que la escena Ranking (RankingRowFormatter) y se
/// mantiene actualizada sola: cada 'pollInterval' segundos compara una foto
/// del ranking con la anterior y, si cambió, se redibuja.
///
/// Resaltado: cuando detecta que apareció UNA entrada nueva (la puntuación
/// recién registrada), la resalta. El resaltado se mantiene hasta que
/// empieza otra partida (se carga la escena de juego), entra otra entrada o
/// se borra el ranking. No depende de RankingSceneController ni modifica a
/// RankingManager: solo lee RankingManager.GetEntries().
/// </summary>
public class RankingBoardView : MonoBehaviour
{
    [Header("Filas (Row0..Row9, en orden numérico)")]
    [SerializeField] private TMP_Text[] rowTexts = new TMP_Text[10];

    [Header("Colores (mismos valores que RankingSceneController)")]
    [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color highlightColor = Color.cyan;
    [SerializeField] private Color emptyColor = new Color(0.25f, 0.25f, 0.25f);
    [SerializeField] private Color goldColor = new Color(1f, 0.8724f, 0f);
    [SerializeField] private Color silverColor = new Color(0.8868f, 0.8868f, 0.8868f);
    [SerializeField] private Color bronzeColor = new Color(0.8774f, 0.4971f, 0.1117f);

    [Header("Actualización")]
    [Tooltip("Cada cuántos segundos se revisa si el ranking cambió.")]
    [Min(0.1f)]
    [SerializeField] private float pollInterval = 0.5f;

    // Foto del ranking en la última lectura (null hasta la primera). Cada
    // elemento resume una entrada: nombre|puntaje|oleada|fecha.
    private string[] lastSignatures;

    // Firma de la entrada a resaltar (null = ninguna). Se guarda la firma y no
    // el índice porque, al entrar otras entradas, la posición cambia.
    private string highlightSignature;

    private float nextPollTime;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Refresh(forceRedraw: true);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        // Tiempo sin escala: el sondeo no depende de Time.timeScale.
        if (Time.unscaledTime < nextPollTime) return;

        nextPollTime = Time.unscaledTime + pollInterval;
        Refresh(forceRedraw: false);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Al cargarse la escena de juego empieza una partida nueva: el
        // resaltado de la puntuación anterior ya no corresponde.
        if (scene.name != SceneNames.Game) return;

        highlightSignature = null;
        Refresh(forceRedraw: true);
    }

    /// <summary>
    /// Lee el ranking y se redibuja si cambió (o si se fuerza).
    /// </summary>
    private void Refresh(bool forceRedraw)
    {
        List<RankingEntry> entries = RankingManager.GetEntries();
        string[] current = BuildSignatures(entries);

        bool changed = lastSignatures == null || !SameSequence(lastSignatures, current);
        if (!changed && !forceRedraw) return;

        // La primera lectura solo toma la foto inicial: todavía no hay una
        // "entrada nueva" que resaltar.
        if (lastSignatures != null && changed)
        {
            highlightSignature = FindAddedEntry(lastSignatures, current);
        }

        lastSignatures = current;
        Redraw(entries, current);
    }

    private void Redraw(List<RankingEntry> entries, string[] signatures)
    {
        // Si hay firmas repetidas, la entrada nueva es la última de las
        // iguales (los empates se agregan después de las existentes).
        int highlightIndex = highlightSignature != null
            ? Array.LastIndexOf(signatures, highlightSignature)
            : -1;

        var palette = new RankingPalette(normalColor, highlightColor, emptyColor,
                                         goldColor, silverColor, bronzeColor);

        for (int i = 0; i < rowTexts.Length; i++)
        {
            if (rowTexts[i] == null) continue;

            bool hasEntry = i < entries.Count;

            rowTexts[i].text = RankingRowFormatter.FormatRow(i, entries);
            rowTexts[i].color = RankingRowFormatter.GetColor(i, hasEntry, i == highlightIndex, palette);
        }
    }

    // ------------------------------------------------------------ ayudantes

    private static string[] BuildSignatures(List<RankingEntry> entries)
    {
        string[] result = new string[entries.Count];

        for (int i = 0; i < entries.Count; i++)
        {
            RankingEntry e = entries[i];
            result[i] = e.playerName + "|" + e.score + "|" + e.wave + "|" + e.date;
        }

        return result;
    }

    private static bool SameSequence(string[] a, string[] b)
    {
        if (a.Length != b.Length) return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }

        return true;
    }

    /// <summary>
    /// Compara las dos fotos como conjuntos: devuelve la firma de la entrada
    /// que está en 'current' y no estaba en 'previous'. Si no hay ninguna
    /// (solo se borró algo) o hay más de una (cambio ambiguo), devuelve null.
    /// </summary>
    private static string FindAddedEntry(string[] previous, string[] current)
    {
        var remaining = new Dictionary<string, int>();
        foreach (string s in previous)
        {
            int n;
            remaining.TryGetValue(s, out n);
            remaining[s] = n + 1;
        }

        string added = null;
        int addedCount = 0;

        foreach (string s in current)
        {
            int n;
            if (remaining.TryGetValue(s, out n) && n > 0)
            {
                remaining[s] = n - 1; // ya existía
            }
            else
            {
                added = s;
                addedCount++;
            }
        }

        return addedCount == 1 ? added : null;
    }

    /// <summary>
    /// Ayuda de configuración: menú contextual del componente (⋮ en el
    /// Inspector). Completa 'Row Texts' buscando, dentro de este objeto, los
    /// textos llamados Row0..Row9 y ordenándolos por número. Evita el error de
    /// asignarlos en el orden de la jerarquía, que está desordenado.
    /// </summary>
    [ContextMenu("Auto-asignar filas por nombre (Row0..Row9)")]
    private void AutoAssignRows()
    {
        TMP_Text[] found = new TMP_Text[10];

        foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            Match match = Regex.Match(text.name, @"^Row(\d+)$");
            if (!match.Success) continue;

            int index = int.Parse(match.Groups[1].Value);
            if (index >= 0 && index < found.Length) found[index] = text;
        }

        rowTexts = found;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        int assigned = 0;
        foreach (TMP_Text t in found) if (t != null) assigned++;
        Debug.Log("[RankingBoardView] Filas asignadas: " + assigned + " de " + found.Length + ".");
    }
}