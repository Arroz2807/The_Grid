using UnityEngine;

/// <summary>
/// Crea el prefab del segundo monitor una sola vez al arrancar la aplicación,
/// sea cual sea la primera escena. No hay que colocar nada en ninguna escena.
///
/// - Ejecutable con un solo monitor: no crea nada (el juego sigue normal).
/// - Editor: siempre lo crea, para poder previsualizarlo en "Display 2".
/// - Si falta el prefab, avisa por consola y no hace nada (sirve como
///   interruptor: borrar el prefab desactiva la función).
/// </summary>
public static class SecondDisplayBootstrap
{
    // Nombre del prefab dentro de Assets/Resources (sin extensión).
    private const string PrefabName = "SecondDisplayRanking";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        // Ya existe (por ejemplo, alguien lo colocó a mano en una escena).
        if (Object.FindFirstObjectByType<SecondDisplayController>() != null) return;

#if !UNITY_EDITOR
        if (Display.displays.Length < 2)
        {
            Debug.Log("[SecondDisplay] Un solo monitor detectado: no se crea el display secundario.");
            return;
        }
#endif

        GameObject prefab = Resources.Load<GameObject>(PrefabName);
        if (prefab == null)
        {
            Debug.LogWarning("[SecondDisplay] No se encontró el prefab '" + PrefabName +
                             "' en Assets/Resources. El segundo monitor queda deshabilitado.");
            return;
        }

        GameObject instance = Object.Instantiate(prefab);
        instance.name = PrefabName; // sin el sufijo (Clone)
    }
}