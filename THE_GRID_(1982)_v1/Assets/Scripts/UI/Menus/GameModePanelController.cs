using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arma la lista de opciones navegables del panel de selección de modo y
/// se la entrega a MenuNavigator. Mismo patrón que SettingsMenu — una
/// clase chica que sólo sabe "qué opciones tiene este panel y en qué
/// orden", nada más.
/// </summary>
public class GameModePanelController : MonoBehaviour
{
    [SerializeField] private MenuNavigator navigator;
    [SerializeField] private NavigableButton customButton;
    [SerializeField] private NavigableButton wavesButton;

    // PvP como botón separado de tipo especial: visualmente existe pero
    // con color apagado para indicar que no está disponible. Funciona con
    // el mismo NavigableButton — OnConfirm() abre el panel "Coming Soon"
    // en vez de iniciar una partida.
    [SerializeField] private NavigableButton pvpButton;
    [SerializeField] private NavigableButton backButton;

    private void OnEnable()
    {
        List<INavigableOption> options = new List<INavigableOption>
        {
            customButton,
            wavesButton,
            pvpButton,
            backButton
        };

        // Custom = índice 0, Back = índice 3
        navigator.SetOptions(options, initialIndex: 0);
    }
}