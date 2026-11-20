
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Controlador de la escena Ranking.
/// Muestra las entradas y permite regresar a la escena de origen.
/// </summary>
public class RankingSceneController : MonoBehaviour
{
    [SerializeField] private TMP_Text[] rowTexts = new TMP_Text[10];

    [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color highlightColor = Color.cyan;
    [SerializeField] private Color emptyColor = new Color(0.25f, 0.25f, 0.25f);
    [SerializeField] private Color goldColor = new Color(1f, 0.84f, 0f);
    [SerializeField] private Color silverColor = new Color(0.75f, 0.75f, 0.75f);
    [SerializeField] private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);

    [SerializeField] private MenuNavigator navigator;
    [SerializeField] private NavigableButton backButton;

    // Posición 1-based a resaltar.
    public static int HighlightPosition = -1;

    // Escena desde la que se abrió Ranking.
    private static string returnSceneName;

    /// <summary>
    /// Registra la escena a la que debe regresar el botón Back.
    /// </summary>
    public static void SetReturnScene(string sceneName)
    {
        returnSceneName = string.IsNullOrWhiteSpace(sceneName)
            ? SceneNames.MainMenu
            : sceneName;
    }

    private void Start()
    {
        PopulateRows();

        if (navigator != null && backButton != null)
        {
            navigator.SetOptions(
                new List<INavigableOption> { backButton }
            );
        }
    }

    private void PopulateRows()
    {
        List<RankingEntry> entries = RankingManager.GetEntries();

        var palette = new RankingPalette(
            normalColor,
            highlightColor,
            emptyColor,
            goldColor,
            silverColor,
            bronzeColor
        );

        for (int i = 0; i < rowTexts.Length; i++)
        {
            if (rowTexts[i] == null)
                continue;

            bool hasEntry = i < entries.Count;
            bool isHighlight = (i + 1) == HighlightPosition;

            rowTexts[i].text = RankingRowFormatter.FormatRow(
                i,
                entries
            );

            rowTexts[i].color = RankingRowFormatter.GetColor(
                i,
                hasEntry,
                isHighlight,
                palette
            );
        }
    }

    public void OnBackButtonClicked()
    {
        HighlightPosition = -1;

        string destination = string.IsNullOrWhiteSpace(returnSceneName)
            ? SceneNames.MainMenu
            : returnSceneName;

        // Evitar que una futura apertura reutilice el origen anterior.
        returnSceneName = null;

        Debug.Log(
            $"[RankingSceneController] Regresando a: {destination}"
        );

        SceneLoader.Load(destination);
    }
}
