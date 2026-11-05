using TMPro;
using UnityEngine;

public class EnemySlotUI : MonoBehaviour, INavigableOption
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private Color normalColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color selectedColor = Color.cyan;

    private static readonly EnemyType[] AllTypes = (EnemyType[])System.Enum.GetValues(typeof(EnemyType));

    private int slotIndex;
    private EnemyType currentType;

    public RectTransform CursorAnchor => labelText != null ? labelText.rectTransform : (RectTransform)transform;

    public void Initialize(int index)
    {
        slotIndex = index;
        currentType = EnemyType.Random;

        if (labelText != null) labelText.text = $"Enemy {index + 1}";

        UpdateTypeText();
        ApplyVisual(false);

        MatchConfig.SetEnemyType(slotIndex, currentType);
    }

    public void OnSelected() => ApplyVisual(true);
    public void OnDeselected() => ApplyVisual(false);
    public void OnConfirm() { }

    public void OnChangeValue(int direction)
    {
        int currentIndex = System.Array.IndexOf(AllTypes, currentType);
        int nextIndex = (currentIndex + direction + AllTypes.Length) % AllTypes.Length;
        currentType = AllTypes[nextIndex];

        UpdateTypeText();
        Debug.Log($"{name}: tipo cambiado a {currentType}.");
        MatchConfig.SetEnemyType(slotIndex, currentType);
    }

    private void UpdateTypeText()
    {
        if (typeText != null) typeText.text = $"<  {currentType}  >";
    }

    private void ApplyVisual(bool selected)
    {
        Color color = selected ? selectedColor : normalColor;
        if (labelText != null) labelText.color = color;
        if (typeText != null) typeText.color = color;
    }
}