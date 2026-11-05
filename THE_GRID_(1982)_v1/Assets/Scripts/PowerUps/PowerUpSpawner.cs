using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orquesta todo el ciclo de vida de los power-ups en la grilla:
/// aparición, detección de recogida (sin polling — reacciona a
/// OnCellEntered de cada LightCycleController), y aplicación del efecto.
/// Recibe las referencias de entidades vivas desde GameManager, igual que
/// MatchStarter.
///
/// Los eventos estáticos OnPlayerEffectStarted/Ended viven acá (no en
/// PowerUpHUD) porque PowerUpSpawner es quien sabe cuándo ocurren los
/// eventos — PowerUpHUD sólo los escucha. En C#, un evento sólo puede
/// dispararse desde dentro de la clase que lo declara.
/// </summary>
public class PowerUpSpawner : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private PowerUpSpawnConfig config;

    [SerializeField] private GameObject fallbackPickupPrefab;

    // Celda → pickup activo. Permite detectar recogida en O(1) y evita
    // duplicados sin iterar sobre todos los pickups.
    private readonly Dictionary<Vector2Int, PowerUpPickup> activePickups =
        new Dictionary<Vector2Int, PowerUpPickup>();

    /// <summary>
    /// Acceso de sólo lectura a los pickups activos para que PowerUpSeeker
    /// pueda consultarlos sin necesitar una referencia al PowerUpSpawner
    /// completo, y sin poder modificar el diccionario accidentalmente.
    /// </summary>
    public IReadOnlyDictionary<Vector2Int, PowerUpPickup> ActivePickups => activePickups;
    // WaveManager lo necesita para ajustar los intervalos entre oleadas.
    public PowerUpSpawnConfig Config => config;

    private readonly List<LightCycleController> entities =
        new List<LightCycleController>();

    private readonly Dictionary<LightCycleController, PowerUpEffect> activeEffects =
        new Dictionary<LightCycleController, PowerUpEffect>();

    // Eventos para el HUD — declarados acá para que PowerUpSpawner pueda
    // dispararlos. PowerUpHUD se suscribe desde OnEnable/OnDisable.
    // Se usan System.Action calificado explícitamente para evitar
    // ambigüedad con System.Random al tener using System.Collections.
    public static event System.Action<string, float> OnPlayerEffectStarted;
    public static event System.Action OnPlayerEffectEnded;

    /// <summary>
    /// Se dispara al recoger un pickup. PowerUpParticlePlayer lo escucha
    /// para reproducir la ráfaga de partículas en la posición correcta
    /// con el color del tipo recogido.
    /// </summary>
    public static event System.Action<Vector3, Color> OnPowerUpCollected;

    // WaveManager puede pausar el spawner durante transiciones y ajustar
    // los intervalos entre oleadas.
    private bool spawningPaused;

    public void SetSpawningPaused(bool paused)
    {
        spawningPaused = paused;
    }

    public void SetSpawnIntervals(float minInterval, float maxInterval)
    {
        if (config == null) return;
        config.minSpawnInterval = minInterval;
        config.maxSpawnInterval = maxInterval;
    }

    private void OnEnable()
    {
        LightCycleController.OnAnyPlayerDied += HandleEntityDied;
        GameManager.OnMatchEnded += HandleMatchEnded;
    }

    private void OnDisable()
    {
        LightCycleController.OnAnyPlayerDied -= HandleEntityDied;
        GameManager.OnMatchEnded -= HandleMatchEnded;
    }

    private void HandleMatchEnded(bool playerWon)
    {
        // Detener el spawn de nuevos power-ups al terminar la partida.
        // Los pickups ya existentes en la grilla se quedan — el jugador
        // no se puede mover (isAlive = false), así que no puede recogerlos.
        StopAllCoroutines();
    }

    private void Start()
    {
        // En modo Custom, MatchConfig.PowerUpsEnabled puede desactivarlos.
        // En Waves siempre se usan — WaveManager controla la frecuencia.
        bool shouldSpawn = config != null && config.powerUpsEnabled;

        if (MatchConfig.GameMode == GameMode.Custom)
            shouldSpawn = shouldSpawn && MatchConfig.PowerUpsEnabled;

        if (shouldSpawn)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    public void RegisterEntity(LightCycleController entity)
    {
        if (entity == null || entities.Contains(entity)) return;

        entities.Add(entity);
        entity.OnCellEntered += () => HandleEntityEnteredCell(entity);
    }

    private void HandleEntityEnteredCell(LightCycleController entity)
    {
        Vector2Int cell = entity.CurrentCell;

        if (!activePickups.TryGetValue(cell, out PowerUpPickup pickup)) return;

        bool canCollect = entity.IsPlayer
            ? pickup.Definition.playerCanPickUp
            : pickup.Definition.enemyCanPickUp;

        if (!canCollect) return;

        activePickups.Remove(cell);
        pickup.Collect(entity);
    }

    private void HandlePickedUp(PowerUpPickup pickup, LightCycleController collector)
    {
        AudioManager.Instance?.PlaySfx(SfxId.PowerUpPickup);
        OnPowerUpCollected?.Invoke(pickup.transform.position, pickup.Definition.color);

        // Puntos sólo si quien recoge es el jugador — los enemigos no
        // acumulan score, evita que la IA "infle" el marcador.
        if (collector.IsPlayer)
        {
            ScoreManager.AddPowerUpScore();
        }

        Debug.Log($"[PowerUp] {pickup.Definition.displayName} recogido por {collector.name}.");

        if (activeEffects.TryGetValue(collector, out PowerUpEffect existing))
        {
            existing.Remove();
        }

        PowerUpEffect effect = PowerUpEffectFactory.Create(
            pickup.Definition.type, collector, pickup.Definition);

        effect.Apply();

        if (collector.IsPlayer && pickup.Definition.duration > 0f)
        {
            OnPlayerEffectStarted?.Invoke(
                pickup.Definition.displayName,
                pickup.Definition.duration);
        }

        if (pickup.Definition.duration > 0f)
        {
            activeEffects[collector] = effect;
            StartCoroutine(ExpireEffect(collector, effect, pickup.Definition.duration));
        }
    }

    private IEnumerator ExpireEffect(LightCycleController target, PowerUpEffect effect, float duration)
    {
        yield return new WaitForSeconds(duration);

        effect.Remove();

        if (activeEffects.TryGetValue(target, out PowerUpEffect current) && current == effect)
        {
            activeEffects.Remove(target);
        }

        if (target != null && target.IsPlayer)
        {
            OnPlayerEffectEnded?.Invoke();
        }
    }

    private void HandleEntityDied(LightCycleController entity)
    {
        if (activeEffects.TryGetValue(entity, out PowerUpEffect effect))
        {
            effect.Remove();
            activeEffects.Remove(entity);

            if (entity.IsPlayer)
            {
                OnPlayerEffectEnded?.Invoke();
            }
        }

        entities.Remove(entity);
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(config.minSpawnInterval);

        while (true)
        {
            if (!spawningPaused && activePickups.Count < config.maxSimultaneous)
            {
                TrySpawnPickup();
            }

            // UnityEngine.Random calificado explícitamente para evitar
            // ambigüedad con System.Random (que entra por System.Collections).
            float wait = UnityEngine.Random.Range(config.minSpawnInterval, config.maxSpawnInterval);
            yield return new WaitForSeconds(wait);
        }
    }

    private void TrySpawnPickup()
    {
        PowerUpDefinition definition = PickRandomDefinition();
        if (definition == null) return;

        if (!TryFindValidCell(out Vector2Int cell)) return;

        SpawnPickup(definition, cell);
    }

    private PowerUpDefinition PickRandomDefinition()
    {
        if (config.availableDefinitions == null || config.availableDefinitions.Length == 0)
            return null;

        float totalWeight = 0f;
        foreach (PowerUpDefinition def in config.availableDefinitions)
        {
            if (def != null && def.enabled) totalWeight += def.spawnWeight;
        }

        if (totalWeight <= 0f) return null;

        // UnityEngine.Random calificado explícitamente.
        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (PowerUpDefinition def in config.availableDefinitions)
        {
            if (def == null || !def.enabled) continue;
            cumulative += def.spawnWeight;
            if (roll <= cumulative) return def;
        }

        return null;
    }

    private bool TryFindValidCell(out Vector2Int result)
    {
        for (int attempt = 0; attempt < config.maxPlacementAttempts; attempt++)
        {
            if (!gridManager.TryGetRandomFreeCell(1, out Vector2Int candidate))
                continue;

            if (activePickups.ContainsKey(candidate))
                continue;

            if (!IsFarEnoughFromParticipants(candidate))
                continue;

            result = candidate;
            return true;
        }

        result = Vector2Int.zero;
        return false;
    }

    private bool IsFarEnoughFromParticipants(Vector2Int cell)
    {
        foreach (LightCycleController entity in entities)
        {
            if (entity == null) continue;

            int dist = Mathf.Abs(entity.CurrentCell.x - cell.x)
                     + Mathf.Abs(entity.CurrentCell.y - cell.y);

            if (dist < config.minDistanceFromParticipants) return false;
        }

        return true;
    }

    private void SpawnPickup(PowerUpDefinition definition, Vector2Int cell)
    {
        GameObject prefab = definition.pickupPrefab != null
            ? definition.pickupPrefab
            : fallbackPickupPrefab;

        if (prefab == null)
        {
            Debug.LogWarning($"[PowerUp] No hay prefab para {definition.displayName} ni fallback asignado.");
            return;
        }

        Vector3 worldPos = gridManager.GridToWorld(cell);
        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, transform);

        PowerUpPickup pickup = instance.GetComponent<PowerUpPickup>();
        if (pickup == null)
        {
            Debug.LogError($"[PowerUp] El prefab '{prefab.name}' no tiene componente PowerUpPickup.");
            Destroy(instance);
            return;
        }

        pickup.Initialize(definition, cell);
        pickup.OnPickedUp += HandlePickedUp;

        activePickups[cell] = pickup;

        Debug.Log($"[PowerUp] {definition.displayName} apareció en {cell}.");
        AudioManager.Instance?.PlaySfx(SfxId.PowerUpSpawn);
    }
}