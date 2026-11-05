
using UnityEngine;

/// <summary>
/// Traduce los comandos recibidos por ArduinoSerialManager
/// a direcciones cardinales para el movimiento del ciclo.
/// </summary>
public class ArduinoDirectionInputProvider : MonoBehaviour, IDirectionInputProvider
{
    [SerializeField]
    private bool playTurnSound = true;

    /// <summary>
    /// Permite que un proveedor combinado controle el sonido.
    /// </summary>
    public void SetTurnSoundEnabled(bool enabled)
    {
        playTurnSound = enabled;
    }

    public Vector2Int? GetDesiredDirection()
    {
        ArduinoSerialManager serialManager = ArduinoSerialManager.Instance;

        if (serialManager == null)
        {
            Debug.LogWarning(
                "[ArduinoInput] No existe ArduinoSerialManager."
            );
            return null;
        }

        if (!serialManager.TryGetLatestCommand(out char command))
            return null;

        Vector2Int? direction = ConvertCommandToDirection(command);

        if (direction.HasValue)
        {
            Debug.Log(
                $"[ArduinoInput] Comando recibido: '{command}' " +
                $"-> dirección solicitada: {direction.Value}"
            );

            if (playTurnSound)
            {
                AudioManager.Instance?.PlaySfx(SfxId.Turn);
            }
        }
        else
        {
            Debug.LogWarning(
                $"[ArduinoInput] Comando desconocido: '{command}'."
            );
        }

        return direction;
    }

    private static Vector2Int? ConvertCommandToDirection(char command)
    {
        switch (command)
        {
            case 'L':
                return Vector2Int.left;

            case 'R':
                return Vector2Int.right;

            case 'U':
                return Vector2Int.up;

            case 'D':
                return Vector2Int.down;

            default:
                return null;
        }
    }
}
