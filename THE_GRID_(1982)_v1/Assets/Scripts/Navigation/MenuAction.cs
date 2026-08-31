/// <summary>
/// Las únicas acciones que un menú navegable entiende, sin importar de
/// dónde vengan (teclado hoy, joystick físico más adelante).
/// </summary>
public enum MenuAction
{
    None,
    MoveUp,
    MoveDown,
    DecreaseValue,
    IncreaseValue,
    Confirm
}