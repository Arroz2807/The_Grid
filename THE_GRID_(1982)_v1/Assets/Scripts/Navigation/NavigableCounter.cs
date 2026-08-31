using System;
using TMPro;
using UnityEngine;

public class NavigableCounter : MonoBehaviour, INavigableOption
{
    [Tooltip("El texto del TÍTULO (ej. 'Amount of Enemies:'), no el del valor. El cursor se alinea con esto.")]
    [SerializeField] private RectTransform titleAnchor;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private int minValue = 1;
    [SerializeField] private int maxValue = 4;
    [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color selectedColor = Color.cyan;

    private int currentValue;

    public event Action<int> OnValueChanged;

    public int CurrentValue => currentValue;
    public RectTransform CursorAnchor => titleAnchor != null ? titleAnchor : (RectTransform)transform;

    private void Awake()
    {
        currentValue = minValue;
        UpdateText();
        ApplyVisual(false);

        if (titleAnchor == null)
        {
            Debug.LogWarning($"{name}: 'Title Anchor' no asignado — el cursor se va a alinear con este objeto en vez del título.");
        }
    }

    public void OnSelected() => ApplyVisual(true);
    public void OnDeselected() => ApplyVisual(false);
    public void OnConfirm() { }

    public void OnChangeValue(int direction)
    {
        int previous = currentValue;
        currentValue = Mathf.Clamp(currentValue + direction, minValue, maxValue);

        if (currentValue == previous) return;

        UpdateText();
        Debug.Log($"{name}: cantidad cambiada a {currentValue}.");
        OnValueChanged?.Invoke(currentValue);
    }

    private void UpdateText()
    {
        if (valueText != null) valueText.text = $"<  {currentValue}  >";
    }

    private void ApplyVisual(bool selected)
    {
        Color color = selected ? selectedColor : normalColor;

        if (valueText != null) valueText.color = color;

        if (titleAnchor != null && titleAnchor.TryGetComponent(out TMP_Text titleText))
        {
            titleText.color = color;
        }
    }
}