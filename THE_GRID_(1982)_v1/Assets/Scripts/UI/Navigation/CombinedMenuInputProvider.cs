
using UnityEngine;

/// <summary>
/// Combina el teclado y el joystick Arduino para navegar los menús.
///
/// El joystick tiene prioridad cuando genera una acción.
/// La repetición se controla desde Unity, no desde Arduino.
/// </summary>
public class CombinedMenuInputProvider : MonoBehaviour, IMenuInputProvider
{
    [Header("Repetición del joystick")]
    [Tooltip("Tiempo que espera antes de repetir una dirección.")]
    [SerializeField] private float initialRepeatDelay = 0.35f;

    [Tooltip("Tiempo entre repeticiones.")]
    [SerializeField] private float repeatInterval = 0.12f;

    private KeyboardMenuInput keyboardProvider;

    private char previousDirection = 'N';
    private float repeatTimer;

    // Indica que el joystick debe volver a neutral
    // antes de poder generar nuevas acciones.
    private bool waitingForNeutral;

    private void Awake()
    {
        keyboardProvider = GetComponentInChildren<KeyboardMenuInput>();

        if (keyboardProvider == null)
        {
            Debug.LogWarning(
                "[CombinedMenuInput] No se encontró KeyboardMenuInput. " +
                "El menú podrá utilizar el joystick, pero no el teclado."
            );
        }
    }

    /// <summary>
    /// Bloquea temporalmente la navegación mediante joystick.
    /// Se libera cuando el hardware vuelve a neutral.
    ///
    /// No bloquea el teclado ni modifica el estado del Arduino.
    /// </summary>
    public void RequireJoystickNeutral()
    {
        waitingForNeutral = true;

        previousDirection = 'N';
        repeatTimer = 0f;
    }

    public MenuAction GetAction()
    {
        ArduinoSerialManager serialManager = ArduinoSerialManager.Instance;

        // 1. Comprobar confirmación física.
        if (serialManager != null &&
            serialManager.TryGetConfirmCommand())
        {
            return MenuAction.Confirm;
        }

        // 2. Comprobar el joystick.
        if (serialManager != null)
        {
            char currentDirection = serialManager.GetHeldDirection();

            // Esperar a que el joystick vuelva al centro.
            if (waitingForNeutral)
            {
                if (currentDirection == 'N')
                {
                    waitingForNeutral = false;
                    previousDirection = 'N';
                    repeatTimer = 0f;
                }
            }
            else
            {
                MenuAction joystickAction = GetJoystickAction(currentDirection);

                if (joystickAction != MenuAction.None)
                    return joystickAction;
            }
        }
        else
        {
            previousDirection = 'N';
            repeatTimer = 0f;
            waitingForNeutral = false;
        }

        // 3. Consultar el teclado.
        if (keyboardProvider != null)
        {
            return keyboardProvider.GetAction();
        }

        return MenuAction.None;
    }

    /// <summary>
    /// Convierte la dirección mantenida del joystick en una acción
    /// de menú y controla su repetición.
    /// </summary>
    private MenuAction GetJoystickAction(char direction)
    {
        if (direction == 'N')
        {
            previousDirection = 'N';
            repeatTimer = 0f;
            return MenuAction.None;
        }

        if (direction != 'L' &&
            direction != 'R' &&
            direction != 'U' &&
            direction != 'D')
        {
            return MenuAction.None;
        }

        // Una dirección nueva actúa inmediatamente.
        if (direction != previousDirection)
        {
            previousDirection = direction;
            repeatTimer = initialRepeatDelay;

            return ConvertDirection(direction);
        }

        // Repetición de la dirección mantenida.
        repeatTimer -= Time.deltaTime;

        if (repeatTimer <= 0f)
        {
            repeatTimer = repeatInterval;
            return ConvertDirection(direction);
        }

        return MenuAction.None;
    }

    private static MenuAction ConvertDirection(char direction)
    {
        switch (direction)
        {
            case 'U':
                return MenuAction.MoveUp;

            case 'D':
                return MenuAction.MoveDown;

            case 'L':
                return MenuAction.DecreaseValue;

            case 'R':
                return MenuAction.IncreaseValue;

            default:
                return MenuAction.None;
        }
    }
}
