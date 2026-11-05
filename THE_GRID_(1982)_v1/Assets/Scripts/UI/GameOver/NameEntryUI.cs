
using TMPro;
using UnityEngine;

/// <summary>
/// Selector de 3 iniciales estilo arcade. W/S cambian la letra activa,
/// D avanza a la siguiente posición, A retrocede, Space confirma.
/// Al confirmar, llama a WavesGameOverController.SubmitName() con las
/// iniciales elegidas y se desactiva.
/// </summary>
public class NameEntryUI : MonoBehaviour
{
    [SerializeField] private GameObject nameEntryPanel;

    [Tooltip("Los tres TMP_Text que muestran cada letra. Orden: izquierda → derecha.")]
    [SerializeField] private TMP_Text[] letterTexts = new TMP_Text[3];

    [Tooltip("El TMP_Text que actúa como cursor/underline bajo la letra activa.")]
    [SerializeField] private TMP_Text[] cursorTexts = new TMP_Text[3];

    [Header("Colores")]
    [SerializeField] private Color activeLetterColor = Color.cyan;
    [SerializeField] private Color inactiveLetterColor = new Color(0.5f, 0.5f, 0.5f);

    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private int[] letterIndices = new int[3];
    private int activeSlot = 0;
    private bool accepting = false;

    private WavesGameOverController gameOverController;

    private void Awake()
    {
        gameOverController = FindFirstObjectByType<WavesGameOverController>();
    }

    private void Start()
    {
        if (nameEntryPanel != null)
            nameEntryPanel.SetActive(false);
    }

    /// <summary>
    /// WavesGameOverController lo llama cuando es momento de pedir el nombre.
    /// </summary>
    public void Show()
    {
        letterIndices[0] = 0;
        letterIndices[1] = 0;
        letterIndices[2] = 0;
        activeSlot = 0;
        accepting = true;

        RefreshDisplay();

        if (nameEntryPanel != null)
            nameEntryPanel.SetActive(true);
    }

    private void Update()
    {
        if (!accepting) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            // W retrocede: A → Z → Y → ...
            letterIndices[activeSlot] =
                (letterIndices[activeSlot] - 1 + Letters.Length) % Letters.Length;

            RefreshDisplay();
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            // S avanza: A → B → C → ...
            letterIndices[activeSlot] =
                (letterIndices[activeSlot] + 1) % Letters.Length;

            RefreshDisplay();
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (activeSlot < 2)
            {
                activeSlot++;
                RefreshDisplay();
            }
            // En la última posición, D también confirma.
            else
            {
                Confirm();
            }
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (activeSlot > 0)
            {
                activeSlot--;
                RefreshDisplay();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            Confirm();
        }
    }

    private void Confirm()
    {
        accepting = false;

        string initials = ""
            + Letters[letterIndices[0]]
            + Letters[letterIndices[1]]
            + Letters[letterIndices[2]];

        if (nameEntryPanel != null)
            nameEntryPanel.SetActive(false);

        // Entregar las iniciales al controller para que guarde el score.
        gameOverController?.SubmitName(initials);
    }

    private void RefreshDisplay()
    {
        for (int i = 0; i < 3; i++)
        {
            if (letterTexts[i] != null)
            {
                letterTexts[i].text = Letters[letterIndices[i]].ToString();
                letterTexts[i].color =
                    i == activeSlot ? activeLetterColor : inactiveLetterColor;
            }

            if (cursorTexts[i] != null)
            {
                // El cursor/underline sólo se muestra bajo la letra activa.
                cursorTexts[i].gameObject.SetActive(i == activeSlot);
            }
        }
    }
}
