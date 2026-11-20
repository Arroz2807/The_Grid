
using UnityEngine;

/// <summary>
/// Crea una silueta ampliada detrás del sprite original.
/// Se utiliza como contorno visual para los power-ups.
/// </summary>
public static class PowerUpOutline
{
    /// <summary>
    /// Crea un contorno a partir del SpriteRenderer original.
    /// </summary>
    public static GameObject Create(
        SpriteRenderer original,
        Color color,
        float scaleMultiplier,
        string objectName)
    {
        if (original == null || original.sprite == null)
        {
            Debug.LogWarning(
                $"[PowerUpOutline] No se pudo crear {objectName}: " +
                "falta el SpriteRenderer o su Sprite."
            );

            return null;
        }

        // La copia será hija del objeto visual original.
        // Así acompaña automáticamente su posición y rotación.
        GameObject outline = new GameObject(objectName);

        outline.transform.SetParent(
            original.transform,
            false
        );

        outline.transform.localPosition = Vector3.zero;
        outline.transform.localRotation = Quaternion.identity;

        // Ampliamos la silueta para que sobresalga del vehículo.
        outline.transform.localScale =
            Vector3.one * Mathf.Max(1.01f, scaleMultiplier);

        SpriteRenderer renderer =
            outline.AddComponent<SpriteRenderer>();

        // Reutilizamos el mismo sprite.
        renderer.sprite = original.sprite;

        // Aplicamos el color del power-up.
        renderer.color = color;

        // Conservamos las propiedades visuales principales.
        renderer.sharedMaterial = original.sharedMaterial;
        renderer.sortingLayerID = original.sortingLayerID;
        renderer.sortingOrder = original.sortingOrder - 1;

        renderer.flipX = original.flipX;
        renderer.flipY = original.flipY;
        renderer.maskInteraction = original.maskInteraction;
        renderer.drawMode = original.drawMode;
        renderer.size = original.size;
        renderer.spriteSortPoint = original.spriteSortPoint;

        return outline;
    }

    /// <summary>
    /// Destruye el contorno si todavía existe.
    /// </summary>
    public static void Destroy(GameObject outline)
    {
        if (outline != null)
        {
            Object.Destroy(outline);
        }
    }
}
