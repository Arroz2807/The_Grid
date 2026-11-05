using System;
using System.Collections.Generic;

/// <summary>
/// Contenedor de la lista de entradas. JsonUtility no puede serializar
/// directamente un List<T> en la raíz — necesita estar dentro de una
/// clase. Este wrapper es la solución estándar para ese caso.
/// </summary>
[Serializable]
public class RankingData
{
    public List<RankingEntry> entries = new List<RankingEntry>();
}