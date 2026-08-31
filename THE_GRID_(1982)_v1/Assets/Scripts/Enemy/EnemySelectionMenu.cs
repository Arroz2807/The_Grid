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
    [SerializeField] private NavigableButton backOption;
    [SerializeField] private NavigableButton startOption;

    private readonly List<GameObject> activeSlots = new List<GameObject>();

    private void OnEnable()
    {
        enemyCountOption.OnValueChanged += HandleCountChanged;
    }

    private void OnDisable()
    {
        enemyCountOption.OnValueChanged -= HandleCountChanged;
    }

    private void Start()
    {
        // Start(), no OnEnable(): Unity garantiza que TODOS los Awake()
        // de este lote de activación ya corrieron antes de cualquier
        // Start() — a diferencia de OnEnable(), donde el orden entre
        // distintos GameObjects no está garantizado. Leer
        // enemyCountOption.CurrentValue desde OnEnable() podía ejecutarse
        // antes de que NavigableCounter.Awake() inicializara su valor,
        // leyendo el 0 por defecto de un int en vez del 1 real.
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

        options.Add(startOption);

        navigator.SetOptions(options);
    }
}