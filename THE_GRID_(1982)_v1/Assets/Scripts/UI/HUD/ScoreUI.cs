
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra el score total en scoreText.
///
/// Los eventos notables (kills, power-ups y oleadas) se encolan
/// en gainText como "+XXX", con una etiqueta descriptiva opcional.
///
/// El movimiento actualiza scoreText silenciosamente.
///
/// La cola conserva el mensaje actual si el objeto se desactiva
/// temporalmente y continúa mostrándolo cuando vuelve a activarse.
/// </summary>
[DefaultExecutionOrder(-50)]
public class ScoreUI : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Mensajes de puntos")]
    [Tooltip("Texto que muestra los puntos ganados, por ejemplo: +25.")]
    [SerializeField] private TMP_Text gainText;

    [Tooltip("Etiqueta descriptiva del evento. Puede ser null.")]
    [SerializeField] private TMP_Text gainLabelText;

    [Tooltip("Segundos que cada mensaje permanece visible.")]
    [SerializeField] private float gainDisplayTime = 1.2f;

    // Mensajes pendientes de mostrar.
    private readonly Queue<(int points, string label)> messageQueue =
        new Queue<(int, string)>();

    // Referencia a la corrutina que procesa la cola.
    private Coroutine displayCoroutine;

    // Indica si Start ya inicializó el marcador.
    private bool isInitialized;

    // Evita activar accidentalmente el marcador en modo Custom.
    private bool isWavesMode;

    // Color original de los textos de puntos, tal como está configurado en
    // el Inspector. Se guarda para poder restaurarlo antes de cada mensaje:
    // si otro sistema escala el alpha (WaveTransitionUI lo hacía con
    // ScoreGained_Text), el mensaje quedaba invisible aunque el GameObject
    // estuviera activo.
    private Color gainColor = Color.white;
    private Color gainLabelColor = Color.white;

    private void Awake()
    {
        isWavesMode = MatchConfig.GameMode == GameMode.Waves;

        // Guardamos el color ANTES de ocultar nada: HideGainTexts sólo
        // desactiva GameObjects, pero el orden deja la intención explícita.
        if (gainText != null)
        {
            gainColor = gainText.color;
        }

        if (gainLabelText != null)
        {
            gainLabelColor = gainLabelText.color;
        }

        // Ocultamos los mensajes desde Awake para evitar que aparezcan
        // durante un frame antes de la inicialización de Start.
        HideGainTexts();

        if (!isWavesMode)
        {
            gameObject.SetActive(false);
            return;
        }

        // El marcador comienza desde cero al iniciar una partida Waves.
        ScoreManager.Reset();

        // Suscripción a los eventos del sistema de puntuación.
        ScoreManager.OnScoreChanged += HandleScoreChanged;
        ScoreManager.OnGainEvent += HandleGainEvent;

        // Ocultamos el marcador cuando termina la partida.
        GameManager.OnMatchEnded += HandleMatchEnded;
    }

    private void OnEnable()
    {
        // Si el objeto se reactiva accidentalmente en modo Custom,
        // volvemos a ocultarlo.
        if (!isWavesMode)
        {
            gameObject.SetActive(false);
            return;
        }

        // Si había mensajes pendientes antes de desactivarse,
        // reanudamos su procesamiento.
        TryStartDisplayQueue();
    }

    private void Start()
    {
        UpdateDisplay(ScoreManager.TotalScore);

        // A partir de este momento podemos procesar mensajes.
        isInitialized = true;

        TryStartDisplayQueue();
    }

    private void OnDisable()
    {
        // Unity detiene las corrutinas cuando se desactiva el objeto.
        // Detenemos explícitamente la nuestra y limpiamos su referencia.
        StopDisplayCoroutine();

        // Ocultamos los textos, pero NO vaciamos la cola.
        // Así podemos recuperar los mensajes si el objeto vuelve
        // a activarse durante la partida.
        HideGainTexts();
    }

    private void OnDestroy()
    {
        // Evitamos dejar suscripciones a eventos estáticos.
        ScoreManager.OnScoreChanged -= HandleScoreChanged;
        ScoreManager.OnGainEvent -= HandleGainEvent;

        GameManager.OnMatchEnded -= HandleMatchEnded;
    }

    /// <summary>
    /// Actualiza el marcador cuando cambia el puntaje total.
    /// </summary>
    private void HandleScoreChanged(int newTotal)
    {
        UpdateDisplay(newTotal);
    }

    /// <summary>
    /// Recibe un evento de puntos y lo agrega a la cola.
    /// </summary>
    private void HandleGainEvent(int points, string label)
    {
        if (gainText == null)
        {
            Debug.LogError(
                "[ScoreUI] No hay ningún TMP_Text asignado a gainText.",
                this
            );

            return;
        }

        // Guardamos el evento para mostrarlo cuando corresponda.
        messageQueue.Enqueue((points, label));

        // Si la UI está activa y no hay una corrutina procesando
        // mensajes, comenzamos a mostrar la cola.
        TryStartDisplayQueue();
    }

    /// <summary>
    /// Inicia el procesamiento de la cola cuando las condiciones
    /// necesarias se cumplen.
    /// </summary>
    private void TryStartDisplayQueue()
    {
        if (!isWavesMode) return;
        if (!isInitialized) return;
        if (!gameObject.activeInHierarchy) return;
        if (messageQueue.Count == 0) return;
        if (displayCoroutine != null) return;

        displayCoroutine = StartCoroutine(DrainQueue());
    }

    /// <summary>
    /// Actualiza visualmente el puntaje total.
    /// </summary>
    private void UpdateDisplay(int total)
    {
        if (scoreText != null)
        {
            scoreText.text = total.ToString("D6");
        }
    }

    /// <summary>
    /// Procesa los mensajes pendientes uno por uno.
    ///
    /// Utiliza Peek() para consultar el mensaje actual sin quitarlo
    /// inmediatamente de la cola. Sólo lo elimina después de mostrarlo.
    ///
    /// Esto permite conservar el mensaje si la corrutina se interrumpe
    /// por una desactivación temporal del objeto.
    /// </summary>
    private IEnumerator DrainQueue()
    {
        while (messageQueue.Count > 0)
        {
            // Consultamos el primer mensaje sin eliminarlo.
            (int points, string label) = messageQueue.Peek();

            // Restauramos el color configurado antes de mostrar. Un alpha
            // externo (por ejemplo, una transición de oleada) dejaría el
            // texto activo pero completamente transparente.
            gainText.color = gainColor;
            gainText.text = $"+{points}";
            gainText.gameObject.SetActive(true);

            // La etiqueta es opcional.
            if (gainLabelText != null)
            {
                gainLabelText.color = gainLabelColor;
                gainLabelText.text = label;
                gainLabelText.gameObject.SetActive(true);
            }

            // Esperamos usando tiempo real, independientemente
            // de Time.timeScale.
            yield return new WaitForSecondsRealtime(gainDisplayTime);

            // Ocultamos ambos textos.
            HideGainTexts();

            // El mensaje se elimina únicamente después de mostrarse.
            if (messageQueue.Count > 0)
            {
                messageQueue.Dequeue();
            }
        }

        // La cola terminó de procesarse.
        displayCoroutine = null;
    }

    /// <summary>
    /// Oculta el texto de puntos y su etiqueta.
    /// </summary>
    private void HideGainTexts()
    {
        if (gainText != null)
        {
            gainText.gameObject.SetActive(false);
        }

        if (gainLabelText != null)
        {
            gainLabelText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Detiene la corrutina actual y limpia su referencia.
    /// No modifica la cola de mensajes.
    /// </summary>
    private void StopDisplayCoroutine()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }
    }

    /// <summary>
    /// Se ejecuta cuando termina la partida.
    /// No deben quedar mensajes pendientes ni una corrutina activa.
    /// </summary>
    private void HandleMatchEnded(bool playerWon)
    {
        messageQueue.Clear();

        StopDisplayCoroutine();
        HideGainTexts();

        // Ocultamos todo el marcador al finalizar la partida.
        gameObject.SetActive(false);
    }
}
