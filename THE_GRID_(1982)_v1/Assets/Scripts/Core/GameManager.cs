using UnityEngine;

/// <summary>
/// Reacciona al fin de la partida de un jugador. Se suscribe al evento
/// estático de LightCycleController en vez de que LightCycleController lo
/// llame directamente — así, LightCycleController no necesita saber que
/// GameManager existe.
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private TrailManager trailManager;

    private void OnEnable()
    {
        LightCycleController.OnAnyPlayerDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        // Desuscribirse es tan importante como suscribirse: si no lo
        // hacés, un GameManager destruido (por ejemplo, al cambiar de
        // escena) puede seguir "escuchando" el evento y causar errores de
        // referencia nula. Es uno de los errores más comunes con eventos
        // en C#/Unity.
        LightCycleController.OnAnyPlayerDied -= HandlePlayerDied;
    }

    private void Start()
    {
        GameObject playerInstance = Instantiate(playerPrefab);
        LightCycleController controller = playerInstance.GetComponent<LightCycleController>();

        // GameManager es quien conoce a GridManager y TrailManager (los
        // tiene asignados en su propio Inspector), y se los "inyecta" al
        // jugador recién creado. Esto evita depender de que el prefab
        // tenga esas referencias precargadas, lo cual sería frágil en
        // cuanto instancies más de un jugador.
        // Nota: para que esta línea compile, las referencias privadas
        // gridManager/trailManager de LightCycleController necesitan un
        // método público de inicialización — lo agregamos abajo.
        controller.Initialize(gridManager, trailManager);
    }

    private void HandlePlayerDied(LightCycleController player)
    {
        Debug.Log($"GameManager: la partida terminó. Perdió {player.name}.");

        // Futuro: acá va a ir mostrar un panel de Game Over, detener el
        // tiempo, calcular puntaje, etc.
    }
}