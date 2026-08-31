using UnityEngine;

public interface INavigableOption
{
    RectTransform CursorAnchor { get; }
    void OnSelected();
    void OnDeselected();
    void OnConfirm();
    void OnChangeValue(int direction);
}