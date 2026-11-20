
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
        if (definition == null)
        {
            Debug.LogError("[PowerUpPickup] Se intentó inicializar un pickup sin definición.", this);
            return;
        }

        Definition = definition;
        Cell = cell;

        // Configuración visual.
        if (TryGetComponent(out SpriteRenderer sr))
        {
            if (definition.pickupSprite != null)
            {
                sr.sprite = definition.pickupSprite;
            }
            else
            {
                Debug.LogWarning(
                    $"[PowerUpPickup] {definition.displayName} no tiene un sprite asignado.",
                    this
                );
            }

            // Blanco conserva los colores originales del sprite.
            // El color de la definición sigue disponible para las partículas.
            sr.color = Color.white;
        }
        else
        {
            Debug.LogError(
                "[PowerUpPickup] El prefab no tiene un SpriteRenderer.",
                this
            );
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
