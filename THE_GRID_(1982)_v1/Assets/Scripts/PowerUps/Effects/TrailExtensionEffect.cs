using System.Collections;
using UnityEngine;

/// <summary>
/// Aumenta permanentemente maxTrailLength del participante en
/// (int)definition.magnitude segmentos, hasta definition.trailLengthCap.
/// Feedback visual: flash de color durante 1 segundo.
/// Feedback sonoro: TrailExtensionPickup al aplicar.
/// </summary>
public class TrailExtensionEffect : PowerUpEffect
{
    private const float FlashDuration = 1f;

    public TrailExtensionEffect(LightCycleController target, PowerUpDefinition definition)
        : base(target, definition) { }

    protected override void OnApply()
    {
        if (Target == null) return;

        int amount = Mathf.Max(1, Mathf.RoundToInt(Definition.magnitude));
        int cap = Mathf.Max(amount + 1, Definition.trailLengthCap);

        int previousMax = Target.MaxTrailLength;
        bool applied = Target.TryIncreaseMaxTrailLength(amount, cap);

        if (applied)
        {
            Debug.Log($"[TrailExtension] {Target.name}: {previousMax} → {Target.MaxTrailLength} (tope: {cap}).");
            AudioManager.Instance?.PlaySfx(SfxId.TrailExtensionPickup);
            Target.StartCoroutine(ColorFlashRoutine());
        }
        else
        {
            Debug.Log($"[TrailExtension] {Target.name}: ya está en el tope ({cap}), sin efecto.");
        }
    }

    protected override void OnRemove() { }

    private IEnumerator ColorFlashRoutine()
    {
        SpriteRenderer sr = Target.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color originalColor = Target.NormalColor;
        sr.color = Definition.color;

        yield return new WaitForSeconds(FlashDuration);

        if (Target != null && sr != null)
        {
            sr.color = originalColor;
        }
    }
}