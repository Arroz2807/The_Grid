
using UnityEngine;

/// <summary>
/// Multiplica la velocidad del participante por Definition.magnitude
/// durante Definition.duration segundos.
///
/// Feedback visual: contorno amarillo alrededor del LightCycle.
/// Feedback sonoro: TurboStart al aplicar, TurboEnd al terminar.
/// </summary>
public class TurboEffect : PowerUpEffect
{
    private float originalInterval;

    private GameObject[] outlineInstances;

    // Grosor del contorno respecto al ancho del sprite.
    private const float OutlineThickness = 0.20f;

    public TurboEffect(
        LightCycleController target,
        PowerUpDefinition definition)
        : base(target, definition)
    {
    }

    protected override void OnApply()
    {
        if (Target == null)
            return;

        originalInterval = Target.MoveInterval;

        float speedMultiplier = Mathf.Max(
            1f,
            Definition.magnitude
        );

        float turboInterval = Mathf.Max(
            0.05f,
            originalInterval / speedMultiplier
        );

        Target.SetMoveInterval(turboInterval);

        CreateOutline();

        AudioManager.Instance?.PlaySfx(SfxId.TurboStart);

        Debug.Log(
            $"[Turbo] Aplicado a {Target.name}: " +
            $"{originalInterval:F3}s → {turboInterval:F3}s " +
            $"(x{speedMultiplier})."
        );
    }

    protected override void OnRemove()
    {
        if (Target != null)
        {
            Target.SetMoveInterval(originalInterval);
        }

        DestroyOutline();

        AudioManager.Instance?.PlaySfx(SfxId.TurboEnd);

        Debug.Log(
            $"[Turbo] Finalizado en " +
            $"{(Target != null ? Target.name : "entidad destruida")}."
        );
    }

    private void CreateOutline()
    {
        SpriteRenderer original = Target.VisualRenderer;

        outlineInstances = PowerUpOutlineRenderer.Create(
            original,
            Definition.color,
            OutlineThickness,
            "TurboOutline"
        );

        if (outlineInstances == null)
        {
            Debug.LogWarning(
                $"[Turbo] No se pudo crear el contorno de {Target.name}."
            );
        }
    }

    private void DestroyOutline()
    {
        PowerUpOutlineRenderer.Destroy(outlineInstances);
        outlineInstances = null;
    }
}
