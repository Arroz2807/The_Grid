using UnityEngine;

/// <summary>
/// Punto único de navegación del menú principal: qué panel se muestra y
/// cuándo se pasa a la escena de juego.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameModePanel;
    [SerializeField] private GameObject enemySelectionPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject pvpPanel;

    private void ShowOnly(GameObject panel)
    {
        mainMenuPanel.SetActive(panel == mainMenuPanel);
        gameModePanel.SetActive(panel == gameModePanel);
        enemySelectionPanel.SetActive(panel == enemySelectionPanel);
        creditsPanel.SetActive(panel == creditsPanel);
        settingsPanel.SetActive(panel == settingsPanel);
        pvpPanel.SetActive(panel == pvpPanel);
    }

    // ── Main Menu ──────────────────────────────────────────────────────

    public void OnPlayButtonClicked()
    {
        ShowOnly(gameModePanel);
    }

    public void OnCreditsButtonClicked() => ShowOnly(creditsPanel);

    public void OnSettingsButtonClicked() => ShowOnly(settingsPanel);

    public void OnBackToMainMenuButtonClicked() => ShowOnly(mainMenuPanel);

    public void OnQuitButtonClicked() => SceneLoader.Quit();

    public void OnRankingButtonClicked()
    {
        RankingSceneController.HighlightPosition = -1;
        SceneLoader.Load(SceneNames.Ranking);
    }

    // ── Game Mode Panel ────────────────────────────────────────────────

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

    public void OnBackFromGameModeButtonClicked() => ShowOnly(mainMenuPanel);

    public void OnBackFromSelectionToGameModeButtonClicked() => ShowOnly(gameModePanel);

    // ── Enemy Selection ────────────────────────────────────────────────

    public void OnStartMatchButtonClicked()
    {
        SceneLoader.Load(SceneNames.Game);
    }

    // ── PvP Coming Soon ────────────────────────────────────────────────

    public void OnBackFromPvpButtonClicked() => ShowOnly(gameModePanel);
}