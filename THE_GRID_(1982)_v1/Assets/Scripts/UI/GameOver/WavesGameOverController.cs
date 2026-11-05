
using TMPro;
using UnityEngine;

/// <summary>
/// Orquesta el flujo de fin de partida en modo Waves:
/// muerte → delay → NameEntryUI → SubmitName → WavesGameOverPanel.
/// El botón Ranking carga la escena dedicada de ranking.
/// </summary>
public class WavesGameOverController : MonoBehaviour
{
    private const string BestScoreKey = "WavesBestScore";

    [SerializeField] private GameObject wavesGameOverPanel;
    [SerializeField] private TMP_Text waveReachedText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text rankingPositionText;
    [SerializeField] private NameEntryUI nameEntryUI;
    [SerializeField] private float panelRevealDelay = 0.8f;

    private WaveManager waveManager;
    private GameManager gameManager;
    private int cachedFinalScore;
    private int cachedWaveReached;
    private int cachedRankingPosition;

    private void Awake()
    {
        waveManager = FindFirstObjectByType<WaveManager>();
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void OnEnable()
    {
        WaveManager.OnWavesGameOver += HandleWavesGameOver;
    }

    private void OnDisable()
    {
        WaveManager.OnWavesGameOver -= HandleWavesGameOver;
    }

    private void HandleWavesGameOver()
    {
        StartCoroutine(WaitThenShowNameEntry());
    }

    private System.Collections.IEnumerator WaitThenShowNameEntry()
    {
        yield return new WaitForSecondsRealtime(panelRevealDelay);

        cachedFinalScore = ScoreManager.TotalScore;
        cachedWaveReached = waveManager != null ? waveManager.CurrentWave : 0;

        // Mostrar el selector de iniciales y limpiar la escena en el mismo
        // frame: los objetos se destruyen detrás del panel.
        nameEntryUI?.Show();
        gameManager?.CleanupWavesMatch();
    }

    public void SubmitName(string initials)
    {
        cachedRankingPosition = RankingManager.TryAddEntry(
            initials, cachedFinalScore, cachedWaveReached);

        int bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

        if (cachedFinalScore > bestScore)
        {
            bestScore = cachedFinalScore;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }

        if (waveReachedText != null)
            waveReachedText.text = cachedWaveReached.ToString();

        if (finalScoreText != null)
            finalScoreText.text = cachedFinalScore.ToString("D6");

        if (bestScoreText != null)
            bestScoreText.text = bestScore.ToString("D6");

        if (rankingPositionText != null)
        {
            rankingPositionText.text = cachedRankingPosition > 0
                ? $"#{cachedRankingPosition}" : "---";
        }

        if (wavesGameOverPanel != null)
            wavesGameOverPanel.SetActive(true);
    }

    public void OnRankingButtonClicked()
    {
        // Pasar la posición a resaltar y cargar la escena de ranking.
        RankingSceneController.HighlightPosition = cachedRankingPosition;
        SceneLoader.Load(SceneNames.Ranking);
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
