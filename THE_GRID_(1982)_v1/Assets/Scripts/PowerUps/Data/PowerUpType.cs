/// <summary>
/// Identificador de cada tipo de power-up. Mismo patrón que SfxId y
/// EnemyType: vocabulario compartido entre quien spawna, quien recoge y
/// quien aplica efectos, sin dependencias entre ellos.
/// </summary>
public enum PowerUpType
{
    Turbo,
    Shield,
    TrailExtension
}