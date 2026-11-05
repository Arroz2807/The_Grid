
using UnityEngine;

/// <summary>
/// Combina el teclado y el joystick Arduino en una única fuente de input.
///
/// Prioridad:
/// 1. Joystick, si hay una dirección nueva.
/// 2. Teclado, si el joystick no tiene una dirección nueva.
///
/// Si ambos tienen una dirección pendiente, se consumen ambas,
/// pero solamente se devuelve la del joystick.
/// </summary>
public class CombinedDirectionInputProvider : MonoBehaviour, IDirectionInputProvider
{
    private KeyboardInputProvider keyboardProvider;
    private ArduinoDirectionInputProvider arduinoProvider;

    private void Awake()
    {
        // Buscamos cada proveedor por su tipo concreto.
        // No usamos GetComponent<IDirectionInputProvider>() aquí,
        // porque ambos implementan esa misma interfaz.
        keyboardProvider = GetComponent<KeyboardInputProvider>();
        arduinoProvider = GetComponent<ArduinoDirectionInputProvider>();

        // Los sonidos se reproducen desde este proveedor combinado,
        // así evitamos reproducirlos dos veces si coinciden los inputs.
        if (keyboardProvider != null)
        {
            keyboardProvider.SetTurnSoundEnabled(false);
        }

        if (arduinoProvider != null)
        {
            arduinoProvider.SetTurnSoundEnabled(false);
        }

        if (keyboardProvider == null && arduinoProvider == null)
        {
            Debug.LogError(
                $"[CombinedInput] {name}: no se encontró " +
                "KeyboardInputProvider ni ArduinoDirectionInputProvider."
            );
        }
        else
        {
            Debug.Log(
                $"[CombinedInput] Inicializado. " +
                $"Teclado: {keyboardProvider != null}, " +
                $"Joystick: {arduinoProvider != null}."
            );
        }
    }

    public Vector2Int? GetDesiredDirection()
    {
        // Consultamos ambas fuentes en cada lectura.
        // Es importante consumir también el teclado cuando coinciden
        // ambos inputs, para que no quede una tecla antigua pendiente.
        Vector2Int? joystickDirection = null;
        Vector2Int? keyboardDirection = null;

        if (arduinoProvider != null)
        {
            joystickDirection = arduinoProvider.GetDesiredDirection();
        }

        if (keyboardProvider != null)
        {
            keyboardDirection = keyboardProvider.GetDesiredDirection();
        }

        // El joystick tiene prioridad si produjo una dirección nueva.
        if (joystickDirection.HasValue)
        {
            if (keyboardDirection.HasValue)
            {
                Debug.Log(
                    "[CombinedInput] Coincidieron joystick y teclado. " +
                    $"Gana el joystick: {joystickDirection.Value}; " +
                    $"se descarta el teclado: {keyboardDirection.Value}."
                );
            }
            else
            {
                Debug.Log(
                    $"[CombinedInput] Dirección seleccionada del joystick: " +
                    $"{joystickDirection.Value}."
                );
            }

            AudioManager.Instance?.PlaySfx(SfxId.Turn);
            return joystickDirection;
        }

        // Si el joystick no tiene una dirección nueva, usamos el teclado.
        if (keyboardDirection.HasValue)
        {
            Debug.Log(
                $"[CombinedInput] Dirección seleccionada del teclado: " +
                $"{keyboardDirection.Value}."
            );

            AudioManager.Instance?.PlaySfx(SfxId.Turn);
            return keyboardDirection;
        }

        // Ningún control pidió un giro: se mantiene el movimiento actual.
        return null;
    }
}
