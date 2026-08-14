using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Construye dinámicamente un EnemySlotUI por cada enemigo elegido en el
/// dropdown de cantidad (0 a 4). No navega entre paneles ni carga escenas
/// — eso es responsabilidad de MainMenuManager; esta clase sólo arma el
/// contenido de este panel.
/// </summary>
public class EnemySelectionMenu : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown countDropdown;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject enemySlotPrefab;

    private readonly List<GameObject> activeSlots = new List<GameObject>();

    // OnEnable, no Start: este panel arranca inactivo y se activa recién
    // cuando el jugador aprieta "Jugar" en el menú principal. Con Start()
    // esto sólo correría la primera vez; con OnEnable() se reconstruye
    // cada vez que el panel se vuelve a mostrar, conservando la última
    // cantidad elegida si el jugador va y vuelve.
    private void OnEnable()
    {
        countDropdown.ClearOptions();
        countDropdown.AddOptions(new List<string> { "0", "1", "2", "3", "4" });
        countDropdown.value = 0;
        countDropdown.onValueChanged.AddListener(HandleCountChanged);

        HandleCountChanged(countDropdown.value);
    }

    private void OnDisable()
    {
        countDropdown.onValueChanged.RemoveListener(HandleCountChanged);
    }

    private void HandleCountChanged(int count)
    {
        MatchConfig.SetEnemyCount(count);
        RebuildSlots(count);
    }

    private void RebuildSlots(int count)
    {
        // Destruir y reconstruir todo es simple y de sobra para un máximo
        // de 4 elementos — no vale la pena la complejidad extra de
        // agregar/quitar slots de a uno para una escala tan chica.
        foreach (GameObject slot in activeSlots)
        {
            Destroy(slot);
        }
        activeSlots.Clear();

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(enemySlotPrefab, slotContainer);
            instance.GetComponent<EnemySlotUI>().Initialize(i);
            activeSlots.Add(instance);
        }
    }
}