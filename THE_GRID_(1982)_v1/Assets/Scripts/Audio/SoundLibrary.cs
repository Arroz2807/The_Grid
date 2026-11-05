using UnityEngine;

/// <summary>
/// Mapea cada SfxId a su AudioClip y a su volumen de balance individual.
/// volumeScale es un multiplicador interno (0-1) sobre el volumen base del
/// AudioSource — permite equilibrar efectos sin tocar el volumen general
/// que el jugador configura desde Settings. No aparece en el menú del juego.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [System.Serializable]
    public struct SfxEntry
    {
        public SfxId id;
        public AudioClip clip;

        [Tooltip("Multiplicador de volumen interno (0-1). Afecta sólo este SFX, sin tocar el volumen general del jugador. Usalo para balancear efectos que suenan demasiado fuerte o suave respecto a los demás.")]
        [Range(0f, 1f)]
        public float volumeScale;
    }

    [SerializeField] private SfxEntry[] entries;

    /// <summary>
    /// Busca el clip correspondiente a un SfxId. Devuelve null si no existe
    /// la entrada (sin error — el sonido simplemente no suena).
    /// </summary>
    public AudioClip GetClip(SfxId id)
    {
        foreach (SfxEntry entry in entries)
        {
            if (entry.id == id) return entry.clip;
        }
        return null;
    }

    /// <summary>
    /// Devuelve el volumeScale configurado para un SfxId.
    /// Si no existe la entrada, devuelve 1 (volumen completo).
    /// </summary>
    public float GetVolumeScale(SfxId id)
    {
        foreach (SfxEntry entry in entries)
        {
            if (entry.id == id) return entry.volumeScale;
        }
        return 1f;
    }
}