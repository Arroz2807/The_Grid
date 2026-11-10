
using UnityEngine;

public class ArduinoInputDebug : MonoBehaviour
{
    private void Update()
    {
        if (ArduinoSerialManager.Instance == null)
            return;

        char direction =
            ArduinoSerialManager.Instance.GetHeldDirection();

        if (direction != 'N')
        {
            Debug.Log($"Joystick mantenido: {direction}");
        }

        if (ArduinoSerialManager.Instance.TryGetConfirmCommand())
        {
            Debug.Log("Botón de confirmar presionado.");
        }
    }
}
