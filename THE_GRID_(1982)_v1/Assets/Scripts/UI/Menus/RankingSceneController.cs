using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Controlador de la escena de Ranking. Lee el ranking de RankingManager,
/// lo muestra, y vuelve al Main Menu al presionar Back. La posición a
/// resaltar se pasa entre escenas a través de un campo estático.
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

    // Posición 1-based a resaltar. Se setea antes de cargar esta escena.
    // -1 = sin resaltar (venimos del Main Menu).
    public static int HighlightPosition = -1;

    private void Start()
    {
        PopulateRows();

        // Registrar el único botón navegable de esta escena.
        if (navigator != null && backButton != null)
        {
            navigator.SetOptions(new List<INavigableOption> { backButton });
        }
    }

    private void PopulateRows()
    {
        List<RankingEntry> entries = RankingManager.GetEntries();

        for (int i = 0; i < rowTexts.Length; i++)
        {
            if (rowTexts[i] == null) continue;

            bool isHighlight = (i + 1) == HighlightPosition;

            if (i < entries.Count)
            {
                RankingEntry e = entries[i];
                rowTexts[i].text = $"{(i + 1):D2}   {e.playerName}   {e.score:D6}   W{e.wave}";

                // Highlight de la última entrada tiene prioridad sobre el
                // color de posición — el jugador ve su propia fila en cyan.
                if (isHighlight)
                    rowTexts[i].color = highlightColor;
                else if (i == 0)
                    rowTexts[i].color = goldColor;
                else if (i == 1)
                    rowTexts[i].color = silverColor;
                else if (i == 2)
                    rowTexts[i].color = bronzeColor;
                else
                    rowTexts[i].color = normalColor;
            }
            else
            {
                rowTexts[i].text = $"{(i + 1):D2}   ---   ------   --";
                rowTexts[i].color = emptyColor;
            }
        }
    }

    public void OnBackButtonClicked()
    {
        HighlightPosition = -1; // limpiar para la próxima vez
        SceneLoader.Load(SceneNames.MainMenu);
    }
}