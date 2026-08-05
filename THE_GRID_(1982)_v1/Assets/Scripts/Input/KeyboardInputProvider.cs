using UnityEngine;

/// <summary>
/// Traduce las teclas del teclado en las dos señales que el jugador puede
/// emitir: un giro (izquierda/derecha) y un pedido de alternar el rastro.
/// Implementa dos interfaces separadas — IDirectionInputProvider e
/// ITrailToggleInputProvider — en vez de una sola con ambos métodos,
/// siguiendo el principio de segregación de interfaces (ver comentario en
/// ITrailToggleInputProvider.cs).
/// </summary>
public class KeyboardInputProvider : MonoBehaviour, IDirectionInputProvider, ITrailToggleInputProvider
{
    [Header("Controles")]
    [Tooltip("Tecla para encender/apagar el rastro.")]
    [SerializeField] private KeyCode trailToggleKey = KeyCode.Q;

    private TurnInput pendingTurn = TurnInput.None;

    private void Update()
    {
        // Si ya hay un giro esperando a ser consumido, ignoramos nuevas
        // teclas hasta que se procese. Esto evita que dos teclas
        // presionadas muy rápido, dentro del mismo intervalo de
        // movimiento, generen dos giros que sumados equivaldrían a un giro
        // de 180°.
        if (pendingTurn != TurnInput.None) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            pendingTurn = TurnInput.Left;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            pendingTurn = TurnInput.Right;
        }
    }

    public TurnInput GetTurnInput()
    {
        TurnInput result = pendingTurn;
        pendingTurn = TurnInput.None; // se consume al leer
        return result;
    }

    // A diferencia de GetTurnInput(), acá no hace falta bufferear: no hay
    // un único "momento del paso" donde la pulsación se pueda perder, ya
    // que LightCycleController consulta esto en todos los frames.
    public bool WasTrailToggleRequested()
    {
        return Input.GetKeyDown(trailToggleKey);
    }
}