/// <summary>
/// Representa una intención de giro pendiente de aplicar.
/// </summary>
public enum TurnInput
{
    None,
    Left,
    Right
}

/// <summary>
/// Cualquier fuente de decisiones de giro (teclado, IA, red) implementa esta
/// interfaz. LightCycleController solo conoce esta interfaz, nunca una
/// implementación concreta — así se puede reemplazar la fuente de input sin
/// tocar el código de movimiento.
/// </summary>
public interface IDirectionInputProvider
{
    /// <summary>
    /// Devuelve el giro pendiente desde la última vez que se consultó, y lo
    /// consume (la próxima llamada devuelve None hasta que haya un giro
    /// nuevo). Este contrato de "consumir al leer" es lo que le permite a
    /// LightCycleController aplicar como mucho un giro por paso de
    /// movimiento, sin importar cuántas veces se haya presionado una tecla
    /// mientras tanto.
    /// </summary>
    TurnInput GetTurnInput();
}