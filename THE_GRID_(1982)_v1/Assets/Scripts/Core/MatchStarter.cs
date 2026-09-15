using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Coordina el inicio de la partida: muestra un countdown, avisa a
/// GameManager que cree las entidades, y luego las acelera progresivamente
/// desde una velocidad inicial hasta la velocidad configurada en cada
/// prefab. Vive en la escena de juego. No sabe nada de IA ni de grilla —
/// sólo controla el timing y la velocidad.
/// </summary>
public class MatchStarter : MonoBehaviour
{
    [Header("Countdown")]
    [Tooltip("Números que se muestran antes de arrancar (ej. 3, 2, 1).")]
    [SerializeField] private int countdownFrom = 3;
    [Tooltip("Segundos que dura cada número del countdown.")]
    [SerializeField] private float countdownStepDuration = 1f;
    [SerializeField] private TMP_Text countdownText;
    [Tooltip("Panel semitransparente que cubre la pantalla durante el countdown. Se desactiva junto con el texto al terminar.")]
    [SerializeField] private GameObject countdownPanel;

    [Header("Aceleración inicial")]
    [Tooltip("Intervalo de movimiento (seg/paso) con el que arrancan todas las entidades. Mayor = más lento.")]
    [SerializeField] private float startMoveInterval = 0.45f;
    [Tooltip("Segundos que tarda la aceleración desde startMoveInterval hasta el intervalo normal de cada entidad.")]
    [SerializeField] private float rampUpDuration = 3f;

    /// <summary>
    /// GameManager se suscribe acá. Cuando se dispara, debe instanciar
    /// jugador y enemigos y devolver sus LightCycleController en la lista.
    /// El diseño de callback con lista evita que MatchStarter necesite
    /// conocer prefabs ni GridManager.
    /// </summary>
    public static event Action OnMatchReady;

    // Lista de entidades sobre las que MatchStarter va a aplicar el
    // ramp-up. GameManager la llena llamando a RegisterEntity() por cada
    // LightCycleController que cree.
    private readonly List<LightCycleController> entities = new List<LightCycleController>();

    // Intervalo "normal" de cada entidad, leído ANTES de pisarlo con
    // startMoveInterval. Es el destino del ramp-up.
    private readonly Dictionary<LightCycleController, float> targetIntervals =
        new Dictionary<LightCycleController, float>();

    private void Start()
    {
        StartCoroutine(CountdownRoutine());
    }

    /// <summary>
    /// GameManager llama a esto por cada entidad que instancia al recibir
    /// OnMatchReady, para que MatchStarter pueda aplicarles el ramp-up.
    /// </summary>
    public void RegisterEntity(LightCycleController entity)
    {
        if (entity != null)
        {
            // Guardamos el intervalo normal AQUÍ, antes de que
            // CountdownRoutine lo pise con startMoveInterval.
            targetIntervals[entity] = entity.MoveInterval;
            entities.Add(entity);
        }
    }

    private IEnumerator CountdownRoutine()
    {
        if (countdownPanel != null) countdownPanel.SetActive(true);
        if (countdownText != null) countdownText.gameObject.SetActive(true);

        for (int i = countdownFrom; i > 0; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();
            yield return new WaitForSeconds(countdownStepDuration);
        }

        if (countdownText != null) countdownText.text = "GO!";

        // Avisamos a GameManager que cree las entidades. Las entidades
        // existen a partir de este punto, no antes — así no hay ningún
        // input que bloquear durante el countdown.
        OnMatchReady?.Invoke();

        // Arrancamos todas las entidades en velocidad lenta.
        foreach (LightCycleController entity in entities)
        {
            entity.SetMoveInterval(startMoveInterval);
        }

        yield return new WaitForSeconds(0.5f);

        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (countdownPanel != null) countdownPanel.SetActive(false);

        // Ramp-up: interpolamos linealmente de startMoveInterval al
        // intervalo normal (el que tiene configurado cada entidad en su
        // prefab, que MatchStarter leyó antes de pisar con startMoveInterval).
        yield return StartCoroutine(RampUpRoutine());
    }

    private IEnumerator RampUpRoutine()
    {
        float elapsed = 0f;

        while (elapsed < rampUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rampUpDuration);

            foreach (LightCycleController entity in entities)
            {
                float target = targetIntervals[entity];
                // Interpolamos de lento (startMoveInterval) a rápido
                // (target). Como un intervalo menor = más rápido, usamos
                // Lerp de start a target (que es menor).
                entity.SetMoveInterval(Mathf.Lerp(startMoveInterval, target, t));
            }

            yield return null;
        }

        // Forzamos el valor exacto al terminar, sin error de float.
        foreach (LightCycleController entity in entities)
        {
            entity.SetMoveInterval(targetIntervals[entity]);
        }
    }
}