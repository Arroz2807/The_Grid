
using System.Collections;
using UnityEngine;

/// <summary>
/// Orquesta el ciclo de vida de las oleadas en el modo Waves: inicia,
/// detecta el fin de cada oleada, acumula score, y espera a que
/// WaveTransitionUI termine su animación antes de spawnear la siguiente.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [SerializeField] private WaveSystemConfig systemConfig;
    [SerializeField] private WaveProgressionConfig progressionConfig;

    private GameManager gameManager;
    private PowerUpSpawner powerUpSpawner;
    private TrailManager trailManager;

    private int currentWave;
    private int aliveEnemiesThisWave;
    private bool wavesRunning;

    // Señal que WaveTransitionUI dispara cuando termina su animación.
    public static event System.Action OnTransitionComplete;

    /// <summary>
    /// Llamado por WaveTransitionUI cuando termina su animación.
    /// </summary>
    public static void NotifyTransitionComplete()
    {
        OnTransitionComplete?.Invoke();
    }

    // Eventos que otros sistemas escuchan.
    public static event System.Action<int> OnWaveStarted;
    public static event System.Action<int> OnWaveCompleted;
    public static event System.Action OnWavesGameOver;

    // Flag que se activa cuando WaveTransitionUI llama OnTransitionComplete.
    private bool transitionDone;

    public int CurrentWave => currentWave;

    private void OnEnable()
    {
        // WaveManager escucha su propio evento para saber cuándo la UI terminó.
        OnTransitionComplete += HandleTransitionComplete;
    }

    private void OnDisable()
    {
        OnTransitionComplete -= HandleTransitionComplete;
    }

    private void HandleTransitionComplete()
    {
        transitionDone = true;
    }

    public void Initialize(GameManager gm, PowerUpSpawner spawner, TrailManager tm)
    {
        gameManager = gm;
        powerUpSpawner = spawner;
        trailManager = tm;
    }

    public void StartWaves()
    {
        if (wavesRunning) return;

        if (systemConfig == null)
        {
            Debug.LogError("[WaveManager] systemConfig es null.");
            return;
        }

        if (progressionConfig == null)
        {
            Debug.LogError("[WaveManager] progressionConfig es null.");
            return;
        }

        ScoreManager.Reset();

        // Sincronizar los parámetros de score con los valores del asset.
        ScoreManager.EnemyKillScore = systemConfig.enemyKillScore;
        ScoreManager.PowerUpPickupScore = systemConfig.powerUpPickupScore;
        ScoreManager.MovementScorePerCell = systemConfig.movementScorePerCell;
        ScoreManager.MovementScoreCellInterval = systemConfig.movementScoreCellInterval;

        wavesRunning = true;
        currentWave = systemConfig.startingWave - 1;

        StartCoroutine(WaveLoopRoutine());
    }

    public void HandlePlayerDied()
    {
        wavesRunning = false;
        StopAllCoroutines();

        OnWavesGameOver?.Invoke();
        gameManager.EndMatch(playerWon: false);
    }

    public void HandleEnemyDied()
    {
        aliveEnemiesThisWave--;
        ScoreManager.AddKillScore();

        Debug.Log($"[WaveManager] Enemigo eliminado. Quedan: {aliveEnemiesThisWave}");
    }

    // ─── Rutina principal ────────────────────────────────────────────────

    private IEnumerator WaveLoopRoutine()
    {
        while (wavesRunning)
        {
            currentWave++;

            if (systemConfig.maximumWaves > 0 &&
                currentWave > systemConfig.maximumWaves)
            {
                Debug.Log("[WaveManager] Máximo de oleadas alcanzado.");
                OnWavesGameOver?.Invoke();
                yield break;
            }

            yield return StartCoroutine(RunWave(currentWave));

            if (!wavesRunning) yield break;

            // Limpiar rastros justo al terminar la oleada.
            trailManager?.ClearAllTrails();

            // Score de oleada ANTES de OnWaveCompleted.
            ScoreManager.AddWaveCompletionScore(
                currentWave, systemConfig.baseWaveCompletionScore);

            // Avisar a la UI.
            OnWaveCompleted?.Invoke(currentWave);

            Debug.Log(
                $"[WaveManager] Oleada {currentWave} completada. Score: {ScoreManager.TotalScore}");

            // Esperar a que WaveTransitionUI termine su animación.
            transitionDone = false;
            yield return new WaitUntil(() => transitionDone || !wavesRunning);

            if (!wavesRunning) yield break;
        }
    }

    private IEnumerator RunWave(int waveNumber)
    {
        WaveEnemyConfig waveConfig = progressionConfig.GetWaveConfig(waveNumber);
        int enemyCount = waveConfig.enemyTypes.Length;

        aliveEnemiesThisWave = enemyCount;

        AdjustPowerUpSpawnRate(waveNumber);

        OnWaveStarted?.Invoke(waveNumber);

        Debug.Log(
            $"[WaveManager] Iniciando oleada {waveNumber} con {enemyCount} enemigo(s).");

        yield return new WaitForSeconds(systemConfig.preparationTime);

        LightCycleController playerController = gameManager.GetPlayerController();

        if (playerController == null)
        {
            Debug.LogError("[WaveManager] GetPlayerController() devolvió null.");
        }

        gameManager.ResetEnemySpawnTracking();

        for (int i = 0; i < enemyCount; i++)
        {
            gameManager.SpawnWaveEnemy(i, waveConfig, playerController);
        }

        float elapsed = 0f;

        while (aliveEnemiesThisWave > 0 && wavesRunning)
        {
            elapsed += Time.deltaTime;

            if (systemConfig.waveTimeoutSeconds > 0 &&
                elapsed >= systemConfig.waveTimeoutSeconds)
            {
                Debug.LogWarning(
                    $"[WaveManager] Oleada {waveNumber} finalizó por timeout.");

                break;
            }

            yield return null;
        }
    }

    private void AdjustPowerUpSpawnRate(int waveNumber)
    {
        if (powerUpSpawner == null || powerUpSpawner.Config == null) return;

        // t = 0 en las primeras oleadas, t = 1 al alcanzar el umbral avanzado.
        float t = Mathf.Clamp01(
            (float)(waveNumber - 1) /
            Mathf.Max(1, systemConfig.lateGameWaveThreshold - 1));

        powerUpSpawner.Config.minSpawnInterval = Mathf.Lerp(
            systemConfig.initialMinSpawnInterval,
            systemConfig.lateMinSpawnInterval, t);

        powerUpSpawner.Config.maxSpawnInterval = Mathf.Lerp(
            systemConfig.initialMaxSpawnInterval,
            systemConfig.lateMaxSpawnInterval, t);

        // Reducir la cantidad máxima simultánea gradualmente,
        // manteniendo siempre al menos 1.
        powerUpSpawner.Config.maxSimultaneous = Mathf.Max(
            1,
            Mathf.RoundToInt(Mathf.Lerp(
                systemConfig.initialMaxSimultaneous,
                systemConfig.lateMaxSimultaneous, t)));

        Debug.Log(
            $"[WaveManager] Power-ups oleada {waveNumber}: " +
            $"interval [{powerUpSpawner.Config.minSpawnInterval:F1}, " +
            $"{powerUpSpawner.Config.maxSpawnInterval:F1}]s, " +
            $"max {powerUpSpawner.Config.maxSimultaneous} simultáneos.");
    }
}
