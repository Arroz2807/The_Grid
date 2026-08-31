using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra el panel de fin de partida cuando GameManager avisa que la
/// partida terminó, con el texto y color correspondientes al resultado.
/// Su única responsabilidad es de presentación — nunca decide si se ganó
/// o se perdió, sólo reacciona a lo que ya decidió GameManager.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private float panelRevealDelay = 1f;

    [Header("Color según resultado")]
    [Tooltip("Color del texto cuando el jugador gana (queda como el último con vida).")]
    [SerializeField] private Color victoryColor = Color.white;

    [Tooltip("Color del texto cuando el jugador pierde. Por defecto #E1610F.")]
    [SerializeField] private Color defeatColor = new Color(225f / 255f, 97f / 255f, 15f / 255f);

    private bool matchEndTriggered;

    private void OnEnable()
    {
        GameManager.OnMatchEnded += HandleMatchEnded;
    }

    private void OnDisable()
    {
        GameManager.OnMatchEnded -= HandleMatchEnded;
    }

    private void HandleMatchEnded(bool playerWon)
    {
        if (matchEndTriggered) return;
        matchEndTriggered = true;

        if (titleText != null)
        {
            titleText.text = playerWon ? "VICTORY" : "DEFEAT";
            titleText.color = playerWon ? victoryColor : defeatColor;
        }

        StartCoroutine(ShowPanelAfterDelay());
    }

    private IEnumerator ShowPanelAfterDelay()
    {
        yield return new WaitForSecondsRealtime(panelRevealDelay);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void OnRetryButtonClicked()
    {
        SceneLoader.Load(SceneNames.Game);
    }

    public void OnMainMenuButtonClicked()
    {
        SceneLoader.Load(SceneNames.MainMenu);
    }
}