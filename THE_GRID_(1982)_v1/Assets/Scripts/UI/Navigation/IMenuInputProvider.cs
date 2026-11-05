/// <summary>
/// Cualquier fuente de entrada para los menús (teclado, más adelante un
/// joystick físico de arcade) implementa esta interfaz. MenuNavigator
/// sólo conoce esta interfaz, nunca KeyboardMenuInput directamente — así
/// se puede reemplazar la fuente de input sin tocar el código de
/// navegación, exactamente el mismo motivo por el que existe
/// IDirectionInputProvider del lado del jugador.
/// </summary>
public interface IMenuInputProvider
{
    /// <summary>
    /// La acción pedida en este frame, o MenuAction.None si no hay
    /// ninguna. Se consulta una vez por frame desde MenuNavigator.Update().
    /// </summary>
    MenuAction GetAction();
}