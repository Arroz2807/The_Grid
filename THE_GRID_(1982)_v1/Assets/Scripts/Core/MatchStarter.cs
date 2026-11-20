
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Coordina el inicio de la partida: countdown, creación de entidades,
/// música y aceleración progresiva.
///
/// Si se regresa desde Ranking a Waves Game Over, omite el inicio
/// de partida para permitir restaurar el panel de resultados.
/// </summary>
public class MatchStarter : MonoBehaviour
{
    [Header("Countdown")]
    [Tooltip("Números que se muestran antes de arrancar.")]
    [SerializeField] private int countdownFrom = 3;

    [Tooltip("Segundos que dura cada número del countdown.")]
    [SerializeField] private float countdownStepDuration = 1f;

    [SerializeField] private TMP_Text countdownText;

    [Tooltip("Panel semitransparente del countdown.")]
    [SerializeField] private GameObject countdownPanel;

    [Header("Aceleración inicial")]
    [Tooltip("Intervalo de movimiento inicial.")]
    [SerializeField] private float startMoveInterval = 0.45f;

    [Tooltip("Duración de la aceleración progresiva.")]
    [SerializeField] private float rampUpDuration = 3f;

    public static event Action OnMatchReady;

    private readonly List<LightCycleController> entities =
        new List<LightCycleController>();

    private readonly Dictionary<LightCycleController, float> targetIntervals =
        new Dictionary<LightCycleController, float>();

    private void Start()
    {
        // Al volver desde Ranking, la escena Game se recarga,
        // pero no debe comenzar una partida nueva.
        if (WavesGameOverSession.ConsumeSkipNextMatchStart())
        {
            if (countdownText != null)
                countdownText.gameObject.SetActive(false);

            if (countdownPanel != null)
                countdownPanel.SetActive(false);

            Debug.Log(
                "[MatchStarter] Inicio omitido: restaurando Waves Game Over."
            );

            return;
        }

        StartCoroutine(CountdownRoutine());
    }

    /// <summary>
    /// Registra una entidad para aplicar el ramp-up.
    /// </summary>
    public void RegisterEntity(LightCycleController entity)
    {
        if (entity == null)
            return;

        targetIntervals[entity] = entity.MoveInterval;
        entities.Add(entity);
    }

    private IEnumerator CountdownRoutine()
    {
        // Permitir que SceneMusicPlayer registre la playlist.
        yield return null;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayPlaylist();
        }
        else
        {
            Debug.LogWarning(
                "[MatchStarter] No se encontró AudioManager. " +
                "No se pudo iniciar la playlist de gameplay.",
                this
            );
        }

        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        for (int i = countdownFrom; i > 0; i--)
        {
            if (countdownText != null)
                countdownText.text = i.ToString();

            yield return new WaitForSeconds(countdownStepDuration);
        }

        if (countdownText != null)
            countdownText.text = "GO!";

        // GameManager crea las entidades.
        OnMatchReady?.Invoke();

        foreach (LightCycleController entity in entities)
        {
            if (entity != null)
                entity.SetMoveInterval(startMoveInterval);
        }

        yield return new WaitForSeconds(0.5f);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

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
                if (entity == null ||
                    !targetIntervals.TryGetValue(entity, out float target))
                {
                    continue;
                }

                entity.SetMoveInterval(
                    Mathf.Lerp(startMoveInterval, target, t)
                );
            }

            yield return null;
        }

        // Aplicar los intervalos normales exactos.
        foreach (LightCycleController entity in entities)
        {
            if (entity == null ||
                !targetIntervals.TryGetValue(entity, out float target))
            {
                continue;
            }

            entity.SetMoveInterval(target);
        }
    }
}
