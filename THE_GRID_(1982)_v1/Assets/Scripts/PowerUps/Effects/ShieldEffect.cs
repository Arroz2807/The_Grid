
using UnityEngine;

/// <summary>
/// Activa el escudo de LightCycleController.
///
/// Feedback visual: contorno celeste alrededor del LightCycle.
/// Feedback sonoro:
/// - ShieldActivate al aplicar.
/// - ShieldBlock al absorber el impacto.
/// </summary>
public class ShieldEffect : PowerUpEffect
{
    private GameObject[] outlineInstances;

    // El escudo tendrá un contorno más grueso que el Turbo.
    private const float OutlineThickness = 0.30f;

    public ShieldEffect(
        LightCycleController target,
        PowerUpDefinition definition)
        : base(target, definition)
    {
    }

    protected override void OnApply()
    {
        if (Target == null)
            return;

        Target.ActivateShield();

        LightCycleController.OnShieldConsumed +=
            HandleShieldConsumed;

        CreateOutline();

        AudioManager.Instance?.PlaySfx(SfxId.ShieldActivate);

        Debug.Log($"[Escudo] Activado en {Target.name}.");
    }

    protected override void OnRemove()
    {
        LightCycleController.OnShieldConsumed -=
            HandleShieldConsumed;

        if (Target != null)
        {
            Target.DeactivateShield();
        }

        DestroyOutline();

        Debug.Log(
            $"[Escudo] Desactivado en " +
            $"{(Target != null ? Target.name : "entidad destruida")}."
        );
    }

    private void HandleShieldConsumed(
        LightCycleController consumed)
    {
        if (consumed != Target)
            return;

        LightCycleController.OnShieldConsumed -=
            HandleShieldConsumed;

        DestroyOutline();

        AudioManager.Instance?.PlaySfx(SfxId.ShieldBlock);

        Debug.Log($"[Escudo] Absorbió un impacto en {Target.name}.");
    }

    private void CreateOutline()
    {
        SpriteRenderer original = Target.VisualRenderer;

        outlineInstances = PowerUpOutlineRenderer.Create(
            original,
            Definition.color,
            OutlineThickness,
            "ShieldOutline"
        );

        if (outlineInstances == null)
        {
            Debug.LogWarning(
                $"[Escudo] No se pudo crear el contorno de {Target.name}."
            );
        }
    }

    private void DestroyOutline()
    {
        PowerUpOutlineRenderer.Destroy(outlineInstances);
        outlineInstances = null;
    }
}
