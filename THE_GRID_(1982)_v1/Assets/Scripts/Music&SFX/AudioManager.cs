using UnityEngine;

/// <summary>
/// Único punto de reproducción de audio del juego. Sabe CÓMO reproducir
/// (qué AudioSource usar, a qué volumen) pero no CUÁNDO — eso lo deciden
/// quienes lo llaman o los eventos a los que se suscribe. Persiste entre
/// escenas (DontDestroyOnLoad) porque la música necesita sobrevivir el
/// cambio Menú → Partida sin cortarse ni duplicarse.
/// </summary>
public class AudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "MusicVolume";
    private const string SfxVolumeKey = "SfxVolume";

    public static AudioManager Instance { get; private set; }

    [Header("Fuentes de audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Biblioteca de efectos")]
    [SerializeField] private SoundLibrary soundLibrary;

    [Header("Volumen (editor por ahora — una futura pantalla de configuración toma prioridad apenas exista)")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private void Awake()
    {
        // Guardia clásica anti-duplicado.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Si ya se guardó un volumen antes (por ejemplo, desde una futura
        // pantalla de configuración que llame a SetMusicVolume/
        // SetSfxVolume), ese valor persistido tiene prioridad sobre el
        // del Inspector. Hasta que eso exista, el Inspector es la única
        // forma de ajustarlo.
        if (PlayerPrefs.HasKey(MusicVolumeKey))
        {
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey);
        }

        if (PlayerPrefs.HasKey(SfxVolumeKey))
        {
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey);
        }

        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;

        sfxSource.volume = sfxVolume;
        sfxSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        LightCycleController.OnAnyPlayerDied += HandleEntityDied;
    }

    private void OnDisable()
    {
        LightCycleController.OnAnyPlayerDied -= HandleEntityDied;
    }

    private void HandleEntityDied(LightCycleController died)
    {
        PlaySfx(SfxId.Explosion);
    }

    /// <summary>
    /// Cambia la música actual. Si el clip pedido ya es el que está
    /// sonando, no hace nada. "loop" controla si se repite (música de
    /// menú/partida, true por defecto) o se reproduce una sola vez
    /// (música de victoria/derrota, por ejemplo).
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void PlaySfx(SfxId id)
    {
        if (soundLibrary == null) return;

        AudioClip clip = soundLibrary.GetClip(id);
        if (clip == null) return;

        sfxSource.PlayOneShot(clip);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSfxVolume() => sfxVolume;
}