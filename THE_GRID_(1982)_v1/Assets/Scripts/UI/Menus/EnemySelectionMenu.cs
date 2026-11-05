using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Construye dinámicamente un EnemySlotUI por cada enemigo elegido, y
/// mantiene actualizada la lista completa de opciones navegables de este
/// panel, entregándosela a MenuNavigator cada vez que cambia. No navega
/// ni mueve ningún cursor — eso es responsabilidad exclusiva de
/// MenuNavigator.
/// </summary>
public class EnemySelectionMenu : MonoBehaviour
{
    [SerializeField] private MenuNavigator navigator;
    [SerializeField] private NavigableCounter enemyCountOption;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject enemySlotPrefab;
    [SerializeField] private NavigableToggle powerUpsToggle;
    [SerializeField] private NavigableButton backOption;
    [SerializeField] private NavigableButton startOption;

    private readonly List<GameObject> activeSlots = new List<GameObject>();

    private void OnEnable()
    {
        enemyCountOption.OnValueChanged += HandleCountChanged;

        // Reconstruir la lista cada vez que el panel se activa — no sólo
        // en Start(). Así el cursor siempre arranca en EnemyCount al
        // volver al panel desde otro menú, en vez de quedarse en el
        // último índice visitado (normalmente Back).
        // La guardia evita correr esto antes de que Awake() de
        // NavigableCounter haya inicializado CurrentValue.
        if (enemyCountOption != null && enemyCountOption.CurrentValue > 0)
        {
            HandleCountChanged(enemyCountOption.CurrentValue);
        }
    }

    private void OnDisable()
    {
        enemyCountOption.OnValueChanged -= HandleCountChanged;
    }

    private void Start()
    {
        // Primera carga: Start() garantiza que NavigableCounter.Awake()
        // ya corrió y CurrentValue está inicializado.
        HandleCountChanged(enemyCountOption.CurrentValue);
    }

    private void HandleCountChanged(int count)
    {
        MatchConfig.SetEnemyCount(count);
        RebuildSlotsAndOptions(count);
    }

    private void RebuildSlotsAndOptions(int count)
    {
        Debug.Log($"{name}: reconstruyendo slots para {count} enemigo(s).");

        foreach (GameObject slot in activeSlots)
        {
            Destroy(slot);
        }
        activeSlots.Clear();

        // Back va primero: ahora vive visualmente arriba a la izquierda,
        // separado del resto del flujo — ponerlo primero en la lista
        // evita un salto grande del cursor entre Start (al final) y Back.
        List<INavigableOption> options = new List<INavigableOption> { backOption, enemyCountOption };

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(enemySlotPrefab, slotContainer);
            EnemySlotUI slotUI = instance.GetComponent<EnemySlotUI>();
            slotUI.Initialize(i);

            activeSlots.Add(instance);
            options.Add(slotUI);
        }

        // Toggle de power-ups después de los slots, antes de Start.
        if (powerUpsToggle != null) options.Add(powerUpsToggle);

        options.Add(startOption);

        // índice 1 = EnemyCount (back es 0)
        navigator.SetOptions(options, initialIndex: 1);
    }
}