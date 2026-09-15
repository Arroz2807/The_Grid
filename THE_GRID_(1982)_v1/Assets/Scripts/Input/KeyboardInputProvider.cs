using UnityEngine;

/// <summary>
/// Traduce el teclado (WASD o flechas) en la dirección absoluta deseada
/// por el jugador. Es el único lugar que sabe con certeza que una
/// dirección la pidió el jugador humano (nunca la IA) — por eso el
/// sonido de giro se dispara acá, y no en LightCycleController.
/// </summary>
public class KeyboardInputProvider : MonoBehaviour, IDirectionInputProvider
{
    private Vector2Int? pendingDirection;

    private void Update()
    {
        // Si ya hay una dirección esperando a ser consumida, ignoramos
        // nuevas teclas hasta que se procese.
        if (pendingDirection.HasValue) return;

        Vector2Int? pressed = ReadDirectionKey();
        if (!pressed.HasValue) return;

        pendingDirection = pressed;

        // Sonido de giro: se dispara con cada pulsación válida, sin
        // importar si LightCycleController termina aceptándola o no (por
        // ejemplo, si fuera un giro de 180°). Es una simplificación
        // deliberada — separar ambos casos requeriría que este script
        // supiera el resultado de una decisión que no le pertenece.
        AudioManager.Instance?.PlaySfx(SfxId.Turn);
    }

    private static Vector2Int? ReadDirectionKey()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) return Vector2Int.right;
        return null;
    }

    public Vector2Int? GetDesiredDirection()
    {
        Vector2Int? result = pendingDirection;
        pendingDirection = null;
        return result;
    }
}