using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NavigableButton : MonoBehaviour, INavigableOption
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color selectedColor = Color.cyan;
    [SerializeField] private float selectedScale = 1.08f;

    private Button button;

    public RectTransform CursorAnchor => label != null ? label.rectTransform : (RectTransform)transform;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (label == null)
        {
            Debug.LogWarning($"{name}: 'Label' no asignado en NavigableButton.");
        }

        ApplyVisual(false);
    }

    public void OnSelected() => ApplyVisual(true);
    public void OnDeselected() => ApplyVisual(false);
    public void OnConfirm() => button.onClick.Invoke();
    public void OnChangeValue(int direction) { }

    private void ApplyVisual(bool selected)
    {
        if (label != null) label.color = selected ? selectedColor : normalColor;
        transform.localScale = Vector3.one * (selected ? selectedScale : 1f);
    }
}