using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra el panel de fin de partida cuando GameManager avisa que la
/// partida terminó — ya no escucha la muerte de entidades directamente,
/// sólo la conclusión ya decidida por el árbitro. Su única responsabilidad
/// es de presentación: qué texto mostrar y cuándo revelar el panel, nunca
/// decidir si se ganó o se perdió.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [Tooltip("Panel de fin de partida completo. Debe empezar inactivo en la escena.")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Texto del título del panel — se sobreescribe con VICTORIA o DERROTA según corresponda.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Segundos de espera antes de mostrar el panel, para no tapar la explosión de partículas.")]
    [SerializeField] private float panelRevealDelay = 1f;

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
            titleText.text = playerWon ? "Victory" : "Game Over";
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