using System.Collections.Generic;
using UnityEngine;

public class MenuNavigator : MonoBehaviour
{
    [Tooltip("Lista fija para menús estáticos (Main Menu). Tipada como NavigableButton -no MonoBehaviour- para que Unity no pueda resolver el componente equivocado al arrastrar. Vacío si las opciones se arman por código (ver SetOptions).")]
    [SerializeField] private List<NavigableButton> initialOptions;

    [SerializeField] private RectTransform cursor;
    [SerializeField] private float cursorOffsetX = -30f;

    private IMenuInputProvider inputProvider;
    private readonly List<INavigableOption> options = new List<INavigableOption>();
    private int selectedIndex;

    private void Awake()
    {
        inputProvider = GetComponent<IMenuInputProvider>();

        if (inputProvider == null)
        {
            Debug.LogError($"{name}: falta IMenuInputProvider (ej. KeyboardMenuInput). Desactivando.");
            enabled = false;
        }
    }

    private void Start()
    {
        if (options.Count == 0 && initialOptions != null && initialOptions.Count > 0)
        {
            List<INavigableOption> converted = new List<INavigableOption>();
            foreach (NavigableButton button in initialOptions)
            {
                if (button == null)
                {
                    Debug.LogWarning($"{name}: hay un elemento vacío en Initial Options.");
                    continue;
                }
                converted.Add(button);
            }
            SetOptions(converted);
        }

        if (options.Count == 0)
        {
            Debug.LogWarning($"{name}: no quedó ninguna opción navegable después de Start().");
        }

        if (cursor == null)
        {
            Debug.LogWarning($"{name}: el campo 'Cursor' está sin asignar.");
        }
    }

    private void OnEnable()
    {
        if (options.Count > 0)
        {
            SelectIndex(selectedIndex, notifyOld: false);
        }
    }

    public void SetOptions(List<INavigableOption> newOptions)
    {
        options.Clear();
        options.AddRange(newOptions);

        Debug.Log($"{name}: lista reconstruida ({options.Count} elementos).");

        if (options.Count == 0) return;

        selectedIndex = Mathf.Clamp(selectedIndex, 0, options.Count - 1);
        SelectIndex(selectedIndex, notifyOld: false);
    }

    private void Update()
    {
        if (options.Count == 0) return;

        switch (inputProvider.GetAction())
        {
            case MenuAction.MoveUp: MoveSelection(-1); break;
            case MenuAction.MoveDown: MoveSelection(1); break;
            case MenuAction.DecreaseValue:
                options[selectedIndex].OnChangeValue(-1);
                AudioManager.Instance?.PlaySfx(SfxId.MenuNavigate);
                break;
            case MenuAction.IncreaseValue:
                options[selectedIndex].OnChangeValue(1);
                AudioManager.Instance?.PlaySfx(SfxId.MenuNavigate);
                break;
            case MenuAction.Confirm:
                options[selectedIndex].OnConfirm();
                AudioManager.Instance?.PlaySfx(SfxId.MenuConfirm);
                break;
        }

        // Se recalcula todos los frames (no sólo al cambiar de selección)
        // para que ajustar Cursor Offset X en el Inspector, incluso en
        // Play, se vea reflejado al instante.
        PositionCursor();
    }

    private void MoveSelection(int delta)
    {
        int newIndex = (selectedIndex + delta + options.Count) % options.Count;
        SelectIndex(newIndex, notifyOld: true);
        AudioManager.Instance?.PlaySfx(SfxId.MenuNavigate);
    }

    private void SelectIndex(int index, bool notifyOld)
    {
        if (notifyOld && selectedIndex >= 0 && selectedIndex < options.Count)
        {
            options[selectedIndex].OnDeselected();
        }

        selectedIndex = index;
        options[selectedIndex].OnSelected();
        // No hace falta llamar a PositionCursor() acá: Update() ya lo
        // recalcula todos los frames, incluido el siguiente frame después
        // de este cambio de selección.

        Debug.Log($"{name}: índice {selectedIndex} seleccionado.");
    }

    private void PositionCursor()
    {
        if (cursor == null) return;

        RectTransform anchor = options[selectedIndex].CursorAnchor;
        if (anchor == null) return;

        // GetWorldCorners da el borde real en espacio de mundo, sin
        // importar el pivote del RectTransform ni depender de píxeles
        // hardcodeados. corners[0]=inferior-izq, corners[1]=superior-izq.
        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        Vector3 leftEdgeMidHeight = (corners[0] + corners[1]) * 0.5f;

        cursor.position = leftEdgeMidHeight + new Vector3(cursorOffsetX, 0f, 0f);
    }
}