using UnityEngine;

/// <summary>
/// Instancia una explosión de partículas en el lugar donde murió un
/// jugador, con el color de rastro de esa entidad. Se suscribe al evento
/// estático de LightCycleController — igual que hace GameManager — así
/// que no necesita ninguna referencia directa a LightCycleController, ni
/// viceversa: es un oyente más, completamente independiente.
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
            Debug.LogWarning("DeathExplosionSpawner: no hay ningún prefab asignado en 'Explosion Prefab'.");
            return;
        }

        ParticleSystem instance = Instantiate(explosionPrefab, player.transform.position, Quaternion.identity);
        instance.transform.localScale = player.transform.localScale;

        // Tintamos la explosión con el mismo color oscurecido que usa el
        // rastro de quien murió, en vez del color fijo que traía el
        // prefab — así cada entidad explota con SU propio color.
        ParticleSystem.MainModule main = instance.main;
        main.startColor = player.TrailColor;

        instance.Play();
    }
}