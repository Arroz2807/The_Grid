using UnityEngine;

/// <summary>
/// Mapea cada SfxId a su AudioClip correspondiente. Es puramente una
/// tabla de datos — no reproduce nada, no sabe qué es un AudioSource. Al
/// ser un ScriptableObject (un asset, no un componente de escena), se
/// puede editar la lista de sonidos sin tocar ningún GameObject, y en
/// teoría se podrían tener varias "librerías" intercambiables más
/// adelante (por ejemplo, un pack de sonidos alternativo).
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [System.Serializable]
    public struct SfxEntry
    {
        public SfxId id;
        public AudioClip clip;
    }

    [Tooltip("No hace falta completar todos los SfxId desde el principio — uno sin entrada acá simplemente no suena, sin error.")]
    [SerializeField] private SfxEntry[] entries;

    public AudioClip GetClip(SfxId id)
    {
        foreach (SfxEntry entry in entries)
        {
            if (entry.id == id) return entry.clip;
        }
        return null;
    }
}