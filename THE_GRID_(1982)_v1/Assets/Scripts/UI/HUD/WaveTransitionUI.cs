using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Muestra la transición entre oleadas con fade in/out suave.
///
/// Sólo es dueña de los textos de la transición ("WAVE N COMPLETE" y el
/// countdown). El "+100" del bonus NO se escribe acá: lo muestra ScoreUI
/// a través de ScoreManager.OnGainEvent, que WaveManager dispara al
/// completar la oleada. Antes esta clase escribía el mismo TMP_Text que
/// ScoreUI (ScoreGained_Text) y le animaba el alpha, por lo que apagaba
/// los mensajes de puntos de todo el resto de la partida.
/// </summary>
public class WaveTransitionUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameObject transitionPanel;
    [SerializeField] private TMP_Text waveCompleteText;
    [SerializeField] private TMP_Text countdownText;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float panelMaxAlpha = 0f;

    [Header("Timing")]
    [SerializeField] private float completeDisplayTime = 1.5f;
    [SerializeField] private float countdownStepDuration = 0.8f;
    [SerializeField] private int countdownFrom = 3;

    private Image panelImage;
    private TMP_Text[] allTexts;

    private void Awake()
    {
        if (transitionPanel != null)
            panelImage = transitionPanel.GetComponent<Image>();

        allTexts = new TMP_Text[] { waveCompleteText, countdownText };
    }

    private void OnEnable()
    {
        WaveManager.OnWaveCompleted += HandleWaveCompleted;
        WaveManager.OnWavesGameOver += HandleWavesGameOver;
    }

    private void OnDisable()
    {
        WaveManager.OnWaveCompleted -= HandleWaveCompleted;
        WaveManager.OnWavesGameOver -= HandleWavesGameOver;
    }

    /// <summary>
    /// Si el jugador muere mientras la transición está en pantalla, se
    /// corta de inmediato: WaveManager sólo frena sus propias corrutinas,
    /// no las de esta clase, así que sin esto el countdown seguiría
    /// visible encima del panel de derrota.
    /// </summary>
    private void HandleWavesGameOver()
    {
        StopAllCoroutines();

        if (transitionPanel != null)
            transitionPanel.SetActive(false);

        // Dejar los textos en su estado inicial por si la transición
        // se cortó a mitad de camino.
        if (waveCompleteText != null)
            waveCompleteText.gameObject.SetActive(true);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        SetAlpha(0f);
    }

    private void Start()
    {
        SetAlpha(0f);
        if (transitionPanel != null)
            transitionPanel.SetActive(false);
    }

    private void HandleWaveCompleted(int waveNumber)
    {
        StartCoroutine(ShowTransition(waveNumber));
    }

    private IEnumerator ShowTransition(int completedWave)
    {
        if (waveCompleteText != null)
            waveCompleteText.text = $"WAVE {completedWave} COMPLETE";

        // El "+100" del bonus lo muestra ScoreUI desde OnGainEvent con el
        // texto "+100" y la etiqueta "Wave complete". No se toca acá para
        // no pisar el mensaje de puntos del jugador.

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        SetAlpha(0f);
        if (transitionPanel != null)
            transitionPanel.SetActive(true);

        yield return StartCoroutine(FadeRoutine(0f, 1f, fadeInDuration));
        yield return new WaitForSecondsRealtime(completeDisplayTime);

        if (waveCompleteText != null)
            waveCompleteText.gameObject.SetActive(false);
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        for (int i = countdownFrom; i > 0; i--)
        {
            if (countdownText != null)
                countdownText.text = i.ToString();
            yield return new WaitForSecondsRealtime(countdownStepDuration);
        }

        yield return StartCoroutine(FadeRoutine(1f, 0f, fadeOutDuration));

        if (transitionPanel != null)
            transitionPanel.SetActive(false);

        if (waveCompleteText != null)
            waveCompleteText.gameObject.SetActive(true);

        SetAlpha(0f);
        WaveManager.NotifyTransitionComplete();
    }

    private IEnumerator FadeRoutine(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }
        SetAlpha(toAlpha);
    }

    private void SetAlpha(float textAlpha)
    {
        if (panelImage != null)
        {
            Color c = panelImage.color;
            c.a = textAlpha * panelMaxAlpha;
            panelImage.color = c;
        }

        foreach (TMP_Text text in allTexts)
        {
            if (text == null) continue;
            Color c = text.color;
            c.a = textAlpha;
            text.color = c;
        }
    }
}