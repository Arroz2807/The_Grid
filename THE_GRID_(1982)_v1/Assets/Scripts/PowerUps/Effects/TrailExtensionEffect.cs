
using UnityEngine;

/// <summary>
/// Aumenta permanentemente maxTrailLength del participante en
/// (int)definition.magnitude segmentos, hasta definition.trailLengthCap.
///
/// Feedback visual: flash de color durante 1 segundo.
/// Feedback sonoro: TrailExtensionPickup al aplicar.
/// </summary>
public class TrailExtensionEffect : PowerUpEffect
{
    private const float FlashDuration = 1f;

    public TrailExtensionEffect(
        LightCycleController target,
        PowerUpDefinition definition)
        : base(target, definition)
    {
    }

    protected override void OnApply()
    {
        if (Target == null) return;

        int amount = Mathf.Max(
            1,
            Mathf.RoundToInt(Definition.magnitude)
        );

        int cap = Mathf.Max(
            amount + 1,
            Definition.trailLengthCap
        );

        int previousMax = Target.MaxTrailLength;

        bool applied = Target.TryIncreaseMaxTrailLength(
            amount,
            cap
        );

        if (applied)
        {
            Debug.Log(
                $"[TrailExtension] {Target.name}: " +
                $"{previousMax} → {Target.MaxTrailLength} " +
                $"(tope: {cap})."
            );

            AudioManager.Instance?.PlaySfx(
                SfxId.TrailExtensionPickup
            );

            // El controlador administra el renderer y el flash.
            Target.FlashVisualColor(
                Definition.color,
                FlashDuration
            );
        }
        else
        {
            Debug.Log(
                $"[TrailExtension] {Target.name}: " +
                $"ya está en el tope ({cap}), sin efecto."
            );
        }
    }

    protected override void OnRemove()
    {
        // La extensión de rastro es permanente.
        // No hay nada que restaurar al finalizar.
    }
}
