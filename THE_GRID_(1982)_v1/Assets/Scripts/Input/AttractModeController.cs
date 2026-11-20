
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Controla el Attract Mode del menú principal.
///
/// Se encarga de:
/// - Detectar inactividad en MainMenu_Panel.
/// - Iniciar el modo de presentación.
/// - Reproducir el video cuando no está en modo de prueba.
/// - Detectar interrupciones por teclado o Arduino.
/// - Administrar la pausa y reanudación de la música.
/// - Evitar acciones accidentales por direcciones mantenidas
///   del joystick.
///
/// Este componente debe existir únicamente en la escena MainMenu.
/// </summary>
public class AttractModeController : MonoBehaviour
{
    [Header("Referencias del menú")]
    [SerializeField] private MainMenuManager mainMenuManager;
    [SerializeField] private GameObject attractModePanel;
    [SerializeField] private CombinedMenuInputProvider menuInputProvider;

    [Header("Configuración del Attract Mode")]
    [Tooltip("Minutos de inactividad antes de iniciar el modo.")]
    [SerializeField] private float inactivityMinutes = 2f;

    [Tooltip("Activa el modo de prueba sin necesitar un video.")]
    [SerializeField] private bool testWithoutVideo = true;

    [Header("Video")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip videoClip;

    [Header("Audio del video")]
    [SerializeField] private AudioSource videoAudioSource;

    private float inactivityTimer;

    private bool isAttractModeActive;
    private bool joystickNeutralArmed;
    private bool missingVideoWarningShown;

    private ArduinoSerialManager serialManager;

    private void Awake()
    {
        serialManager = ArduinoSerialManager.Instance;

        if (mainMenuManager == null)
        {
            Debug.LogError(
                "[AttractModeController] No se asignó MainMenuManager."
            );
        }

        if (attractModePanel == null)
        {
            Debug.LogError(
                "[AttractModeController] No se asignó AttractMode_Panel."
            );
        }

        // En el modo definitivo, estas referencias son obligatorias.
        // En el modo de prueba no necesitamos reproductor ni audio de video.
        if (!testWithoutVideo)
        {
            if (videoPlayer == null)
            {
                Debug.LogError(
                    "[AttractModeController] No se asignó VideoPlayer."
                );
            }

            if (videoAudioSource == null)
            {
                Debug.LogError(
                    "[AttractModeController] No se asignó el AudioSource del video."
                );
            }
        }
    }

    private void OnEnable()
    {
        if (mainMenuManager != null)
        {
            mainMenuManager.OnPanelChanged += HandlePanelChanged;
        }

        inactivityTimer = 0f;
    }

    private void OnDisable()
    {
        if (mainMenuManager != null)
        {
            mainMenuManager.OnPanelChanged -= HandlePanelChanged;
        }

        isAttractModeActive = false;

        StopVideoAndRestoreAudio();
    }

    private void Update()
    {
        // Obtener el administrador si todavía no estaba disponible.
        if (serialManager == null)
        {
            serialManager = ArduinoSerialManager.Instance;
        }

        if (isAttractModeActive)
        {
            CheckAttractModeInput();
            return;
        }

        // El temporizador solamente funciona en el menú principal.
        if (mainMenuManager == null ||
            !mainMenuManager.IsMainMenuVisible)
        {
            inactivityTimer = 0f;
            ClearPendingActivity();
            return;
        }

        CheckMenuActivity();

        inactivityTimer += Time.deltaTime;

        float inactivityLimit = inactivityMinutes * 60f;

        if (inactivityTimer >= inactivityLimit)
        {
            StartAttractMode();
        }
    }

    /// <summary>
    /// Detecta actividad mientras el menú principal está visible.
    /// Cualquier actividad reinicia el temporizador.
    /// </summary>
    private void CheckMenuActivity()
    {
        bool activityDetected = Input.anyKeyDown;

        if (serialManager != null)
        {
            while (serialManager.TryConsumeActivity(out char activity))
            {
                if (activity == 'L' ||
                    activity == 'R' ||
                    activity == 'U' ||
                    activity == 'D' ||
                    activity == 'C')
                {
                    activityDetected = true;
                }
            }
        }

        if (activityDetected)
        {
            inactivityTimer = 0f;
        }
    }

    /// <summary>
    /// Inicia el Attract Mode.
    /// </summary>
    private void StartAttractMode()
    {
        if (isAttractModeActive)
            return;

        if (mainMenuManager == null || attractModePanel == null)
        {
            Debug.LogError(
                "[AttractModeController] Faltan referencias del menú."
            );

            inactivityTimer = 0f;
            return;
        }

        // Comprobar si se puede iniciar el modo definitivo.
        if (!testWithoutVideo)
        {
            if (videoClip == null)
            {
                if (!missingVideoWarningShown)
                {
                    Debug.LogWarning(
                        "[AttractModeController] No hay ningún VideoClip " +
                        "asignado. Asigná un video o activá Test Without Video."
                    );

                    missingVideoWarningShown = true;
                }

                inactivityTimer = 0f;
                return;
            }

            if (videoPlayer == null || videoAudioSource == null)
            {
                Debug.LogError(
                    "[AttractModeController] Faltan referencias del video o audio."
                );

                inactivityTimer = 0f;
                return;
            }
        }

        // Descartar interacciones anteriores al inicio.
        ClearPendingActivity();

        // Si el joystick ya está inclinado, no debe interrumpir
        // el modo hasta que primero vuelva a neutral.
        joystickNeutralArmed =
            serialManager == null ||
            serialManager.GetHeldDirection() == 'N';

        // Si no estamos en modo de prueba, preparar el reproductor.
        if (!testWithoutVideo)
        {
            videoPlayer.Stop();
            videoPlayer.clip = videoClip;
            videoPlayer.isLooping = true;

            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, videoAudioSource);

            videoAudioSource.Stop();
        }

        // Marcar el estado antes de cambiar de panel.
        isAttractModeActive = true;
        inactivityTimer = 0f;

        // Pausar la música si estaba reproduciéndose.
        AudioManager.Instance?.PauseMusicForAttractMode();

        // Mostrar el panel de presentación.
        mainMenuManager.ShowAttractMode();

        // Reproducir el video solamente en el modo definitivo.
        if (!testWithoutVideo)
        {
            videoPlayer.Play();
        }

        if (testWithoutVideo)
        {
            Debug.Log(
                "[AttractModeController] Attract Mode iniciado en MODO DE PRUEBA."
            );
        }
        else
        {
            Debug.Log(
                "[AttractModeController] Attract Mode iniciado con video."
            );
        }
    }

