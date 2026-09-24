using UnityEngine;

/// <summary>
/// Reproduce una ráfaga de partículas en la posición de recogida de un
/// power-up. Mismo patrón que DeathExplosionSpawner: escucha un evento,
/// instancia partículas, las destruye solas (Stop Action = Destroy). El
/// color de la ráfaga se toma del color de la definición del pickup para
/// dar feedback visual inmediato del tipo recogido sin necesitar prefabs
/// separados por tipo.
/// </summary>
public class PowerUpParticlePlayer : MonoBehaviour
{
    [SerializeField] private ParticleSystem collectParticlePrefab;

    private void OnEnable()
    {
        PowerUpSpawner.OnPowerUpCollected += HandleCollected;
    }

    private void OnDisable()
    {
        PowerUpSpawner.OnPowerUpCollected -= HandleCollected;
    }

    private void HandleCollected(Vector3 worldPosition, Color color)
    {
        if (collectParticlePrefab == null) return;

        ParticleSystem instance = Instantiate(
            collectParticlePrefab,
            worldPosition,
            Quaternion.identity);

        ParticleSystem.MainModule main = instance.main;
        main.startColor = color;
        main.stopAction = ParticleSystemStopAction.Destroy;

        instance.Play();
    }
}