using UnityEngine;

/// <summary>
/// Raíz del prefab del segundo monitor. Se encarga de tres cosas:
/// 1) garantizar que exista UNA sola instancia (guarda anti-duplicado),
/// 2) mantenerla viva entre escenas (DontDestroyOnLoad),
/// 3) activar el display y dirigir a él la cámara y el Canvas del prefab.
///
/// No tiene lógica de ranking: el contenido lo dibuja RankingBoardView.
/// El prefab NO lleva EventSystem, GraphicRaycaster ni AudioListener, para
/// no interferir con los inputs, los botones ni el audio del monitor
/// principal.
/// </summary>
public class SecondDisplayController : MonoBehaviour
{
    public static SecondDisplayController Instance { get; private set; }

    [Tooltip("0 = monitor principal, 1 = segundo monitor.")]
    [SerializeField] private int displayIndex = 1;

    [SerializeField] private Camera displayCamera;
    [SerializeField] private Canvas displayCanvas;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Display.displays solo refleja los monitores reales en un ejecutable.
        // En el Editor tiene un único elemento: ahí el segundo display se
        // previsualiza con el desplegable "Display 2" de la ventana Game.
        if (displayIndex > 0 && displayIndex < Display.displays.Length)
        {
            Display.displays[displayIndex].Activate();
            Debug.Log("[SecondDisplay] Display " + (displayIndex + 1) + " activado.");
        }

        if (displayCamera != null) displayCamera.targetDisplay = displayIndex;
        if (displayCanvas != null) displayCanvas.targetDisplay = displayIndex;
    }
}