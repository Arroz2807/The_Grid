using UnityEngine;

/// <summary>
/// Conjunto de colores del ranking. Es un valor inmutable: cada vista (la
/// escena Ranking y el segundo monitor) lo arma con SUS propios campos del
/// Inspector y se lo pasa a RankingRowFormatter. Así los colores se siguen
/// configurando donde ya estaban, pero la regla de qué color lleva cada fila
/// vive en un solo lugar.
/// </summary>
public readonly struct RankingPalette
{
    public readonly Color Normal;
    public readonly Color Highlight;
    public readonly Color Empty;
    public readonly Color Gold;
    public readonly Color Silver;
    public readonly Color Bronze;

    public RankingPalette(Color normal, Color highlight, Color empty,
                          Color gold, Color silver, Color bronze)
    {
        Normal = normal;
        Highlight = highlight;
        Empty = empty;
        Gold = gold;
        Silver = silver;
        Bronze = bronze;
    }
}