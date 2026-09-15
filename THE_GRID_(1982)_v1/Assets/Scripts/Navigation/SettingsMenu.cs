using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arma la lista de opciones navegables del panel de configuración y se
/// la entrega a MenuNavigator. Sigue exactamente el mismo patrón que
/// EnemySelectionMenu, pero con una lista fija (sin elementos dinámicos).
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
}