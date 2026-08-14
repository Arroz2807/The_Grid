/// <summary>
/// Los tipos de comportamiento de IA que el jugador puede elegir para cada
/// enemigo desde el menú. El orden acá importa: el dropdown de cada slot
/// se completa leyendo estos nombres directamente (Enum.GetNames), así que
/// agregar un valor nuevo alcanza para que aparezca como opción — no hace
/// falta tocar ningún script de UI.
/// </summary>
public enum EnemyType
{
    Random,
    Chase,
    Predict,
    Ambush
}