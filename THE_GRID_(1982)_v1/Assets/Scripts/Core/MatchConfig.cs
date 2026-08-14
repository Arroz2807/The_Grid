using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda la configuración elegida en el menú (cantidad de enemigos y tipo
/// de cada uno) mientras dura la sesión de juego. Es un dato TEMPORAL, no
/// persistente: cambia en cada visita al menú y no tiene sentido que
/// sobreviva a un reinicio de la aplicación — por eso es una clase
/// estática común, no un ScriptableObject. Los campos estáticos
/// sobreviven a un cambio de escena (por eso el viaje MainMenu → Game
/// funciona) pero se resetean solos al cerrar la aplicación, que es
/// exactamente el ciclo de vida que corresponde acá.
/// </summary>
public static class MatchConfig
{
    private static readonly List<EnemyType> enemyTypes = new List<EnemyType>();

    public static IReadOnlyList<EnemyType> EnemyTypes => enemyTypes;

    public static void SetEnemyCount(int count)
    {
        count = Mathf.Clamp(count, 0, 4);

        while (enemyTypes.Count < count) enemyTypes.Add(EnemyType.Random);
        while (enemyTypes.Count > count) enemyTypes.RemoveAt(enemyTypes.Count - 1);
    }

    public static void SetEnemyType(int slotIndex, EnemyType type)
    {
        if (slotIndex < 0 || slotIndex >= enemyTypes.Count) return;
        enemyTypes[slotIndex] = type;
    }
}