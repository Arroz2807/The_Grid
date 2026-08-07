using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Punto único para cargar escenas y cerrar la aplicación. Sin esta clase,
/// tanto MainMenuManager como, más adelante, un menú de pausa o de Game
/// Over, terminarían llamando a SceneManager.LoadScene(...) por su cuenta
/// — código duplicado que además repetiría el mismo parche de
/// "Application.Quit no funciona en el Editor" en cada lugar que lo
/// necesite.
///
/// Es una clase estática, no un MonoBehaviour: no tiene ningún estado
/// propio ni necesita vivir en un GameObject de la escena — sólo envuelve
/// llamadas a APIs de Unity que ya son estáticas (SceneManager,
/// Application). Convertirla en un componente obligaría a cada botón a
/// buscarla en la escena antes de poder usarla, sin ninguna ventaja a
/// cambio.
/// </summary>
public static class SceneLoader
{
    /// <summary>
    /// Carga la escena indicada, reemplazando la actual por completo.
    /// </summary>
    public static void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Cierra la aplicación. Application.Quit() está documentado por
    /// Unity como una llamada que no hace nada dentro del Editor — por
    /// eso, en el Editor, detenemos el modo Play en su lugar, que es el
    /// equivalente real a "cerrar el juego" mientras se está probando.
    /// La directiva #if UNITY_EDITOR hace que la rama de EditorApplication
    /// ni siquiera se compile en un build real (UnityEditor no existe ahí),
    /// así que no hay riesgo de que esto rompa una build final.
    /// </summary>
    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}