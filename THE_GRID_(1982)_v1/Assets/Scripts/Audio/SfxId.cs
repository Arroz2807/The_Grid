/// <summary>
/// Identificador de cada evento sonoro del juego. Agregar un sonido nuevo
/// es agregar un valor acá y una fila en el asset SoundLibrary.
/// </summary>
public enum SfxId
{
    // Menú
    MenuNavigate,
    MenuConfirm,
    MatchStart,

    // Gameplay
    Turn,
    Collision,
    Explosion,
    Victory,
    Defeat,

    // Power-ups
    PowerUpSpawn,
    PowerUpPickup,
    TurboStart,
    TurboEnd,
    ShieldActivate,
    ShieldBlock,
    TrailExtensionPickup
}