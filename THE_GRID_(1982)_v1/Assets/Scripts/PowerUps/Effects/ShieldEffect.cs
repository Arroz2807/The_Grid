using UnityEngine;

/// <summary>
/// Activa el escudo de LightCycleController. Al consumirse destruye el
/// visual y reproduce el sonido de bloqueo. Feedback visual: outline de
/// 8 puntos circular para distinguirse del Turbo. Feedback sonoro:
/// ShieldActivate al aplicar, ShieldBlock al absorber el impacto.
/// </summary>
public class ShieldEffect : PowerUpEffect
{
    private GameObject[] outlineInstances;
    private const float OutlineOffsetFactor = 0.15f;

    public ShieldEffect(LightCycleController target, PowerUpDefinition definition)
        : base(target, definition) { }

    protected override void OnApply()
    {
        if (Target == null) return;

        Target.ActivateShield();
        LightCycleController.OnShieldConsumed += HandleShieldConsumed;

        CreateOutline();
        AudioManager.Instance?.PlaySfx(SfxId.ShieldActivate);

        Debug.Log($"[Escudo] Activado en {Target.name}.");
    }

    protected override void OnRemove()
    {
        LightCycleController.OnShieldConsumed -= HandleShieldConsumed;

        if (Target != null)
        {
            Target.DeactivateShield();
        }

        DestroyOutline();
        Debug.Log($"[Escudo] Desactivado en {(Target != null ? Target.name : "entidad destruida")}.");
    }

    private void HandleShieldConsumed(LightCycleController consumed)
    {
        if (consumed != Target) return;

        LightCycleController.OnShieldConsumed -= HandleShieldConsumed;
        DestroyOutline();
        AudioManager.Instance?.PlaySfx(SfxId.ShieldBlock);

        Debug.Log($"[Escudo] Absorbió un impacto en {Target.name}.");
    }

    private void CreateOutline()
    {
        SpriteRenderer original = Target.GetComponent<SpriteRenderer>();
        if (original == null || original.sprite == null) return;

        float offset = Target.transform.localScale.x * OutlineOffsetFactor;
        float d = offset * 0.707f;
        Vector3[] offsets =
        {
            new Vector3( offset,  0,     0), new Vector3(-offset,  0,     0),
            new Vector3( 0,       offset, 0), new Vector3( 0,      -offset, 0),
            new Vector3( d,       d,     0), new Vector3(-d,       d,     0),
            new Vector3( d,      -d,     0), new Vector3(-d,      -d,     0),
        };

        outlineInstances = new GameObject[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject copy = new GameObject($"ShieldOutline_{i}");
            copy.transform.SetParent(Target.transform, worldPositionStays: false);
            copy.transform.localPosition = offsets[i];
            copy.transform.localScale = Vector3.one;
            SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
            sr.sprite = original.sprite;
            sr.color = Definition.color;
            sr.sortingLayerID = original.sortingLayerID;
            sr.sortingOrder = original.sortingOrder - 1;
            outlineInstances[i] = copy;
        }
    }

    private void DestroyOutline()
    {
        if (outlineInstances == null) return;
        foreach (GameObject outline in outlineInstances)
        {
            if (outline != null) Object.Destroy(outline);
        }
        outlineInstances = null;
    }
}