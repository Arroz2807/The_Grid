using UnityEngine;

/// <summary>
/// Punto único de navegación del menú principal: qué panel se muestra y
/// cuándo se pasa a la escena de juego.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject enemySelectionPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsPanel;

    private void ShowOnly(GameObject panel)
    {
        mainMenuPanel.SetActive(panel == mainMenuPanel);
        enemySelectionPanel.SetActive(panel == enemySelectionPanel);
        creditsPanel.SetActive(panel == creditsPanel);
        settingsPanel.SetActive(panel == settingsPanel);
    }

    public void OnPlayButtonClicked()
    {
        ShowOnly(enemySelectionPanel);
    }

    public void OnCreditsButtonClicked()
    {
        ShowOnly(creditsPanel);
    }

    public void OnSettingsButtonClicked()
    {
        ShowOnly(settingsPanel);
    }

    public void OnBackToMainMenuButtonClicked()
    {
        ShowOnly(mainMenuPanel);
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