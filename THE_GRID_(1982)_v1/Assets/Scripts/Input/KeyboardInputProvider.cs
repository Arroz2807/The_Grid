using UnityEngine;

/// <summary>
/// Traduce el teclado en las dos señales que el jugador puede emitir: una
/// dirección absoluta deseada (WASD o flechas) y un pedido de alternar el
/// rastro. Implementa dos interfaces separadas — IDirectionInputProvider e
/// ITrailToggleInputProvider — siguiendo el principio de segregación de
/// interfaces.
/// </summary>
public class KeyboardInputProvider : MonoBehaviour, IDirectionInputProvider, ITrailToggleInputProvider
{
    [Header("Controles")]
    [Tooltip("Tecla para encender/apagar el rastro.")]
    [SerializeField] private KeyCode trailToggleKey = KeyCode.Q;

    private Vector2Int? pendingDirection;

    private void Update()
    {
        // Si ya hay una dirección esperando a ser consumida, ignoramos
        // nuevas teclas hasta que se procese — evita que pulsaciones muy
        // rápidas, dentro del mismo intervalo de movimiento, se acumulen
        // de forma imprevista.
        if (pendingDirection.HasValue) return;

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            pendingDirection = Vector2Int.up;
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            pendingDirection = Vector2Int.down;
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            pendingDirection = Vector2Int.left;
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            pendingDirection = Vector2Int.right;
        }
    }

    public Vector2Int? GetDesiredDirection()
    {
        Vector2Int? result = pendingDirection;
        pendingDirection = null; // se consume al leer
        return result;
    }

    public bool WasTrailToggleRequested()
    {
        return Input.GetKeyDown(trailToggleKey);
    }
}