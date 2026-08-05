using UnityEngine;

/// <summary>
/// Instancia una explosión de partículas en el lugar donde murió un
/// jugador. Se suscribe al evento estático de LightCycleController — igual
/// que hace GameManager — así que no necesita ninguna referencia directa a
/// LightCycleController, ni viceversa: es un oyente más, completamente
/// independiente.
/// </summary>
public class DeathExplosionSpawner : MonoBehaviour
{
    [Tooltip("Prefab con un Particle System configurado para reproducirse una única vez (sin loop), Stop Action = Destroy y Culling Mode = Always Simulate.")]
    [SerializeField] private ParticleSystem explosionPrefab;

    private void OnEnable()
    {
        LightCycleController.OnAnyPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        LightCycleController.OnAnyPlayerDied -= HandlePlayerDied;
    }

    private void HandlePlayerDied(LightCycleController player)
    {
        if (explosionPrefab == null)
        {
            // Si esto llega a aparecer en la consola, el campo Explosion
            // Prefab quedó sin asignar (por ejemplo, después de tocar el
            // script) — es la explicación más simple si algún día vuelve a
            // fallar por completo, no sólo a veces.
            Debug.LogWarning("DeathExplosionSpawner: no hay ningún prefab asignado en 'Explosion Prefab'.");
            return;
        }

        ParticleSystem instance = Instantiate(explosionPrefab, player.transform.position, Quaternion.identity);
        instance.transform.localScale = player.transform.localScale;

        // Se llama a Play() de forma explícita en vez de confiar
        // únicamente en "Play On Awake" del prefab: así el disparo de la
        // explosión no depende de que ese casillero del Inspector esté
        // tildado correctamente, y es determinístico sin importar el
        // estado del prefab.
        instance.Play();

        // No hace falta destruir el GameObject a mano: el prefab tiene
        // Stop Action = Destroy configurado en el Inspector.
    }
}