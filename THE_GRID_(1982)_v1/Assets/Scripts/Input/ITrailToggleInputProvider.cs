/// <summary>
/// Señaliza si el jugador pidió alternar (encender/apagar) su rastro en
/// este frame. Es una interfaz separada de IDirectionInputProvider — no una
/// función más ahí adentro — porque "girar" y "alternar el rastro" son dos
/// capacidades distintas del jugador. Juntarlas en una sola interfaz
/// obligaría a cualquier fuente de input futura (por ejemplo una IA) a
/// implementar ambas aunque sólo necesite una.
/// </summary>
public interface ITrailToggleInputProvider
{
    bool WasTrailToggleRequested();
}