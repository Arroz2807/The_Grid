
using UnityEngine;

/// <summary>
/// Identifica el tipo de playlist que utiliza una escena.
/// </summary>
public enum SceneMusicType
{
    Menu,
    Gameplay
}

/// <summary>
/// Declara qué playlist utiliza una escena.
///
/// SceneMusicPlayer registra las canciones en AudioManager.
/// Las escenas de menú inician su música automáticamente.
/// La escena de gameplay espera a que MatchStarter solicite
/// la transición durante el countdown.
/// </summary>
public class SceneMusicPlayer : MonoBehaviour
{
    [Header("Tipo de música")]
    [SerializeField] private SceneMusicType musicType;

    [Header("Playlist de la escena")]
    [Tooltip("Canciones que se reproducirán aleatoriamente. " +
             "Al terminar una, AudioManager seleccionará otra.")]
    [SerializeField] private AudioClip[] musicPlaylist;

    private void Start()
    {
        if (musicPlaylist == null || musicPlaylist.Length == 0)
        {
            Debug.LogWarning(
                $"SceneMusicPlayer ({gameObject.name}): " +
                "no hay canciones asignadas a la playlist.",
                this
            );

            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning(
                $"SceneMusicPlayer ({gameObject.name}): " +
                "no se encontró una instancia de AudioManager.",
                this
            );

            return;
        }

        // Registrar la playlist para que AudioManager pueda utilizarla.
        AudioManager.Instance.RegisterScenePlaylist(
            musicType,
            musicPlaylist
        );

        // Las escenas de menú mantienen su comportamiento actual.
        // Gameplay espera al countdown de MatchStarter.
        if (musicType == SceneMusicType.Menu)
        {
            AudioManager.Instance.PlayScenePlaylist(musicType);
        }
    }
}
