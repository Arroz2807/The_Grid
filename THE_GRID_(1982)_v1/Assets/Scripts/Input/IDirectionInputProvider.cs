using UnityEngine;

/// <summary>
/// Cualquier fuente de decisiones de dirección (teclado, IA, red) implementa
/// esta interfaz. LightCycleController sólo conoce esta interfaz, nunca una
/// implementación concreta — así se puede reemplazar la fuente de input sin
/// tocar el código de movimiento.
/// </summary>
public interface IDirectionInputProvider
{
    /// <summary>
    /// Devuelve la dirección absoluta deseada desde la última vez que se
    /// consultó, y la consume (la próxima llamada devuelve null hasta que
    /// haya una dirección nueva). Debe ser una de las cuatro direcciones
    /// cardinales — LightCycleController decide si esa dirección es
    /// aceptable (por ejemplo, rechazando un giro de 180°), no esta
    /// interfaz ni quien la implementa.
    /// </summary>
    Vector2Int? GetDesiredDirection();
}