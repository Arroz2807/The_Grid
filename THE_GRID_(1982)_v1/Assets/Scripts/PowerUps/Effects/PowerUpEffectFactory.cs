using UnityEngine;

/// <summary>
/// Traduce un PowerUpType a una instancia concreta de PowerUpEffect. Mismo
/// patrón que EnemyBehaviorFactory — el único lugar que necesita tocarse al
/// agregar un nuevo tipo de power-up. Hoy devuelve efectos vacíos para
/// Shield y TrailExtension (se completan en las etapas 4 y 5).
/// </summary>
public static class PowerUpEffectFactory
{
    public static PowerUpEffect Create(PowerUpType type, LightCycleController target, PowerUpDefinition definition)
    {
        switch (type)
        {
            case PowerUpType.Turbo:
                return new TurboEffect(target, definition);

            case PowerUpType.Shield:
                return new ShieldEffect(target, definition);

            case PowerUpType.TrailExtension:
                return new TrailExtensionEffect(target, definition);

            default:
                Debug.LogWarning($"PowerUpType desconocido: {type}");
                return new NullEffect(target, definition);
        }
    }
}

/// <summary>
/// Efecto vacío para tipos todavía no implementados. No aplica nada, no
/// restaura nada. Permite probar el flujo de recogida sin lógica de efecto.
/// </summary>
public class NullEffect : PowerUpEffect
{
    public NullEffect(LightCycleController target, PowerUpDefinition definition)
        : base(target, definition) { }

    protected override void OnApply() { }
    protected override void OnRemove() { }
}