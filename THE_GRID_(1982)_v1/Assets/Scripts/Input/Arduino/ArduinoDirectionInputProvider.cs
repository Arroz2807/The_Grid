
using UnityEngine;

/// <summary>
/// Traduce los cambios de dirección recibidos por ArduinoSerialManager
/// a direcciones cardinales para el movimiento del ciclo.
/// </summary>
public class ArduinoDirectionInputProvider : MonoBehaviour, IDirectionInputProvider
{
    [SerializeField]
    private bool playTurnSound = true;

    public void SetTurnSoundEnabled(bool enabled)
    {
        playTurnSound = enabled;
    }

    public Vector2Int? GetDesiredDirection()
    {
        ArduinoSerialManager serialManager = ArduinoSerialManager.Instance;

        if (serialManager == null)
            return null;

        if (!serialManager.TryGetLatestCommand(out char command))
            return null;

        Vector2Int? direction = ConvertCommandToDirection(command);

        if (direction.HasValue)
        {
            Debug.Log(
                $"[ArduinoInput] Dirección recibida: {command} " +
                $"-> {direction.Value}"
            );

            if (playTurnSound)
                AudioManager.Instance?.PlaySfx(SfxId.Turn);
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
