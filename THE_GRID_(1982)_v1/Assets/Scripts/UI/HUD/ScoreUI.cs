using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra el score total en scoreText. Los eventos notables (kills,
/// power-ups, oleadas) se encolan en gainText como "+XXX" con una etiqueta
/// descriptiva chica debajo. El movimiento actualiza scoreText
/// silenciosamente sin pasar por gainText.
/// </summary>
[DefaultExecutionOrder(-50)] // Antes que PowerUpSpawner y WaveManager
public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;
    [Tooltip("'+XXX' — se muestra brevemente por cada evento notable.")]
    [SerializeField] private TMP_Text gainText;
    [Tooltip("Etiqueta descriptiva del evento ('Enemy killed', etc.). Puede ser null.")]
    [SerializeField] private TMP_Text gainLabelText;
    [Tooltip("Segundos que cada mensaje permanece visible.")]
    [SerializeField] private float gainDisplayTime = 1.2f;

    private readonly Queue<(int points, string label)> messageQueue
        = new Queue<(int, string)>();

    private Coroutine displayCoroutine;

    private void Awake()
    {
        // MatchConfig.GameMode ya tiene el valor correcto al entrar a la
        // escena de juego — se seteó en el menú antes de SceneLoader.Load.
        // Si es Custom, desactivamos el GameObject entero antes del primer
        // frame visible: el jugador nunca ve el marcador.
        if (MatchConfig.GameMode != GameMode.Waves)
        {
            gameObject.SetActive(false);
            return;
        }

        ScoreManager.Reset();
        ScoreManager.OnScoreChanged += HandleScoreChanged;
        ScoreManager.OnGainEvent += HandleGainEvent;
        GameManager.OnMatchEnded += HandleMatchEnded;
    }

    private void OnEnable()
    {
        // Doble garantía: si por algún motivo el GameObject se reactiva
        // en modo Custom (por ejemplo, desde el Editor durante Play),
        // lo volvemos a ocultar de inmediato.
        if (MatchConfig.GameMode != GameMode.Waves)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        ScoreManager.OnScoreChanged -= HandleScoreChanged;
        ScoreManager.OnGainEvent -= HandleGainEvent;
        GameManager.OnMatchEnded -= HandleMatchEnded;
    }

    private void HandleMatchEnded(bool playerWon)
    {
        // Ocultar el ScoreUI apenas termina la partida — antes de que
        // aparezca cualquier panel de Game Over.
        gameObject.SetActive(false);
    }

    private void Start()
    {
        UpdateDisplay(ScoreManager.TotalScore);
        if (gainText != null) gainText.gameObject.SetActive(false);
        if (gainLabelText != null) gainLabelText.gameObject.SetActive(false);
    }

    private void HandleScoreChanged(int newTotal)
    {
        UpdateDisplay(newTotal);
    }

    private void HandleGainEvent(int points, string label)
    {
        messageQueue.Enqueue((points, label));
        if (displayCoroutine == null)
            displayCoroutine = StartCoroutine(DrainQueue());
    }

    private void UpdateDisplay(int total)
    {
        if (scoreText != null)
            scoreText.text = total.ToString("D6");
    }

    private IEnumerator DrainQueue()
    {
        while (messageQueue.Count > 0)
        {
            (int points, string label) = messageQueue.Dequeue();

            if (gainText != null)
            {
                gainText.text = $"+{points}";
                gainText.gameObject.SetActive(true);
            }

            if (gainLabelText != null)
            {
                gainLabelText.text = label;
                gainLabelText.gameObject.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(gainDisplayTime);

            if (gainText != null) gainText.gameObject.SetActive(false);
            if (gainLabelText != null) gainLabelText.gameObject.SetActive(false);
        }

        displayCoroutine = null;
    }
}