    /// <summary>
    /// Comprueba si el usuario realizó alguna interacción
    /// mientras se reproduce el video o está activo el modo de prueba.
    /// </summary>
    private void CheckAttractModeInput()
    {
        // Cualquier tecla interrumpe el modo.
        if (Input.anyKeyDown)
        {
            StopAttractMode();
            return;
        }

        if (serialManager == null)
            return;

        while (serialManager.TryConsumeActivity(out char activity))
        {
            // El pulsador siempre interrumpe.
            if (activity == 'C')
            {
                StopAttractMode();
                return;
            }

            // El joystick volvió a neutral.
            if (activity == 'N')
            {
                joystickNeutralArmed = true;
                continue;
            }

            // Una dirección solamente cuenta si ya se alcanzó
            // neutralidad desde el inicio del modo.
            if ((activity == 'L' ||
                 activity == 'R' ||
                 activity == 'U' ||
                 activity == 'D') &&
                joystickNeutralArmed)
            {
                StopAttractMode();
                return;
            }
        }
    }

    /// <summary>
    /// Método público asignado al botón Back del panel.
    /// </summary>
    public void OnBackButtonClicked()
    {
        StopAttractMode();
    }

    /// <summary>
    /// Detiene el Attract Mode y vuelve al menú principal.
    /// </summary>
    private void StopAttractMode()
    {
        if (!isAttractModeActive)
            return;

        isAttractModeActive = false;

        StopVideoAndRestoreAudio();

        // Evitar que el joystick mantenido navegue inmediatamente.
        if (menuInputProvider != null)
        {
            menuInputProvider.RequireJoystickNeutral();
        }

        ClearPendingActivity();

        inactivityTimer = 0f;

        if (mainMenuManager != null)
        {
            mainMenuManager.ShowMainMenu();
        }

        Debug.Log("[AttractModeController] Attract Mode interrumpido.");
    }

    /// <summary>
    /// Detiene el reproductor y restaura el audio.
    /// </summary>
    private void StopVideoAndRestoreAudio()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (videoAudioSource != null)
        {
            videoAudioSource.Stop();
        }

        AudioManager.Instance?.ResumeMusicAfterAttractMode();
    }

    /// <summary>
    /// Reacciona a los cambios de panel del menú.
    /// </summary>
    private void HandlePanelChanged(GameObject panel)
    {
        if (panel == attractModePanel)
            return;

        // Si otro sistema cambia de panel durante el Attract Mode,
        // detenerlo sin forzar otro cambio de panel.
        if (isAttractModeActive)
        {
            isAttractModeActive = false;

            StopVideoAndRestoreAudio();

            if (menuInputProvider != null)
            {
                menuInputProvider.RequireJoystickNeutral();
            }

            ClearPendingActivity();
        }

        inactivityTimer = 0f;
    }

    /// <summary>
    /// Descarta actividades pendientes de Arduino.
    /// No modifica la dirección física del joystick.
    /// </summary>
    private void ClearPendingActivity()
    {
        if (serialManager != null)
        {
            serialManager.ClearPendingActivity();
        }
    }
}
