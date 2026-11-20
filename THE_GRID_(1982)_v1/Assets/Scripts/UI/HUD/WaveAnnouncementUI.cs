using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Anuncia el comienzo de cada oleada y, cuando aparece por primera vez un
/// tipo de enemigo, lo presenta con su nombre y su color.
///
/// Es la contraparte de WaveTransitionUI: aquélla se ocupa del final de la
/// oleada, ésta del comienzo. Ambas escuchan eventos de WaveManager, así
/// que no necesitan que nadie las llame desde el código de juego.
/// </summary>
public class WaveAnnouncementUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private TMP_Text announcementText;
    [SerializeField] private WaveProgressionConfig progressionConfig;

    [Header("Tiempos")]
    [Tooltip("Espera antes de mostrar el cartel. Deja terminar el GO! del countdown inicial.")]
    [SerializeField] private float initialDelay = 0.5f;

    [Tooltip("Segundos que el cartel queda visible.")]
    [SerializeField] private float displayDuration = 1.6f;

    [Tooltip("Duración del fade de entrada y salida.")]
    [SerializeField] private float fadeDuration = 0.25f;

    private Coroutine announcementRoutine;
    private Color textColor = Color.white;

    private void Awake()
    {
        // En Custom no hay oleadas: este cartel no tiene nada que anunciar.
        if (MatchConfig.GameMode != GameMode.Waves)
        {
            gameObject.SetActive(false);
            return;
        }

        if (announcementText != null)
        {
            textColor = announcementText.color;
            SetAlpha(0f);
        }
    }

    private void OnEnable()
    {
        WaveManager.OnWaveStarted += HandleWaveStarted;
        WaveManager.OnWavesGameOver += HandleWavesGameOver;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveStarted -= HandleWaveStarted;
        WaveManager.OnWavesGameOver -= HandleWavesGameOver;
    }

    private void HandleWaveStarted(int waveNumber)
    {
        if (announcementRoutine != null)
        {
            StopCoroutine(announcementRoutine);
        }

        announcementRoutine = StartCoroutine(ShowAnnouncement(waveNumber));
    }

    private void HandleWavesGameOver()
    {
        if (announcementRoutine != null)
        {
            StopCoroutine(announcementRoutine);
            announcementRoutine = null;
        }

        SetAlpha(0f);
    }

    private IEnumerator ShowAnnouncement(int waveNumber)
    {
        yield return new WaitForSecondsRealtime(initialDelay);

        if (announcementText == null)
        {
            announcementRoutine = null;
            yield break;
        }

        announcementText.text = BuildMessage(waveNumber);

        yield return StartCoroutine(FadeTo(1f, fadeDuration));
        yield return new WaitForSecondsRealtime(displayDuration);
        yield return StartCoroutine(FadeTo(0f, fadeDuration));

        announcementRoutine = null;
    }

    private string BuildMessage(int waveNumber)
    {
        string message = $"WAVE {waveNumber}";

        EnemyType? newEnemy = GetNewEnemyType(waveNumber);

        if (newEnemy.HasValue)
        {
            string name = newEnemy.Value.ToString().ToUpperInvariant();
            string color = GetEnemyColorHex(newEnemy.Value);

            message +=
                $"\n<size=45%><color={color}>NEW: {name}</color></size>";
        }

        return message;
    }

    /// <summary>
    /// Devuelve el primer tipo de enemigo que aparece en esta oleada y no
    /// estaba en ninguna anterior. Null si no hay ninguno.
    /// </summary>
    private EnemyType? GetNewEnemyType(int waveNumber)
    {
        if (progressionConfig == null)
            return null;

        HashSet<EnemyType> previousTypes = new HashSet<EnemyType>();

        for (int wave = 1; wave < waveNumber; wave++)
        {
            WaveEnemyConfig previous = progressionConfig.GetWaveConfig(wave);

            if (previous == null || previous.enemyTypes == null)
                continue;

            foreach (EnemyType type in previous.enemyTypes)
            {
                previousTypes.Add(type);
            }
        }

        WaveEnemyConfig current = progressionConfig.GetWaveConfig(waveNumber);

        if (current == null || current.enemyTypes == null)
            return null;

        foreach (EnemyType type in current.enemyTypes)
        {
            if (!previousTypes.Contains(type))
                return type;
        }

        return null;
    }

    private static string GetEnemyColorHex(EnemyType type)
    {
        switch (type)
        {
            // Coinciden con GameManager.GetEnemyColor() y con la leyenda
            // del panel CÓMO JUGAR.
            case EnemyType.Chase: return "#FF0000";
            case EnemyType.Predict: return "#FFA500";
            case EnemyType.Ambush: return "#FF00FF";
            case EnemyType.Random: return "#008000";
            default: return "#FFFFFF";
        }
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = announcementText.color.a;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (announcementText == null)
            return;

        Color color = textColor;
        color.a = alpha;
        announcementText.color = color;
    }
}
