/// <summary>
/// Identificador de cada evento sonoro del juego. Agregar un sonido nuevo
/// es agregar un valor acá y una fila en el asset SoundLibrary — nunca
/// hace falta tocar AudioManager.
/// </summary>
public enum SfxId
{
    MenuNavigate,
    MenuConfirm,
    MatchStart,
    Turn,
    Collision,
    Explosion,
    Victory,
    Defeat
}