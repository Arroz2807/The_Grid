
using System.Collections.Generic;
using UnityEngine;

public class MenuNavigator : MonoBehaviour
{
    [Tooltip("Lista fija para menús estáticos (Main Menu). Tipada como NavigableButton -no MonoBehaviour- para que Unity no pueda resolver el componente equivocado al arrastrar. Vacío si las opciones se arman por código (ver SetOptions).")]
    [SerializeField] private List<NavigableButton> initialOptions;

    [Tooltip("Índice (0-based) de la opción donde arranca el cursor cuando la lista viene de Initial Options. Las listas armadas por código usan el initialIndex de SetOptions().")]
    [SerializeField] private int initialSelectedIndex = 0;

    [Tooltip("Si está activo, A/D mueven el cursor entre opciones (menús con botones en horizontal) en vez de cambiar valores. W/S siguen funcionando igual.")]
    [SerializeField] private bool horizontalNavigation = false;

    [SerializeField] private RectTransform cursor;
    [SerializeField] private float cursorOffsetX = -30f;

    private IMenuInputProvider inputProvider;
    private readonly List<INavigableOption> options = new List<INavigableOption>();
    private int selectedIndex;

    private void Awake()
    {
        // Primero buscamos el proveedor combinado.
        // Si existe, administra tanto el joystick como el teclado.
        CombinedMenuInputProvider combinedProvider =
            GetComponent<CombinedMenuInputProvider>();

        if (combinedProvider != null)
        {
            inputProvider = combinedProvider;
        }
        else
        {
            // En los demás paneles mantenemos el proveedor que ya utilizaban.
            inputProvider = GetComponent<IMenuInputProvider>();
        }

        if (inputProvider == null)
        {
            Debug.LogError(
                $"{name}: falta IMenuInputProvider (ej. KeyboardMenuInput). Desactivando."
            );

            enabled = false;
        }
        else
        {
            Debug.Log(
                $"{name}: proveedor de entrada seleccionado: " +
                $"{inputProvider.GetType().Name}"
            );
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

            SetOptions(converted, initialSelectedIndex);
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

    public void SetOptions(List<INavigableOption> newOptions, int initialIndex = 0)
    {
        options.Clear();
        options.AddRange(newOptions);

        Debug.Log($"{name}: lista reconstruida ({options.Count} elementos).");

        if (options.Count == 0) return;

        selectedIndex = Mathf.Clamp(initialIndex, 0, options.Count - 1);
        SelectIndex(selectedIndex, notifyOld: false);
    }

    private void Update()
    {
        if (options.Count == 0) return;

        switch (inputProvider.GetAction())
        {
            case MenuAction.MoveUp:
                MoveSelection(-1);
                break;

            case MenuAction.MoveDown:
                MoveSelection(1);
                break;

            case MenuAction.DecreaseValue:
                if (horizontalNavigation)
                {
                    // Menú horizontal: A mueve el cursor a la izquierda.
                    MoveSelection(-1);
                }
                else
                {
                    options[selectedIndex].OnChangeValue(-1);
                    AudioManager.Instance?.PlaySfx(SfxId.MenuNavigate);
                }
                break;

            case MenuAction.IncreaseValue:
                if (horizontalNavigation)
                {
                    MoveSelection(1);
                }
                else
                {
                    options[selectedIndex].OnChangeValue(1);
                    AudioManager.Instance?.PlaySfx(SfxId.MenuNavigate);
                }
                break;

            case MenuAction.Confirm:
                options[selectedIndex].OnConfirm();
                AudioManager.Instance?.PlaySfx(SfxId.MenuConfirm);
                break;
        }

        // Se recalcula todos los frames para que los cambios
        // de Cursor Offset X se reflejen incluso durante Play.
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

        Debug.Log($"{name}: índice {selectedIndex} seleccionado.");
    }

    private void PositionCursor()
    {
        if (cursor == null) return;

        RectTransform anchor = options[selectedIndex].CursorAnchor;

        if (anchor == null) return;

        // GetWorldCorners obtiene los límites reales del RectTransform.
        // corners[0] = inferior izquierda.
        // corners[1] = superior izquierda.
        Vector3[] corners = new Vector3[4];

        anchor.GetWorldCorners(corners);

        Vector3 leftEdgeMidHeight = (corners[0] + corners[1]) * 0.5f;

        cursor.position = leftEdgeMidHeight +
                          new Vector3(cursorOffsetX, 0f, 0f);
    }
}
