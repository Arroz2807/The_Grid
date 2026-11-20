
using System;
using UnityEngine;

/// <summary>
/// Punto único de navegación del menú principal.
/// También notifica los cambios de panel a otros sistemas.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Paneles del menú")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameModePanel;
    [SerializeField] private GameObject enemySelectionPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject pvpPanel;

    [Header("Attract Mode")]
    [SerializeField] private GameObject attractModePanel;

    public event Action<GameObject> OnPanelChanged;

    public bool IsMainMenuVisible =>
        mainMenuPanel != null && mainMenuPanel.activeInHierarchy;

    private void ShowOnly(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogError(
                "[MainMenuManager] No se puede mostrar un panel porque " +
                "la referencia está vacía. Revisá el Inspector."
            );

            return;
        }

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(panel == mainMenuPanel);

        if (gameModePanel != null)
            gameModePanel.SetActive(panel == gameModePanel);

        if (enemySelectionPanel != null)
            enemySelectionPanel.SetActive(panel == enemySelectionPanel);

        if (creditsPanel != null)
            creditsPanel.SetActive(panel == creditsPanel);

        if (settingsPanel != null)
            settingsPanel.SetActive(panel == settingsPanel);

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(panel == howToPlayPanel);

        if (pvpPanel != null)
            pvpPanel.SetActive(panel == pvpPanel);

        if (attractModePanel != null)
            attractModePanel.SetActive(panel == attractModePanel);

        OnPanelChanged?.Invoke(panel);
    }

    // ------------------------------------------------------------
    // MAIN MENU
    // ------------------------------------------------------------

    public void OnPlayButtonClicked()
    {
        ShowOnly(gameModePanel);
    }

    public void OnCreditsButtonClicked() => ShowOnly(creditsPanel);

    public void OnSettingsButtonClicked() => ShowOnly(settingsPanel);

    public void OnHowToPlayButtonClicked() => ShowOnly(howToPlayPanel);

    public void OnBackFromHowToPlayButtonClicked() => ShowMainMenu();

    public void OnBackToMainMenuButtonClicked() => ShowMainMenu();

    public void OnQuitButtonClicked() => SceneLoader.Quit();

    public void OnRankingButtonClicked()
    {
        RankingSceneController.HighlightPosition = -1;

        RankingSceneController.SetReturnScene(
            SceneNames.MainMenu
        );

        SceneLoader.Load(SceneNames.Ranking);
    }

    // ------------------------------------------------------------
    // GAME MODE PANEL
    // ------------------------------------------------------------

    public void OnCustomModeSelected()
    {
        MatchConfig.SetGameMode(GameMode.Custom);
        ShowOnly(enemySelectionPanel);
    }

    public void OnWavesModeSelected()
    {
        MatchConfig.SetGameMode(GameMode.Waves);
        SceneLoader.Load(SceneNames.Game);
    }

    public void OnPvpModeSelected()
    {
        ShowOnly(pvpPanel);
    }

    public void OnBackFromGameModeButtonClicked() => ShowMainMenu();

    public void OnBackFromSelectionToGameModeButtonClicked()
    {
        ShowOnly(gameModePanel);
    }

    // ------------------------------------------------------------
    // ENEMY SELECTION
    // ------------------------------------------------------------

    public void OnStartMatchButtonClicked()
    {
        SceneLoader.Load(SceneNames.Game);
    }

    // ------------------------------------------------------------
    // PVP COMING SOON
    // ------------------------------------------------------------

    public void OnBackFromPvpButtonClicked()
    {
        ShowOnly(gameModePanel);
    }

    // ------------------------------------------------------------
    // ATTRACT MODE
    // ------------------------------------------------------------

    public void ShowAttractMode()
    {
        ShowOnly(attractModePanel);
    }

    public void ShowMainMenu()
    {
        ShowOnly(mainMenuPanel);
    }
}
