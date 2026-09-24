using UnityEngine;

/// <summary>
/// Datos configurables de un tipo de power-up. Es un ScriptableObject
/// porque estos datos son constantes durante una partida (no cambian por
/// instancia), deben poder editarse sin tocar código, y pueden reutilizarse
/// entre escenas. Lo que NO vive acá es la lógica del efecto — eso va en
/// PowerUpEffect, para no mezclar datos con comportamiento.
/// </summary>
[CreateAssetMenu(fileName = "PowerUpDefinition", menuName = "The Grid/Power-Up Definition")]
public class PowerUpDefinition : ScriptableObject
{
    [Header("Identidad")]
    public PowerUpType type;
    [Tooltip("Nombre visible en UI.")]
    public string displayName;

    [Header("Visual en escenario")]
    [Tooltip("Prefab que aparece en la grilla. Debe tener un PowerUpPickup.")]
    public GameObject pickupPrefab;
    [Tooltip("Color del objeto en la grilla.")]
    public Color color = Color.white;

    [Header("Aparición")]
    [Tooltip("Peso relativo de aparición. Mayor = más frecuente que los otros tipos.")]
    [Min(0)]
    public float spawnWeight = 1f;
    [Tooltip("Si false, este tipo nunca aparece aunque esté en la lista del spawner.")]
    public bool enabled = true;

    [Header("Efecto")]
    [Tooltip("Duración del efecto en segundos. 0 = instantáneo o permanente (ej: Escudo, TrailExtension).")]
    [Min(0)]
    public float duration = 5f;
    [Tooltip("Magnitud del efecto. Turbo: multiplicador de velocidad (ej: 1.5 = 50% más rápido). TrailExtension: segmentos a agregar (ej: 10).")]
    public float magnitude = 1f;
    [Tooltip("Sólo para TrailExtension: tope máximo de segmentos de rastro que puede tener un participante. Ignorado por los demás tipos.")]
    [Min(1)]
    public int trailLengthCap = 50;

    [Header("Reglas de recogida")]
    public bool playerCanPickUp = true;
    public bool enemyCanPickUp = true;

    [Header("Audio")]
    [Tooltip("ID del sfx que suena al recoger. Debe existir en SfxId y en SoundLibrary.")]
    public SfxId pickupSfxId;
}