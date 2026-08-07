/// <summary>
/// Centraliza los nombres de escena en un único lugar. Sin esta clase,
/// cada script que necesite cargar una escena escribiría el string a mano
/// ("MainMenu", "Game", ...), y un error de tipeo en cualquiera de esos
/// lugares recién se descubre en tiempo de ejecución. Con esta clase el
/// nombre se escribe una única vez y el resto del código lo referencia con
/// autocompletado — si algún día cambiás el nombre de una escena, se
/// actualiza en un solo lugar.
///
/// Esto no elimina el riesgo por completo: si el nombre real de la escena
/// en Build Settings no coincide con lo que hay acá, el error va a seguir
/// apareciendo en tiempo de ejecución. Eliminarlo del todo requeriría
/// referenciar el SceneAsset directamente, lo cual exige compilación
/// condicional (#if UNITY_EDITOR) y agrega una complejidad que no se
/// justifica para un proyecto de este tamaño.
/// </summary>
public static class SceneNames
{
    public const string MainMenu = "MainMenu";

    // Ajustá este valor si tu escena de juego no se llama "Game".
    public const string Game = "Main";
}