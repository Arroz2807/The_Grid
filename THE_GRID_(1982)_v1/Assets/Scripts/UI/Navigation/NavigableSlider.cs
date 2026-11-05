using TMPro;
using UnityEngine;

/// <summary>
/// Opción navegable que representa un volumen ajustable en pasos de 0.1.
/// A/D incrementan/decrementan el valor. Persiste en PlayerPrefs con la
/// clave indicada y notifica a AudioManager en cada cambio — así la
/// pantalla de configuración y AudioManager siempre están sincronizados.
/// La visualización es una fila de bloques que se iluminan (segmentada),
/// más legible que un slider continuo en un arcade a distancia.
/// </summary>
public class NavigableSlider : MonoBehaviour, INavigableOption
{
    [SerializeField] private RectTransform titleAnchor;
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Diez TMP_Text que representan los 10 segmentos del slider (0.1 cada uno). Asignados en orden de izquierda a derecha.")]
    [SerializeField] private TMP_Text[] segments;

    [SerializeField] private Color filledColor = Color.cyan;
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] private Color normalTitleColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color selectedTitleColor = Color.cyan;

    [Tooltip("Clave de PlayerPrefs donde se guarda y se carga el valor.")]
    [SerializeField] private string prefsKey;

    [Tooltip("True = controla música. False = controla SFX.")]
    [SerializeField] private bool controlsMusic = true;

    private const float Step = 0.1f;
    private const int SegmentCount = 10;

    private float currentValue;

    public RectTransform CursorAnchor => titleAnchor != null ? titleAnchor : (RectTransform)transform;

    private void Awake()
    {
        // Si ya existe una preferencia guardada, usamos ese valor —
        // si no, arrancamos al máximo, consistente con el default de
        // AudioManager.
        currentValue = PlayerPrefs.GetFloat(prefsKey, 1f);
        UpdateVisuals();
        ApplyColor(false);
    }

    public void OnSelected() => ApplyColor(true);
    public void OnDeselected() => ApplyColor(false);
    public void OnConfirm() { }

    public void OnChangeValue(int direction)
    {
        float next = currentValue + direction * Step;

        // Mathf.Round corta errores de punto flotante acumulados
        // (0.1 + 0.1 + ... en float nunca es exactamente 1.0).
        next = Mathf.Round(next * SegmentCount) / SegmentCount;
        next = Mathf.Clamp01(next);

        if (Mathf.Approximately(next, currentValue)) return;

        currentValue = next;
        PlayerPrefs.SetFloat(prefsKey, currentValue);

        if (controlsMusic)
        {
            AudioManager.Instance?.SetMusicVolume(currentValue);
        }
        else
        {
            AudioManager.Instance?.SetSfxVolume(currentValue);
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (segments == null) return;

        int filledCount = Mathf.RoundToInt(currentValue * SegmentCount);

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null) continue;
            segments[i].color = i < filledCount ? filledColor : emptyColor;
        }
    }

    private void ApplyColor(bool selected)
    {
        if (titleText != null)
        {
            titleText.color = selected ? selectedTitleColor : normalTitleColor;
        }
    }
}