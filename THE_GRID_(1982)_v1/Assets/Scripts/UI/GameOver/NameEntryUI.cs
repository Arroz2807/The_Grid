
using TMPro;
using UnityEngine;

/// <summary>
/// Selector de 3 iniciales estilo arcade.
///
/// Recibe las acciones mediante IMenuInputProvider,
/// permitiendo utilizar teclado, joystick Arduino
/// y el pulsador físico de confirmación.
///
/// Arriba/Abajo: cambiar letra.
/// Izquierda/Derecha: cambiar posición.
/// Derecha en la última posición: confirmar.
/// Confirm: confirmar el nombre.
/// </summary>
public class NameEntryUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject nameEntryPanel;

    [Header("Iniciales")]
    [Tooltip("Los tres TMP_Text que muestran cada letra. Orden: izquierda a derecha.")]
    [SerializeField] private TMP_Text[] letterTexts = new TMP_Text[3];

    [Tooltip("Los TMP_Text que actúan como cursor debajo de cada letra.")]
    [SerializeField] private TMP_Text[] cursorTexts = new TMP_Text[3];

    [Header("Colores")]
    [SerializeField] private Color activeLetterColor = Color.cyan;
    [SerializeField] private Color inactiveLetterColor = new Color(0.5f, 0.5f, 0.5f);

    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private readonly int[] letterIndices = new int[3];

    private int activeSlot = 0;
    private bool accepting = false;

    private IMenuInputProvider inputProvider;
    private WavesGameOverController gameOverController;

    private void Awake()
    {
        // Buscamos primero el proveedor combinado.
        // Así garantizamos que se utilicen Arduino y teclado.
        CombinedMenuInputProvider combinedProvider =
            GetComponent<CombinedMenuInputProvider>();

        if (combinedProvider != null)
        {
            inputProvider = combinedProvider;
        }
        else
        {
            // Alternativa por si en el futuro se utiliza
            // otro proveedor que implemente la interfaz.
            inputProvider = GetComponent<IMenuInputProvider>();
        }

        if (inputProvider == null)
        {
            Debug.LogError(
                $"{name}: no se encontró ningún IMenuInputProvider."
            );

            enabled = false;
            return;
        }

        gameOverController =
            FindFirstObjectByType<WavesGameOverController>();
    }

    private void Start()
    {
        if (nameEntryPanel != null)
        {
            nameEntryPanel.SetActive(false);
        }
    }

    /// <summary>
    /// WavesGameOverController lo llama cuando es momento
    /// de solicitar las iniciales del jugador.
    /// </summary>
    public void Show()
    {
        // Reiniciar las tres iniciales en A.
        letterIndices[0] = 0;
        letterIndices[1] = 0;
        letterIndices[2] = 0;

        // Comenzar seleccionando la primera posición.
        activeSlot = 0;

        accepting = true;

        RefreshDisplay();

        if (nameEntryPanel != null)
        {
            nameEntryPanel.SetActive(true);
        }

        Debug.Log("[NameEntryUI] Ingreso de iniciales iniciado.");
    }

    private void Update()
    {
        if (!accepting || inputProvider == null)
            return;

        MenuAction action = inputProvider.GetAction();

        switch (action)
        {
            case MenuAction.MoveUp:
                // Retroceder: A -> Z -> Y -> ...
                ChangeLetter(-1);
                break;

            case MenuAction.MoveDown:
                // Avanzar: A -> B -> C -> ...
                ChangeLetter(1);
                break;

            case MenuAction.DecreaseValue:
                // Seleccionar la posición anterior.
                MoveSlot(-1);
                break;

            case MenuAction.IncreaseValue:
                // Seleccionar la siguiente posición.
                // En la última, confirmar el nombre.
                if (activeSlot < 2)
                {
                    MoveSlot(1);
                }
                else
                {
                    Confirm();
                }
                break;

            case MenuAction.Confirm:
                Confirm();
                break;
        }
    }

    /// <summary>
    /// Cambia la letra de la posición actualmente seleccionada.
    /// </summary>
    private void ChangeLetter(int direction)
    {
        letterIndices[activeSlot] =
            (letterIndices[activeSlot] + direction + Letters.Length)
            % Letters.Length;

        RefreshDisplay();
    }

    /// <summary>
    /// Cambia la posición seleccionada.
    /// No permite salir del rango de las tres iniciales.
    /// </summary>
    private void MoveSlot(int direction)
    {
        int newSlot = activeSlot + direction;

        if (newSlot < 0 || newSlot > 2)
            return;

        activeSlot = newSlot;

        RefreshDisplay();
    }

    /// <summary>
    /// Confirma las tres iniciales y las entrega
    /// a WavesGameOverController para guardar el score.
    /// </summary>
    private void Confirm()
    {
        if (!accepting)
            return;

        accepting = false;

        string initials =
            Letters[letterIndices[0]].ToString() +
            Letters[letterIndices[1]].ToString() +
            Letters[letterIndices[2]].ToString();

        Debug.Log($"[NameEntryUI] Iniciales confirmadas: {initials}");

        if (nameEntryPanel != null)
        {
            nameEntryPanel.SetActive(false);
        }

        gameOverController?.SubmitName(initials);
    }

    /// <summary>
    /// Actualiza las letras, los colores y los cursores.
    /// </summary>
    private void RefreshDisplay()
    {
        for (int i = 0; i < 3; i++)
        {
            if (letterTexts != null &&
                i < letterTexts.Length &&
                letterTexts[i] != null)
            {
                letterTexts[i].text =
                    Letters[letterIndices[i]].ToString();

                letterTexts[i].color =
                    i == activeSlot
                        ? activeLetterColor
                        : inactiveLetterColor;
            }

            if (cursorTexts != null &&
                i < cursorTexts.Length &&
                cursorTexts[i] != null)
            {
                // Mostrar únicamente el cursor de la letra activa.
                cursorTexts[i].gameObject.SetActive(i == activeSlot);
            }
        }
    }
}
