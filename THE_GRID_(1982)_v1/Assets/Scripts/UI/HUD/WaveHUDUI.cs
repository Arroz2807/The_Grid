using TMPro;
using UnityEngine;

/// <summary>
/// Muestra la oleada actual en el HUD durante el modo Waves.
///
/// Escucha WaveManager.OnWaveStarted, que ya existía pero no tenía ningún
/// consumidor. No muestra la cantidad de enemigos: sólo el número de
/// oleada, que es la referencia que necesita el jugador para entender
/// cuánto avanzó.
/// </summary>
public class WaveHUDUI : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;

    private void Awake()
    {
        // En Custom no hay oleadas: el HUD no tiene nada que mostrar.
        if (MatchConfig.GameMode != GameMode.Waves)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        WaveManager.OnWaveStarted += HandleWaveStarted;
        WaveManager.OnWavesGameOver += HandleWavesGameOver;

        if (waveText != null)
        {
            waveText.text = string.Empty;
        }
    }

    private void OnDisable()
    {
        WaveManager.OnWaveStarted -= HandleWaveStarted;
        WaveManager.OnWavesGameOver -= HandleWavesGameOver;
    }

    private void HandleWaveStarted(int waveNumber)
    {
        if (waveText != null)
        {
            waveText.text = $"WAVE {waveNumber:00}";
        }
    }

    private void HandleWavesGameOver()
    {
        if (waveText != null)
        {
            waveText.text = string.Empty;
        }
    }
}
