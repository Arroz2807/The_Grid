/// <summary>
/// Los modos de juego disponibles. Vive en MatchConfig y sobrevive el
/// viaje MainMenu → Game igual que EnemyTypes, sin DontDestroyOnLoad.
/// </summary>
public enum GameMode
{
    Custom,
    Waves,
    PvP
}