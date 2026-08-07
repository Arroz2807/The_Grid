using System.Collections;
using UnityEngine;

/// <summary>
/// Muestra el panel de Game Over cuando un jugador muere, y traduce sus
/// botones (Reintentar, Volver al menú) en acciones concretas. Vive en la
/// escena del juego, no en una escena aparte. Igual que GameManager y
/// DeathExplosionSpawner, es un oyente más de
/// LightCycleController.OnAnyPlayerDied, completamente independiente de
/// los otros dos — incluido el hecho de que este demore su reacción unos
/// segundos no afecta en nada a los demás oyentes del mismo evento.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [Tooltip("Panel de Game Over completo (título, botones, estadísticas). Debe empezar inactivo en la escena.")]
    [SerializeField] private GameObject gameOverPanel;

    [Tooltip("Segundos de espera antes de mostrar el panel, para no tapar la explosión de partículas.")]
    [SerializeField] private float panelRevealDelay = 1f;

    private bool gameOverTriggered;

    private void OnEnable()
    {
        LightCycleController.OnAnyPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        LightCycleController.OnAnyPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied(LightCycleController player)
    {
        // Guard simple: si el evento llegara a dispararse más de una vez
        // (por ejemplo, más adelante con varios jugadores), no queremos
        // apilar corrutinas ni reiniciar la cuenta regresiva a mitad de
        // camino.
        if (gameOverTriggered) return;
        gameOverTriggered = true;

        StartCoroutine(ShowPanelAfterDelay());
    }

    private IEnumerator ShowPanelAfterDelay()
    {
        yield return new WaitForSecondsRealtime(panelRevealDelay);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Acá es donde, más adelante, se completarían las estadísticas de
        // la partida (tiempo sobrevivido, celdas recorridas, etc.) antes
        // o al momento de mostrar el panel.
    }

    /// <summary>
    /// Asignado al botón "Reintentar". Recargar la escena actual —en vez
    /// de resetear el estado a mano— da un reinicio 100% limpio, gratis.
    /// </summary>
    public void OnRetryButtonClicked()
    {
        SceneLoader.Load(SceneNames.Game);
    }

    /// <summary>
    /// Asignado al botón "Volver al menú".
    /// </summary>
    public void OnMainMenuButtonClicked()
    {
        SceneLoader.Load(SceneNames.MainMenu);
    }
}