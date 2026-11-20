
using UnityEngine;

/// <summary>
/// Genera un contorno visual alrededor de un SpriteRenderer
/// mediante copias del mismo sprite distribuidas en círculo.
///
/// Las copias:
/// - Acompañan la posición y rotación del sprite original.
/// - Utilizan su mismo material y Sorting Layer.
/// - Se dibujan detrás del sprite original.
/// - Se desplazan según el tamaño real del sprite en el mundo.
/// </summary>
public static class PowerUpOutlineRenderer
{
    private const int DefaultSegments = 16;

    /// <summary>
    /// Crea un contorno alrededor del SpriteRenderer indicado.
    ///
    /// thicknessFactor representa el grosor en relación
    /// con la dimensión más pequeña del sprite.
    /// </summary>
    public static GameObject[] Create(
        SpriteRenderer original,
        Color color,
        float thicknessFactor,
        string objectName,
        int segments = DefaultSegments)
    {
        if (original == null || original.sprite == null)
        {
            Debug.LogWarning(
                "[PowerUpOutlineRenderer] No se puede crear " +
                "el contorno: falta el SpriteRenderer o su Sprite."
            );

            return null;
        }

        if (segments < 4)
            segments = 4;

        // -----------------------------------------------------
        // 1. CALCULAR EL GROSOR REAL EN EL MUNDO
        // -----------------------------------------------------

        // Bounds considera el tamaño real del sprite incluyendo
        // la escala de Visual y la transformación de sus padres.
        Vector3 worldSize = original.bounds.size;

        float smallestDimension = Mathf.Min(
            worldSize.x,
            worldSize.y
        );

        if (smallestDimension <= 0.0001f)
        {
            Debug.LogWarning(
                "[PowerUpOutlineRenderer] El tamaño del sprite " +
                "no es válido."
            );

            return null;
        }

        float thickness = smallestDimension *
            Mathf.Clamp(thicknessFactor, 0.05f, 0.5f);

        // Convertimos el desplazamiento del mundo a coordenadas
        // locales. Esto evita depender de la escala de la raíz.
        Vector3 worldScale = original.transform.lossyScale;

        float scaleX = Mathf.Max(
            Mathf.Abs(worldScale.x),
            0.0001f
        );

        float scaleY = Mathf.Max(
            Mathf.Abs(worldScale.y),
            0.0001f
        );

        float localOffsetX = thickness / scaleX;
        float localOffsetY = thickness / scaleY;

        // -----------------------------------------------------
        // 2. PREPARAR LAS COPIAS
        // -----------------------------------------------------

        GameObject[] instances = new GameObject[segments];

        for (int i = 0; i < segments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / segments;

            float offsetX =
                Mathf.Cos(angle) * localOffsetX;

            float offsetY =
                Mathf.Sin(angle) * localOffsetY;

            GameObject copy = new GameObject(
                $"{objectName}_{i:00}"
            );

            // Al ser hijo del Visual, acompaña sus movimientos
            // y rotaciones automáticamente.
            copy.transform.SetParent(
                original.transform,
                false
            );

            copy.transform.localPosition = new Vector3(
                offsetX,
                offsetY,
                0f
            );

            copy.transform.localRotation =
                Quaternion.identity;

            copy.transform.localScale = Vector3.one;

            // -------------------------------------------------
            // 3. CONFIGURAR EL SPRITE RENDERER
            // -------------------------------------------------

            SpriteRenderer renderer =
                copy.AddComponent<SpriteRenderer>();

            renderer.sprite = original.sprite;
            renderer.color = color;

            renderer.sharedMaterial =
                original.sharedMaterial;

            renderer.sortingLayerID =
                original.sortingLayerID;

            // El contorno queda detrás del sprite original.
            // Las partes que sobresalen forman el borde visible.
            renderer.sortingOrder =
                original.sortingOrder - 1;

            renderer.flipX = original.flipX;
            renderer.flipY = original.flipY;

            renderer.maskInteraction =
                original.maskInteraction;

            renderer.spriteSortPoint =
                original.spriteSortPoint;

            renderer.drawMode =
                original.drawMode;

            renderer.size = original.size;

            renderer.enabled = original.enabled;

            instances[i] = copy;
        }

        return instances;
    }

    /// <summary>
    /// Destruye todas las copias que forman un contorno.
    /// </summary>
    public static void Destroy(GameObject[] instances)
    {
        if (instances == null)
            return;

        foreach (GameObject instance in instances)
        {
            if (instance != null)
            {
                Object.Destroy(instance);
            }
        }
    }
}
