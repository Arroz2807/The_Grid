using UnityEngine;

/// <summary>
/// Anima el pickup mientras está en la grilla esperando ser recogido:
/// rotación continua y pulsación de escala. Componente separado de
/// PowerUpPickup para respetar la separación de responsabilidades —
/// PowerUpPickup maneja la lógica de recogida, este maneja sólo la
/// presentación visual idle. Se puede activar/desactivar desde el Inspector
/// sin tocar PowerUpPickup.
/// </summary>
public class PowerUpIdleAnimator : MonoBehaviour
{
    [Tooltip("Grados por segundo de rotación.")]
    [SerializeField] private float rotationSpeed = 90f;

    [Tooltip("Amplitud del pulso de escala (0.1 = ±10% del tamaño base).")]
    [SerializeField] private float pulseAmplitude = 0.1f;

    [Tooltip("Frecuencia del pulso en Hz.")]
    [SerializeField] private float pulseFrequency = 2f;

    private Vector3 baseScale;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        // Rotación continua en Z.
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        // Pulsación de escala usando seno — sin corrutinas, sin estado extra.
        float pulse = 1f + pulseAmplitude * Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f);
        transform.localScale = baseScale * pulse;
    }
}