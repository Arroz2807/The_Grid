using UnityEngine;

/// <summary>
/// Declara "esta escena usa esta música". Un GameObject con este
/// componente por escena (MainMenu, Game) alcanza — así AudioManager
/// nunca necesita saber nombres de escena ni un mapeo centralizado.
/// </summary>
public class SceneMusicPlayer : MonoBehaviour
{
    [SerializeField] private AudioClip musicClip;

    private void Start()
    {
        AudioManager.Instance?.PlayMusic(musicClip);
    }
}