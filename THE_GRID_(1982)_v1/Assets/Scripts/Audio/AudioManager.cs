
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Único punto de reproducción de audio del juego.
///
/// Administra música individual, playlists de menú y partida,
/// efectos de sonido, Attract Mode, transiciones de resultado
/// y transiciones graduales de volumen.
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

    [Header("Volumen general")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("Multiplicadores por playlist")]
    [Range(0f, 1f)]
    [SerializeField] private float menuMusicMultiplier = 0.7f;

    [Range(0f, 1f)]
    [SerializeField] private float gameplayMusicMultiplier = 0.4f;

    [Header("Transiciones de música")]
    [Tooltip("Duración del aumento gradual de volumen.")]
    [Min(0f)]
    [SerializeField] private float fadeInDuration = 1.5f;

    [Tooltip("Duración de la disminución gradual de volumen.")]
    [Min(0f)]
    [SerializeField] private float fadeOutDuration = 1.5f;

    [Header("Transición de resultado")]
    [Tooltip("Tiempo que tarda la música de partida en bajar hasta el silencio antes de reproducir el efecto de victoria o derrota.")]
    [Min(0f)]
    [SerializeField] private float resultMusicFadeOutDuration = 1f;

    // --------------------------------------------------
    // FUENTE DE AUDIO DE RESULTADO
    // --------------------------------------------------

    // Se mantiene separada de sfxSource para poder detener
    // victoria/derrota sin cortar explosiones u otros SFX.
    private AudioSource resultAudioSource;

    // --------------------------------------------------
    // PLAYLISTS
    // --------------------------------------------------

    private AudioClip[] menuPlaylist;
    private AudioClip[] gameplayPlaylist;

    private AudioClip[] activePlaylist;
    private SceneMusicType? activePlaylistType;

    private int currentPlaylistIndex = -1;

    private Coroutine playlistRoutine;
    private Coroutine musicTransitionRoutine;
    private Coroutine musicFadeRoutine;

    // Multiplicador temporal del fade.
    // 0 = silencio, 1 = volumen configurado.
    private float currentFadeMultiplier = 1f;

    // --------------------------------------------------
    // ATTRACT MODE
    // --------------------------------------------------

    private bool musicPausedForAttractMode;
    private bool musicWasPlayingBeforeAttractMode;

    // --------------------------------------------------
    // AUDIO DE RESULTADO
    // --------------------------------------------------

    private Coroutine resultAudioRoutine;
    private bool isResultAudioPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (PlayerPrefs.HasKey(MusicVolumeKey))
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey);

        if (PlayerPrefs.HasKey(SfxVolumeKey))
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey);

        if (musicSource != null)
            musicSource.playOnAwake = false;

        if (sfxSource != null)
            sfxSource.playOnAwake = false;

        CreateResultAudioSource();

        ApplyMusicVolume();
        ApplySfxVolume();
    }

    /// <summary>
    /// Crea una fuente independiente para los efectos
    /// de victoria y derrota.
    /// </summary>
    private void CreateResultAudioSource()
    {
        resultAudioSource = gameObject.AddComponent<AudioSource>();

        resultAudioSource.playOnAwake = false;
        resultAudioSource.loop = false;
        resultAudioSource.spatialBlend = 0f;

        // Mantener la misma salida de audio que los SFX generales.
        if (sfxSource != null)
        {
            resultAudioSource.outputAudioMixerGroup =
                sfxSource.outputAudioMixerGroup;

            resultAudioSource.priority = sfxSource.priority;
        }

        resultAudioSource.volume = sfxVolume;
    }

    private void OnEnable()
    {
        LightCycleController.OnAnyPlayerDied += HandleEntityDied;
    }

    private void OnDisable()
    {
        LightCycleController.OnAnyPlayerDied -= HandleEntityDied;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // --------------------------------------------------
    // VOLUMEN
    // --------------------------------------------------

    /// <summary>
    /// Calcula el volumen efectivo:
    /// volumen general × multiplicador de playlist × fade.
    /// </summary>
    private void ApplyMusicVolume()
    {
        if (musicSource == null)
            return;

        float playlistMultiplier = 1f;

        if (activePlaylistType.HasValue)
        {
            switch (activePlaylistType.Value)
            {
                case SceneMusicType.Menu:
                    playlistMultiplier = menuMusicMultiplier;
                    break;

                case SceneMusicType.Gameplay:
                    playlistMultiplier = gameplayMusicMultiplier;
                    break;
            }
        }

        musicSource.volume =
            musicVolume *
            playlistMultiplier *
            currentFadeMultiplier;
    }

    private void ApplySfxVolume()
    {
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;

        if (resultAudioSource != null)
            resultAudioSource.volume = sfxVolume;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ApplyMusicVolume();

        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplySfxVolume();

        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
    }

    public float GetMusicVolume() => musicVolume;
    public float GetSfxVolume() => sfxVolume;

    // --------------------------------------------------
    // FADE DE MÚSICA
    // --------------------------------------------------

    /// <summary>
    /// Cambia gradualmente el multiplicador temporal
    /// del volumen de la música.
    /// </summary>
    private IEnumerator FadeMusic(
        float targetMultiplier,
        float duration)
    {
        float startMultiplier = currentFadeMultiplier;

        if (duration <= 0f)
        {
            currentFadeMultiplier = targetMultiplier;
            ApplyMusicVolume();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            currentFadeMultiplier = Mathf.Lerp(
                startMultiplier,
                targetMultiplier,
                progress
            );

            ApplyMusicVolume();

            yield return null;
        }

        currentFadeMultiplier = targetMultiplier;
        ApplyMusicVolume();
    }

    private void StopMusicFade()
    {
        if (musicFadeRoutine != null)
        {
            StopCoroutine(musicFadeRoutine);
            musicFadeRoutine = null;
        }
    }

    private void StopMusicTransition()
    {
        if (musicTransitionRoutine != null)
        {
            StopCoroutine(musicTransitionRoutine);
            musicTransitionRoutine = null;
        }

        StopMusicFade();
    }

    // --------------------------------------------------
    // EFECTOS AUTOMÁTICOS
    // --------------------------------------------------

    private void HandleEntityDied(LightCycleController died)
    {
        PlaySfx(SfxId.Explosion);
    }

    // --------------------------------------------------
    // REGISTRO DE PLAYLISTS
    // --------------------------------------------------

    public void RegisterScenePlaylist(
        SceneMusicType type,
        AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning(
                $"AudioManager: la playlist {type} está vacía."
            );
            return;
        }

        List<AudioClip> validClips = new List<AudioClip>();

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
                validClips.Add(clip);
        }

        if (validClips.Count == 0)
        {
            Debug.LogWarning(
                $"AudioManager: la playlist {type} no contiene canciones válidas."
            );
            return;
        }

        AudioClip[] registeredPlaylist = validClips.ToArray();

        switch (type)
        {
            case SceneMusicType.Menu:
                menuPlaylist = registeredPlaylist;
                break;

            case SceneMusicType.Gameplay:
                gameplayPlaylist = registeredPlaylist;
                break;
        }
    }

    public void PlayScenePlaylist(SceneMusicType type)
    {
        AudioClip[] requestedPlaylist = GetPlaylist(type);

        if (requestedPlaylist == null || requestedPlaylist.Length == 0)
        {
            Debug.LogWarning(
                $"AudioManager: no hay una playlist registrada para {type}."
            );
            return;
        }

        // Una nueva solicitud de música reemplaza cualquier
        // secuencia de resultado que todavía esté activa.
        if (isResultAudioPlaying)
            CancelResultAudio();

        // No reiniciar una playlist que ya está sonando.
        if (
            activePlaylistType == type &&
            musicSource != null &&
            musicSource.isPlaying
        )
        {
            return;
        }

        StartPlaylist(requestedPlaylist, type);
    }

    private AudioClip[] GetPlaylist(SceneMusicType type)
    {
        switch (type)
        {
            case SceneMusicType.Menu:
                return menuPlaylist;

            case SceneMusicType.Gameplay:
                return gameplayPlaylist;

            default:
                return null;
        }
    }

    // --------------------------------------------------
    // CAMBIO ENTRE CONTEXTOS MUSICALES
    // --------------------------------------------------

    public void PlayMenuPlaylist()
    {
        PlayScenePlaylist(SceneMusicType.Menu);
    }

    public void PlayGameplayPlaylist()
    {
        PlayScenePlaylist(SceneMusicType.Gameplay);
    }

    // --------------------------------------------------
    // PLAYLISTS
    // --------------------------------------------------

    public void PlayPlaylist(AudioClip[] clips)
    {
        AudioClip[] validClips = FilterValidClips(clips);

        if (validClips == null || validClips.Length == 0)
        {
            Debug.LogWarning(
                "AudioManager: no se puede iniciar la playlist."
            );
            return;
        }

        StartPlaylist(validClips, null);
    }

    private AudioClip[] FilterValidClips(AudioClip[] clips)
    {
        if (clips == null)
            return null;

        List<AudioClip> validClips = new List<AudioClip>();

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
                validClips.Add(clip);
        }

        return validClips.ToArray();
    }

    private void StartPlaylist(
        AudioClip[] clips,
        SceneMusicType? type)
    {
        if (musicSource == null)
            return;

        // Cualquier nueva playlist tiene prioridad sobre
        // una secuencia de resultado que siga reproduciéndose.
        if (isResultAudioPlaying)
            CancelResultAudio();

        StopMusicTransition();
        StopPlaylistMonitoring();

        musicTransitionRoutine = StartCoroutine(
            TransitionToPlaylist(clips, type)
        );
    }

    /// <summary>
    /// Baja el volumen de la música anterior,
    /// cambia de playlist e inicia la siguiente canción.
    ///
    /// Si la música anterior está pausada, no la reanuda.
    /// </summary>
    private IEnumerator TransitionToPlaylist(
        AudioClip[] clips,
        SceneMusicType? type)
    {
        if (musicSource.isPlaying)
        {
            yield return FadeMusic(
                0f,
                fadeOutDuration
            );
        }

        musicSource.Stop();

        activePlaylist = clips;
        activePlaylistType = type;
        currentPlaylistIndex = -1;

        currentFadeMultiplier = 0f;
        ApplyMusicVolume();

        musicTransitionRoutine = null;

        if (musicPausedForAttractMode)
            yield break;

        PlayNextPlaylistTrack();
    }

    /// <summary>
    /// Selecciona otra canción sin repetir consecutivamente.
    /// </summary>
    private void PlayNextPlaylistTrack()
    {
        if (
            activePlaylist == null ||
            activePlaylist.Length == 0 ||
            musicSource == null ||
            musicPausedForAttractMode ||
            isResultAudioPlaying
        )
        {
            return;
        }

        int nextIndex;

        if (activePlaylist.Length == 1)
        {
            nextIndex = 0;
        }
        else
        {
            do
            {
                nextIndex = Random.Range(0, activePlaylist.Length);
            }
            while (nextIndex == currentPlaylistIndex);
        }

        currentPlaylistIndex = nextIndex;

        AudioClip nextClip = activePlaylist[currentPlaylistIndex];

        StopMusicFade();

        currentFadeMultiplier = 0f;
        ApplyMusicVolume();

        musicSource.Stop();
        musicSource.clip = nextClip;
        musicSource.loop = false;
        musicSource.Play();

        musicFadeRoutine = StartCoroutine(
            FadeMusic(1f, fadeInDuration)
        );

        StopPlaylistMonitoring();

        playlistRoutine = StartCoroutine(
            MonitorPlaylistTrack(nextClip)
        );
    }

    /// <summary>
    /// Detecta cuándo se acerca el final de una canción
    /// para iniciar el fade-out antes de que termine.
    /// </summary>
    private IEnumerator MonitorPlaylistTrack(AudioClip trackedClip)
    {
        while (
            activePlaylist != null &&
            musicSource != null &&
            musicSource.clip == trackedClip
        )
        {
            if (musicPausedForAttractMode || isResultAudioPlaying)
            {
                yield return null;
                continue;
            }

            if (!musicSource.isPlaying)
                break;

            float remainingTime =
                trackedClip.length - musicSource.time;

            if (remainingTime <= fadeOutDuration)
                break;

            yield return null;
        }

        playlistRoutine = null;

        if (
            activePlaylist == null ||
            musicSource == null ||
            musicSource.clip != trackedClip ||
            musicPausedForAttractMode ||
            isResultAudioPlaying
        )
        {
            yield break;
        }

        if (musicSource.isPlaying)
        {
            float remainingTime = Mathf.Max(
                0f,
                trackedClip.length - musicSource.time
            );

            float duration = Mathf.Min(
                fadeOutDuration,
                remainingTime
            );

            yield return FadeMusic(0f, duration);
        }

        if (
            activePlaylist != null &&
            musicSource.clip == trackedClip &&
            !musicPausedForAttractMode &&
            !isResultAudioPlaying
        )
        {
            PlayNextPlaylistTrack();
        }
    }

    private void StopPlaylistMonitoring()
    {
        if (playlistRoutine != null)
        {
            StopCoroutine(playlistRoutine);
            playlistRoutine = null;
        }
    }

    // --------------------------------------------------
    // MÚSICA INDIVIDUAL
    // --------------------------------------------------

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null)
            return;

        if (isResultAudioPlaying)
            CancelResultAudio();

        if (musicPausedForAttractMode)
        {
            musicPausedForAttractMode = false;
            musicWasPlayingBeforeAttractMode = false;
        }

        StopMusicTransition();
        StopPlaylistMonitoring();

        activePlaylist = null;
        activePlaylistType = null;
        currentPlaylistIndex = -1;

        musicTransitionRoutine = StartCoroutine(
            TransitionToSingleMusic(clip, loop)
        );
    }

    private IEnumerator TransitionToSingleMusic(
        AudioClip clip,
        bool loop)
    {
        if (musicSource.isPlaying)
        {
            yield return FadeMusic(0f, fadeOutDuration);
        }

        musicSource.Stop();

        activePlaylist = null;
        activePlaylistType = null;

        currentFadeMultiplier = 0f;
        ApplyMusicVolume();

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();

        musicTransitionRoutine = null;

        if (!musicPausedForAttractMode)
        {
            musicFadeRoutine = StartCoroutine(
                FadeMusic(1f, fadeInDuration)
            );
        }
    }

    // --------------------------------------------------
    // AUDIO DE RESULTADO
    // --------------------------------------------------

    public void PlaySfxThenPlayMenuPlaylist(SfxId id)
    {
        if (soundLibrary == null || resultAudioSource == null)
        {
            PlayMenuPlaylist();
            return;
        }

        AudioClip clip = soundLibrary.GetClip(id);

        if (clip == null)
        {
            PlayMenuPlaylist();
            return;
        }

        // Evitar que dos secuencias de resultado se superpongan.
        CancelResultAudio();

        isResultAudioPlaying = true;

        StopMusicTransition();
        StopPlaylistMonitoring();

        resultAudioRoutine = StartCoroutine(
            PlayResultAudioSequence(clip, id)
        );
    }

    /// <summary>
    /// Controla toda la secuencia de audio de resultado:
    /// fade-out de gameplay, SFX y playlist de menú.
    /// </summary>
    private IEnumerator PlayResultAudioSequence(
        AudioClip clip,
        SfxId id)
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            yield return FadeMusic(
                0f,
                resultMusicFadeOutDuration
            );

            musicSource.Stop();
        }

        // Si Attract Mode está activo, no interrumpimos su pausa.
        if (musicPausedForAttractMode)
        {
            isResultAudioPlaying = false;
            resultAudioRoutine = null;
            yield break;
        }

        float volumeScale = soundLibrary.GetVolumeScale(id);

        resultAudioSource.Stop();
        resultAudioSource.PlayOneShot(clip, volumeScale);

        while (
            resultAudioSource != null &&
            resultAudioSource.isPlaying
        )
        {
            yield return null;
        }

        isResultAudioPlaying = false;
        resultAudioRoutine = null;

        if (musicPausedForAttractMode)
            yield break;

        PlayMenuPlaylist();
    }

    /// <summary>
    /// Cancela la secuencia de resultado y detiene
    /// exclusivamente su efecto de sonido.
    /// </summary>
    private void CancelResultAudio()
    {
        if (resultAudioRoutine != null)
        {
            StopCoroutine(resultAudioRoutine);
            resultAudioRoutine = null;
        }

        if (resultAudioSource != null)
            resultAudioSource.Stop();

        isResultAudioPlaying = false;
    }

    /// <summary>
    /// Cancela el audio de resultado cuando se reinicia
    /// una partida. La nueva escena se encargará de iniciar
    /// la playlist de gameplay con su fade-in.
    /// </summary>
    public void CancelResultAudioForGameplayRestart()
    {
        // Cancelar la secuencia de resultado y detener
        // exclusivamente su efecto de sonido.
        CancelResultAudio();

        // Detener cualquier transición o monitoreo anterior.
        StopMusicTransition();
        StopMusicFade();
        StopPlaylistMonitoring();

        // Detener la música anterior.
        if (musicSource != null)
            musicSource.Stop();

        // Preparar el volumen para que la nueva canción
        // comience desde silencio y realice su fade-in.
        currentFadeMultiplier = 0f;
        ApplyMusicVolume();
    }

    // --------------------------------------------------
    // ATTRACT MODE
    // --------------------------------------------------

    public void PauseMusicForAttractMode()
    {
        if (musicPausedForAttractMode)
            return;

        musicPausedForAttractMode = true;

        musicWasPlayingBeforeAttractMode =
            musicSource != null && musicSource.isPlaying;

        if (musicSource != null && musicSource.isPlaying)
            musicSource.Pause();
    }

    public void ResumeMusicAfterAttractMode()
    {
        if (!musicPausedForAttractMode)
            return;

        bool shouldContinueMusic = musicWasPlayingBeforeAttractMode;

        musicPausedForAttractMode = false;
        musicWasPlayingBeforeAttractMode = false;

        if (!shouldContinueMusic || isResultAudioPlaying)
            return;

        if (activePlaylist != null)
        {
            PlayNextPlaylistTrack();
        }
        else if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    // --------------------------------------------------
    // SFX GENERALES
    // --------------------------------------------------

    public void PlaySfx(SfxId id)
    {
        if (soundLibrary == null || sfxSource == null)
            return;

        AudioClip clip = soundLibrary.GetClip(id);

        if (clip == null)
            return;

        float volumeScale = soundLibrary.GetVolumeScale(id);

        sfxSource.PlayOneShot(clip, volumeScale);
    }
}
