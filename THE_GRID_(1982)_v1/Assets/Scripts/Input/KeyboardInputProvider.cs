
using UnityEngine;

/// <summary>
/// Traduce WASD y las flechas en direcciones absolutas.
/// Almacena una dirección pendiente hasta que otro componente la consume.
/// </summary>
public class KeyboardInputProvider : MonoBehaviour, IDirectionInputProvider
{
    private Vector2Int? pendingDirection;

    [SerializeField]
    private bool playTurnSound = true;

    /// <summary>
    /// Permite que un proveedor combinado controle el sonido.
    /// </summary>
    public void SetTurnSoundEnabled(bool enabled)
    {
        playTurnSound = enabled;
    }

    private void Update()
    {
        // No acumulamos varias teclas pendientes.
        if (pendingDirection.HasValue)
            return;

        Vector2Int? pressed = ReadDirectionKey();

        if (!pressed.HasValue)
            return;

        pendingDirection = pressed;

        if (playTurnSound)
        {
            AudioManager.Instance?.PlaySfx(SfxId.Turn);
        }
    }

    private static Vector2Int? ReadDirectionKey()
    {
        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow))
            return Vector2Int.up;

        if (Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.DownArrow))
            return Vector2Int.down;

        if (Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.LeftArrow))
            return Vector2Int.left;

        if (Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.RightArrow))
            return Vector2Int.right;

        return null;
    }

    public Vector2Int? GetDesiredDirection()
    {
        Vector2Int? result = pendingDirection;
        pendingDirection = null;
        return result;
    }
}
