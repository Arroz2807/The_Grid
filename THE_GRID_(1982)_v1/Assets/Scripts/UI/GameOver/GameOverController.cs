
using System.Collections;
using TMPro;
using UnityEngine;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private float panelRevealDelay = 1f;

    [Header("Color según resultado")]
    [SerializeField] private Color victoryColor = Color.white;
    [SerializeField] private Color defeatColor =
        new Color(225f / 255f, 97f / 255f, 15f / 255f);

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
        if (MatchConfig.GameMode == GameMode.Waves)
            return;

        if (matchEndTriggered)
            return;

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
            gameOverPanel.SetActive(true);

        AudioManager.Instance?.PlaySfxThenPlayMenuPlaylist(
            playerWon ? SfxId.Victory : SfxId.Defeat
        );
    }

    public void OnRetryButtonClicked()
    {
        // Cancelar inmediatamente el efecto de resultado
        // y cualquier transición de audio anterior.
        AudioManager.Instance?.CancelResultAudioForGameplayRestart();

        // La nueva escena iniciará la playlist de gameplay.
        SceneLoader.Load(SceneNames.Game);
    }

    public void OnMainMenuButtonClicked()
    {
        SceneLoader.Load(SceneNames.MainMenu);
    }
}
