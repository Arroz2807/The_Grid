using UnityEngine;

/// <summary>
/// Multiplica la velocidad del participante por definition.magnitude
/// durante definition.duration segundos. magnitude es un multiplicador
/// directo: 1.5 = 50% más rápido, 2.0 = el doble de rápido.
/// Feedback visual: outline de 4 puntos en cruz mientras dura el efecto.
/// Feedback sonoro: TurboStart al aplicar, TurboEnd al terminar.
/// </summary>
public class TurboEffect : PowerUpEffect
{
    private float originalInterval;
    private GameObject[] outlineInstances;
    private const float OutlineOffsetFactor = 0.08f;

    public TurboEffect(LightCycleController target, PowerUpDefinition definition)
        : base(target, definition) { }

    protected override void OnApply()
    {
        if (Target == null) return;

        originalInterval = Target.MoveInterval;
        float speedMultiplier = Mathf.Max(1f, Definition.magnitude);
        float turbInterval = Mathf.Max(0.05f, originalInterval / speedMultiplier);
        Target.SetMoveInterval(turbInterval);

        CreateOutline();
        AudioManager.Instance?.PlaySfx(SfxId.TurboStart);

        Debug.Log($"[Turbo] Aplicado a {Target.name}: {originalInterval:F3}s → {turbInterval:F3}s (x{speedMultiplier}).");
    }

    protected override void OnRemove()
    {
        if (Target != null)
        {
            Target.SetMoveInterval(originalInterval);
        }

        DestroyOutline();
        AudioManager.Instance?.PlaySfx(SfxId.TurboEnd);

        Debug.Log($"[Turbo] Finalizado en {(Target != null ? Target.name : "entidad destruida")}.");
    }

    private void CreateOutline()
    {
        SpriteRenderer original = Target.GetComponent<SpriteRenderer>();
        if (original == null || original.sprite == null) return;

        float offset = Target.transform.localScale.x * OutlineOffsetFactor;
        Vector3[] offsets =
        {
            new Vector3( offset,  0,      0),
            new Vector3(-offset,  0,      0),
            new Vector3( 0,       offset, 0),
            new Vector3( 0,      -offset, 0),
        };

        outlineInstances = new GameObject[offsets.Length];
        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject copy = new GameObject($"TurboOutline_{i}");
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