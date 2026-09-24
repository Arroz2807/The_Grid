using System;
using UnityEngine;

/// <summary>
/// Representa un power-up físicamente presente en la grilla. Sabe su celda
/// y su definición, pero no sabe nada de efectos ni de quién lo va a
/// recoger. Al ser recogido, dispara OnPickedUp y se destruye. La guardia
/// de doble recogida evita que dos entidades que entren a la misma celda
/// en el mismo frame lo apliquen dos veces.
/// </summary>
public class PowerUpPickup : MonoBehaviour
{
    public PowerUpDefinition Definition { get; private set; }
    public Vector2Int Cell { get; private set; }

    // Se dispara una sola vez, con la entidad que lo recogió.
    public event Action<PowerUpPickup, LightCycleController> OnPickedUp;

    private bool collected;

    public void Initialize(PowerUpDefinition definition, Vector2Int cell)
    {
        Definition = definition;
        Cell = cell;

        // Color visual del objeto en la grilla.
        if (TryGetComponent(out SpriteRenderer sr))
        {
            sr.color = definition.color;
        }
    }

    /// <summary>
    /// Llamado por PowerUpSpawner cuando una entidad entra en esta celda.
    /// </summary>
    public void Collect(LightCycleController collector)
    {
        if (collected) return;
        collected = true;

        OnPickedUp?.Invoke(this, collector);
        Destroy(gameObject);
    }
}