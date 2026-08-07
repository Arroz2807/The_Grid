using UnityEngine;

/// <summary>
/// Traduce los clicks de los botones del menú principal en acciones
/// concretas. Es deliberadamente chica y "tonta": no decide CÓMO se carga
/// una escena ni CÓMO se cierra la aplicación (eso es responsabilidad de
/// SceneLoader) — sólo sabe qué botón de ESTE menú corresponde a qué
/// acción. El día que exista un menú de pausa o de Game Over, cada uno va
/// a tener su propio manager igual de chico, reusando el mismo SceneLoader.
///
/// Los métodos son públicos y sin parámetros a propósito: son los que se
/// asignan desde el Inspector en el evento OnClick() de cada botón, y ese
/// mecanismo de Unity sólo lista métodos públicos sin parámetros (o con un
/// único parámetro de tipo simple) como opciones disponibles.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public void OnPlayButtonClicked()
    {
        SceneLoader.Load(SceneNames.Game);
    }

    public void OnQuitButtonClicked()
    {
        SceneLoader.Quit();
    }
}