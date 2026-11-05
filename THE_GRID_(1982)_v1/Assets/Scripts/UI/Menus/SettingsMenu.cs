using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arma la lista de opciones navegables del panel de configuración y se
/// la entrega a MenuNavigator.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private MenuNavigator navigator;
    [SerializeField] private NavigableSlider musicSlider;
    [SerializeField] private NavigableSlider sfxSlider;
    [SerializeField] private NavigableButton resetRankingsButton;
    [SerializeField] private NavigableButton backButton;

    private void OnEnable()
    {
        List<INavigableOption> options = new List<INavigableOption>
        {
            musicSlider,
            sfxSlider,
            resetRankingsButton,
            backButton
        };

        navigator.SetOptions(options);
    }

    /// <summary>
    /// Cableado en el OnClick() del botón Reset Rankings en el Inspector.
    /// </summary>
    public void OnResetRankingsButtonClicked()
    {
        RankingManager.ClearRanking();
        Debug.Log("[SettingsMenu] Rankings reseteados.");
    }
}