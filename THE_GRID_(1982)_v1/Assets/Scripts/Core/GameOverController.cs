using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra el panel de fin de partida cuando GameManager avisa que la
/// partida terminó, con el texto, color, sonido y música correspondientes
/// al resultado — todo disparado en el mismo instante en que el panel se
/// hace visible, nunca antes.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private float panelRevealDelay = 1f;

    [Header("Color según resultado")]
    [SerializeField] private Color victoryColor = Color.white;
    [SerializeField] private Color defeatColor = new Color(225f / 255f, 97f / 255f, 15f / 255f);

    [Header("Audio")]
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private AudioClip defeatMusic;

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

        StartCoroutine(ShowPanelAfterDelay(playerWon));
    }

    private IEnumerator ShowPanelAfterDelay(bool playerWon)
    {
        yield return new WaitForSecondsRealtime(panelRevealDelay);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Sonido y música de resultado se disparan acá, junto con el
        // panel — no antes, cuando GameManager.OnMatchEnded avisó que la
        // partida terminó — para que coincidan con lo que el jugador
        // realmente ve en pantalla. loop: false porque es un resultado
        // puntual, no una pista de fondo continua.
        AudioManager.Instance?.PlaySfx(playerWon ? SfxId.Victory : SfxId.Defeat);
        AudioManager.Instance?.PlayMusic(playerWon ? victoryMusic : defeatMusic, loop: false);
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