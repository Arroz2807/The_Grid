using TMPro;
using UnityEngine;

/// <summary>
/// Opción navegable que representa un valor booleano ON/OFF. Space o D/A
/// lo alternan. Sigue el mismo patrón que NavigableCounter y NavigableSlider.
/// </summary>
public class NavigableToggle : MonoBehaviour, INavigableOption
{
    [SerializeField] private RectTransform titleAnchor;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private string onLabel = "ON";
    [SerializeField] private string offLabel = "OFF";
    [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color selectedColor = Color.cyan;
    [SerializeField] private Color onColor = Color.cyan;
    [SerializeField] private Color offColor = new Color(0.4f, 0.4f, 0.4f);

    private bool currentValue = true;

    public RectTransform CursorAnchor => titleAnchor != null ? titleAnchor : (RectTransform)transform;

    public bool CurrentValue => currentValue;

    public event System.Action<bool> OnValueChanged;

    private void Awake()
    {
        // Leer el valor actual de MatchConfig para que el toggle siempre
        // refleje el estado real al abrir el menú — incluso si el jugador
        // cambió el valor en una partida anterior y volvió al menú sin
        // reiniciar la aplicación.
        currentValue = MatchConfig.PowerUpsEnabled;
        UpdateDisplay();
        ApplyColor(false);
    }

    public void OnSelected() => ApplyColor(true);
    public void OnDeselected() => ApplyColor(false);

    public void OnConfirm() => Toggle();

    public void OnChangeValue(int direction) => Toggle();

    private void Toggle()
    {
        currentValue = !currentValue;
        MatchConfig.SetPowerUpsEnabled(currentValue);
        Debug.Log($"[PowerUpsToggle] Power-ups: {(currentValue ? "ON" : "OFF")}");
        UpdateDisplay();
        OnValueChanged?.Invoke(currentValue);
    }

    private void UpdateDisplay()
    {
        if (valueText == null) return;
        valueText.text = currentValue ? onLabel : offLabel;
        valueText.color = currentValue ? onColor : offColor;
    }

    private void ApplyColor(bool selected)
    {
        Color c = selected ? selectedColor : normalColor;
        if (titleText != null) titleText.color = c;
    }
}