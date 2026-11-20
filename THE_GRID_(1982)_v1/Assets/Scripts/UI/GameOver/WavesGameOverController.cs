
using TMPro;
using UnityEngine;

public class WavesGameOverController : MonoBehaviour
{
    private const string BestScoreKey = "WavesBestScore";

    [Header("Panel de Game Over")]
    [SerializeField] private GameObject wavesGameOverPanel;
    [SerializeField] private TMP_Text gameOverTitleText;

    [Header("Datos de partida")]
    [SerializeField] private TMP_Text waveReachedText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text rankingPositionText;

    [Header("Entrada de nombre")]
    [SerializeField] private NameEntryUI nameEntryUI;

    [Header("Temporización")]
    [SerializeField] private float panelRevealDelay = 0.8f;

    private WaveManager waveManager;
    private GameManager gameManager;

    private int cachedFinalScore;
    private int cachedWaveReached;
    private int cachedRankingPosition;
    private int cachedBestScore;

    private bool gameOverHandled;
    private bool nameSubmitted;

    private void Awake()
    {
        waveManager = FindFirstObjectByType<WaveManager>();
        gameManager = FindFirstObjectByType<GameManager>();

        if (gameOverTitleText != null)
            gameOverTitleText.text = "GAME OVER";
    }

    private void OnEnable()
    {
        WaveManager.OnWavesGameOver += HandleWavesGameOver;
    }

    private void OnDisable()
    {
        WaveManager.OnWavesGameOver -= HandleWavesGameOver;
    }

    private void Start()
    {
        // Si regresamos desde Ranking, restaurar los resultados
        // sin volver a solicitar iniciales ni registrar otra entrada.
        if (WavesGameOverSession.TryRestore(
            out int wave,
            out int score,
            out int best,
            out int position))
        {
            cachedWaveReached = wave;
            cachedFinalScore = score;
            cachedBestScore = best;
            cachedRankingPosition = position;

            nameSubmitted = true;

            RefreshResultTexts();

            if (wavesGameOverPanel != null)
                wavesGameOverPanel.SetActive(true);

            Debug.Log(
                "[WavesGameOverController] Game Over restaurado " +
                "después de regresar desde Ranking."
            );
        }
    }

    private void HandleWavesGameOver()
    {
        if (gameOverHandled)
            return;

        gameOverHandled = true;

        AudioManager.Instance?.PlayMenuPlaylist();

        StartCoroutine(WaitThenShowNameEntry());
    }

    private System.Collections.IEnumerator WaitThenShowNameEntry()
    {
        yield return new WaitForSecondsRealtime(panelRevealDelay);

        cachedFinalScore = ScoreManager.TotalScore;

        cachedWaveReached = waveManager != null
            ? waveManager.CurrentWave
            : 0;

        nameEntryUI?.Show();

        gameManager?.CleanupWavesMatch();
    }

    /// <summary>
    /// Recibe las iniciales y registra el resultado de la partida.
    /// </summary>
    public void SubmitName(string initials)
    {
        if (nameSubmitted)
        {
            Debug.LogWarning(
                "[WavesGameOverController] Se ignoró un SubmitName duplicado."
            );

            return;
        }

        nameSubmitted = true;

        cachedRankingPosition = RankingManager.TryAddEntry(
            initials,
            cachedFinalScore,
            cachedWaveReached
        );

        // --------------------------------------------------------
        // RECUPERAR EL MEJOR SCORE REAL
        // --------------------------------------------------------

        int previousBest = PlayerPrefs.GetInt(BestScoreKey, 0);
        int rankingBest = 0;

        var entries = RankingManager.GetEntries();

        if (entries != null)
        {
            foreach (RankingEntry entry in entries)
            {
                if (entry != null && entry.score > rankingBest)
                    rankingBest = entry.score;
            }
        }

        cachedBestScore = Mathf.Max(
            previousBest,
            rankingBest,
            cachedFinalScore
        );

        // Guardar el valor reconciliado.
        if (!PlayerPrefs.HasKey(BestScoreKey) ||
            PlayerPrefs.GetInt(BestScoreKey) != cachedBestScore)
        {
            PlayerPrefs.SetInt(BestScoreKey, cachedBestScore);
            PlayerPrefs.Save();
        }

        RefreshResultTexts();

        if (wavesGameOverPanel != null)
            wavesGameOverPanel.SetActive(true);

        Debug.Log(
            $"[WavesGameOverController] Resultado registrado: " +
            $"{cachedFinalScore} puntos. Best Score: {cachedBestScore}."
        );
    }

    /// <summary>
    /// Actualiza los textos visibles del panel.
    /// También se utiliza al restaurar el Game Over.
    /// </summary>
    private void RefreshResultTexts()
    {
        if (waveReachedText != null)
            waveReachedText.text = cachedWaveReached.ToString();

        if (finalScoreText != null)
            finalScoreText.text = cachedFinalScore.ToString("D6");

        if (bestScoreText != null)
            bestScoreText.text = cachedBestScore.ToString("D6");

        if (rankingPositionText != null)
        {
            rankingPositionText.text = cachedRankingPosition > 0
                ? $"#{cachedRankingPosition}"
                : "---";
        }
    }

    public void OnRankingButtonClicked()
    {
        // Guardar los resultados antes de abandonar la escena.
        WavesGameOverSession.PrepareReturn(
            cachedWaveReached,
            cachedFinalScore,
            cachedBestScore,
            cachedRankingPosition
        );

        RankingSceneController.HighlightPosition =
            cachedRankingPosition;

        RankingSceneController.SetReturnScene(SceneNames.Game);

        SceneLoader.Load(SceneNames.Ranking);
    }

    public void OnRetryButtonClicked()
    {
        // No restaurar el Game Over al iniciar la nueva partida.
        WavesGameOverSession.Clear();

        AudioManager.Instance?.CancelResultAudioForGameplayRestart();

        SceneLoader.Load(SceneNames.Game);
    }

    public void OnMainMenuButtonClicked()
    {
        // Descartar los datos temporales de esta partida.
        WavesGameOverSession.Clear();

        SceneLoader.Load(SceneNames.MainMenu);
    }
}
