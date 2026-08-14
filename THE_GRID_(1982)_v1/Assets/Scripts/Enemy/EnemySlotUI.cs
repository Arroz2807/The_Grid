using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Un único selector de "Enemigo N: [tipo]". No sabe cuántos slots hay en
/// total ni dónde se ubican — eso es trabajo de EnemySelectionMenu. Sólo
/// sabe escribir su propia elección en MatchConfig.
/// </summary>
public class EnemySlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Dropdown typeDropdown;

    private int slotIndex;

    public void Initialize(int index)
    {
        slotIndex = index;

        if (labelText != null)
        {
            labelText.text = $"Enemigo {index + 1}";
        }

        // Las opciones salen de los nombres del enum, no tipeadas a mano
        // acá — así nunca pueden quedar desincronizadas del orden real de
        // EnemyType.
        typeDropdown.ClearOptions();
        typeDropdown.AddOptions(new List<string>(System.Enum.GetNames(typeof(EnemyType))));
        typeDropdown.value = 0;
        typeDropdown.onValueChanged.AddListener(HandleTypeChanged);

        MatchConfig.SetEnemyType(slotIndex, EnemyType.Random);
    }

    private void HandleTypeChanged(int optionIndex)
    {
        MatchConfig.SetEnemyType(slotIndex, (EnemyType)optionIndex);
    }
}