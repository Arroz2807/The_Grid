using UnityEngine;

/// <summary>
/// Punto único de navegación del menú principal: qué panel se muestra y
/// cuándo se pasa a la escena de juego. No construye contenido de UI (eso
/// es EnemySelectionMenu) ni sabe cómo se carga una escena (eso es
/// SceneLoader) — sólo decide el flujo entre pantallas.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject enemySelectionPanel;

    public void OnPlayButtonClicked()
    {
        mainMenuPanel.SetActive(false);
        enemySelectionPanel.SetActive(true);
    }

    public void OnBackFromSelectionButtonClicked()
    {
        enemySelectionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OnStartMatchButtonClicked()
    {
        SceneLoader.Load(SceneNames.Game);
    }

    public void OnQuitButtonClicked()
    {
        SceneLoader.Quit();
    }
}