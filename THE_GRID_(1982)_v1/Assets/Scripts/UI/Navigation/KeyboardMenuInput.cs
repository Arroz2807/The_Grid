using UnityEngine;

/// <summary>
/// Traduce W/A/S/D/Space en MenuAction, con repetición controlada al
/// mantener una tecla direccional apretada: la primera pulsación actúa
/// de inmediato, y si se sigue sosteniendo, empieza a repetirse recién
/// después de una pausa inicial, a un ritmo más lento — no en cada frame.
/// Space nunca se repite: confirmar es una acción puntual, no continua.
/// </summary>
public class KeyboardMenuInput : MonoBehaviour, IMenuInputProvider
{
    [Header("Repetición al mantener presionado")]
    [Tooltip("Segundos que hay que sostener la tecla antes de que empiece a repetirse.")]
    [SerializeField] private float initialRepeatDelay = 0.35f;
    [Tooltip("Segundos entre cada repetición, una vez que empezó a repetirse.")]
    [SerializeField] private float repeatInterval = 0.12f;

    private static readonly (KeyCode key, MenuAction action)[] DirectionalKeys =
    {
        (KeyCode.W, MenuAction.MoveUp),
        (KeyCode.S, MenuAction.MoveDown),
        (KeyCode.A, MenuAction.DecreaseValue),
        (KeyCode.D, MenuAction.IncreaseValue),
    };

    private KeyCode? heldKey;
    private MenuAction heldAction;
    private float timer;

    public MenuAction GetAction()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            return MenuAction.Confirm;
        }

        // Una pulsación NUEVA siempre actúa de inmediato y reinicia la
        // cuenta regresiva de repetición, sin importar qué tecla se
        // sostenía antes.
        foreach ((KeyCode key, MenuAction action) in DirectionalKeys)
        {
            if (Input.GetKeyDown(key))
            {
                heldKey = key;
                heldAction = action;
                timer = initialRepeatDelay;
                return action;
            }
        }

        if (heldKey.HasValue)
        {
            if (!Input.GetKey(heldKey.Value))
            {
                // Se soltó la tecla que se estaba repitiendo.
                heldKey = null;
                return MenuAction.None;
            }

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = repeatInterval;
                return heldAction;
            }
        }

        return MenuAction.None;
    }
}