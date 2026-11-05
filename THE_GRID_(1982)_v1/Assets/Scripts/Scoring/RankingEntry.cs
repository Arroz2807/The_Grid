using System;

/// <summary>
/// Una entrada del ranking de Waves. Serializable para poder guardarse
/// como JSON con JsonUtility. No es un MonoBehaviour — es sólo datos.
/// </summary>
[Serializable]
public class RankingEntry
{
    public string playerName;
    public int score;
    public int wave;
    public string date;

    public RankingEntry(string name, int score, int wave)
    {
        playerName = name;
        this.score = score;
        this.wave = wave;
        date = DateTime.Now.ToString("yyyy-MM-dd");
    }
}